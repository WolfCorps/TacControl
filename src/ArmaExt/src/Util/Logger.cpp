#include "Logger.hpp"

#include <chrono>
#include <iomanip>

FileLogger::FileLogger(const std::string& filePath) : file(filePath) {}

void FileLogger::log(std::string_view message) {
    if (file.is_open()) {
        const auto now = std::chrono::system_clock::now();
        auto inTimeT = std::chrono::system_clock::to_time_t(now);
        file << std::put_time(std::localtime(&inTimeT), "%H:%M:%S") << " " << message;
        file.flush();
    }
}

void FileLogger::log(std::string_view message, LogLevel _loglevel) {
    log(message); //#TODO add prefix according to loglevel
}

void Logger::registerLogger(LoggerTypes type, std::shared_ptr<ILogger> logger) {
    getInstance()._registerLogger(type, std::move(logger));
}

void Logger::log(LoggerTypes type, const std::string& message) {
    getInstance()._log(type, message);
}

void Logger::log(LoggerTypes type, const std::string& message, LogLevel _loglevel) {
#if DEBUG_MOD_ENABLED || isCI
    if (
#if ENABLE_PLUGIN_LOGS
        type == LoggerTypes::pluginCommands ||
#endif
        (_loglevel != LogLevel_DEVEL && _loglevel != LogLevel_DEBUG))
#endif
        getInstance()._log(type, message, _loglevel);
}

std::vector<std::shared_ptr<ILogger>> Logger::getLogger(LoggerTypes type) {
    const auto found = getInstance().registeredLoggers.find(type);
    if (found != getInstance().registeredLoggers.end())
        return found->second;
    return {};
}

void Logger::_registerLogger(LoggerTypes type, std::shared_ptr<ILogger> logger) {
    registeredLoggers[type].emplace_back(logger);
}

void Logger::_log(LoggerTypes type, const std::string& message) const {
    const auto found = registeredLoggers.find(type);
    if (found != registeredLoggers.end()) {
        for (const std::shared_ptr<ILogger>& it : registeredLoggers.at(type)) {
            it->log(message.back() == '\n' ? message : message + '\n');
        }
    }
    //If not found exit silently
}

void Logger::_log(LoggerTypes type, const std::string& message, LogLevel _loglevel) const {
    const auto found = registeredLoggers.find(type);
    if (found != registeredLoggers.end()) {
        for (const std::shared_ptr<ILogger>& it : registeredLoggers.at(type)) {
            it->log(message.back() == '\n' ? message : message + '\n', _loglevel);
        }
    }
    //If not found exit silently
}