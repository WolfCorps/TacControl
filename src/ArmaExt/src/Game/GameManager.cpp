#include "GameManager.hpp"
#include "Util/Module.hpp"
#include <string>
#include <cstring>
#include <sstream>
#include <type_traits>
#include <span>

#include "Networking/NetworkController.hpp"
#include "Networking/Serialize.hpp"
#include "Util/Logger.hpp"
#include "Util/Util.hpp"
#include "Modules/RadioModule.hpp"
#include "Modules/ModuleLogitechG15.hpp"
#include "Modules/ModuleGPS.hpp"
#include "Modules/ModuleMarker.hpp"
#include "Modules/ModuleImageDirectory.hpp"
#include "Modules/ModuleGameInfo.hpp"
#include "Modules/ModuleNote.hpp"
#include "Modules/ModuleVehicle.hpp"

int(*GameManager::extensionCallback)(char const* name, char const* function, char const* data);


namespace detail
{
    template<typename T, typename C>
    typename std::enable_if<std::is_base_of_v<IMessageReceiver, T>>::type
    RegisterReceiver(T* module, C&& container) {
        container.insert(std::pair<std::string, IMessageReceiver*>{ module->GetMessageReceiverName(), static_cast<IMessageReceiver*>(module) });
    }

    template<typename T, typename C>
    typename std::enable_if<!std::is_base_of_v<IMessageReceiver, T>>::type
    RegisterReceiver(T* module, C&& container) {
        
    }

    template<typename T>
    typename std::enable_if<std::is_base_of_v<IStateHolder, T>>::type
        CollectState(T* module, JsonArchive& targetContainer) {

        IStateHolder* stateHolder = static_cast<IStateHolder*>(module);

        auto stateName = stateHolder->GetStateHolderName();

        JsonArchive state;
        stateHolder->SerializeState(state);

        targetContainer.Serialize(stateName.data(), state);
    }

    template<typename T>
    typename std::enable_if<!std::is_base_of_v<IStateHolder, T>>::type
        CollectState(T* module, JsonArchive& targetContainer) {

    }

    template<typename T>
    typename std::enable_if<std::is_base_of_v<IPreInitReceiver, T>>::type
        DoPreInit(T* module) {
        module->OnGamePreInit();
    }

    template<typename T>
    typename std::enable_if<!std::is_base_of_v<IPreInitReceiver, T>>::type
        DoPreInit(T* module) {}

    template<typename T>
    typename std::enable_if<std::is_base_of_v<IPostInitReceiver, T>>::type
        DoPostInit(T* module) {
        module->OnGamePostInit();
    }

    template<typename T>
    typename std::enable_if<!std::is_base_of_v<IPostInitReceiver, T>>::type
        DoPostInit(T* module) {}



}



void RVExtension(char* output, int outputSize, const char* function)
{
	std::string_view func(function);

	if (func == "init") {

		Logger::registerLogger(LoggerTypes::General, std::make_shared<FileLogger>("P:/TCGeneral.log"));


#define MODULES_PREINIT(x) (G##x).ModulePreInit();
#define MODULES_INIT(x) (G##x).ModuleInit();
#define MODULES_POSTINIT(x) (G##x).ModulePostInit();
#define MODULES_REGMSGRECV(x) \
    detail::RegisterReceiver(&(G##x), GGameManager.messageReceiverLookup);

        MODULES_LIST(MODULES_PREINIT);
        MODULES_LIST(MODULES_INIT);
        MODULES_LIST(MODULES_POSTINIT);
        MODULES_LIST(MODULES_REGMSGRECV);

#undef MODULES_PREINIT
#undef MODULES_INIT
#undef MODULES_POSTINIT
#undef MODULES_REGMSGRECV

        GNetworkController.SendStateUpdate(); //Initialize initial state to be able to send FullState to connecting clients
	} else if (func == "preInit") {
#define MODULES_PREINIT(x) detail::DoPreInit(&(G##x));
        MODULES_LIST(MODULES_PREINIT);
#undef MODULES_PREINIT
	} else if (func == "postInit") {
#define MODULES_POSTINIT(x) detail::DoPostInit(&(G##x));
        MODULES_LIST(MODULES_POSTINIT);
#undef MODULES_POSTINIT
    }
}

int RVExtensionArgs(char* output, int outputSize, const char* function, const char** argv, int argc)
{
	std::vector<std::string_view> args;
	args.reserve(argc);
    for (int i = 0; i < argc; ++i) {
		args.emplace_back(Util::trim(std::string_view(argv[i]), "\"")); //Check if " trim is needed
    }

	GGameManager.IncomingMessage(std::string_view(function), args);

	return 0;
}

void RVExtensionVersion(char* output, int outputSize)
{
	std::strncpy(output, "Test-Extension v1.0", outputSize - 1);
}

void RVExtensionRegisterCallback(int(*callbackProc)(char const* name, char const* function, char const* data))
{
	GameManager::extensionCallback = callbackProc;
}

void GameManager::IncomingMessage(std::string_view function, const std::vector<std::string_view>& arguments) {

	auto functionPath = Util::split(function, '.');

	auto found = messageReceiverLookup.find(functionPath[0]);
    if (found == messageReceiverLookup.end()) {
		__debugbreak();
        return;
    }

	//remove root entry
	functionPath.erase(functionPath.begin());

	found->second->OnGameMessage(functionPath, arguments);
}

void GameManager::SendMessage(std::string_view function, std::string_view arguments) {
	auto ret = extensionCallback("TC", function.data(), arguments.data());
    __nop();
}

void GameManager::SendMessageInternal(std::string_view function, const std::vector<std::string_view>& arguments) {
    IncomingMessage(function, arguments);
}

void GameManager::TransferNetworkMessage(std::vector<std::string_view>&& function, nlohmann::json&& arguments, const std::function<void(std::string_view)>& replyFunc) {

    //#TODO into task?

    auto found = messageReceiverLookup.find(function[0]);
    if (found == messageReceiverLookup.end()) {
        __debugbreak();
        return;
    }

    std::span<std::string_view> funcSpan(function.begin(), function.end());

    //remove root entry
    funcSpan = funcSpan.subspan(1);

    found->second->OnNetMessage(funcSpan, arguments, replyFunc);
}

void GameManager::CollectGameState(JsonArchive& ar) {

#define MODULES_PullState(x) \
    detail::CollectState(&(G##x), ar);

    MODULES_LIST(MODULES_PullState);

#undef MODULES_PullState

}



//#TODOD on dlldetach
