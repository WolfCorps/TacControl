#include "GameManager.hpp"
#include "Util/Module.hpp"
#include <string>
#include <cstring>
#include <sstream>

#include "Networking/NetworkController.hpp"
#include "Util/Logger.hpp"
#include "Util/Util.hpp"

int(*GameManager::extensionCallback)(char const* name, char const* function, char const* data);



void RVExtension(char* output, int outputSize, const char* function)
{
	std::string_view func(function);

	if (func == "init") {

		Logger::registerLogger(LoggerTypes::General, std::make_shared<FileLogger>("P:/TCGeneral.log"));


#define MODULES_PREINIT(x) (G##x).PreInit();
#define MODULES_INIT(x) (G##x).Init();
#define MODULES_POSTINIT(x) (G##x).PostInit();
#define MODULES_REGMSGRECV(x) \
	if (dynamic_cast<IMessageReceiver*>(&(G##x))) GGameManager.messageReceiverLookup.insert(std::pair<std::string, IMessageReceiver*>{ (G##x).GetMessageReceiverName(), dynamic_cast<IMessageReceiver*>(&(G##x)) });

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
    }

	//remove root entry
	functionPath.erase(functionPath.begin());

	found->second->OnGameMessage(functionPath, arguments);
}
