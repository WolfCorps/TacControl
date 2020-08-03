#include "GameManager.hpp"
#include "Util/Module.hpp"
#include <string>
#include <cstring>
#include <sstream>
#include <type_traits>

#include "Networking/NetworkController.hpp"
#include "Util/Logger.hpp"
#include "Util/Util.hpp"
#include "Modules/RadioModule.hpp"
#include "Modules/ModuleLogitechG15.hpp"

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



}



void RVExtension(char* output, int outputSize, const char* function)
{
	std::string_view func(function);

	if (func == "init") {

		Logger::registerLogger(LoggerTypes::General, std::make_shared<FileLogger>("P:/TCGeneral.log"));


#define MODULES_PREINIT(x) (G##x).PreInit();
#define MODULES_INIT(x) (G##x).Init();
#define MODULES_POSTINIT(x) (G##x).PostInit();
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
    ret;
}

void GameManager::SendMessageInternal(std::string_view function, const std::vector<std::string_view>& arguments) {
    IncomingMessage(function, arguments);
}
