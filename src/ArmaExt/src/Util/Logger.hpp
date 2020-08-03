#pragma once
#include <string>
#include <fstream>
#include <map>
#include <sstream>
#include <vector>



enum class LogLevel {
    CRITICAL = 0, //these messages stop the program
    ERROR,        //everything that is really bad, but not so bad we need to shut down
    WARNING,      //everything that *might* be bad
    DEBUG,        //output that might help find a problem
    INFO,         //informational output, like "starting database version x.y.z"
    DEVEL         //developer only output (will not be displayed in release mode)
};

class ILogger {
protected:
    ~ILogger() = default;

public:
    virtual void log(std::string_view message) = 0;
    virtual void log(std::string_view message, LogLevel _loglevel) = 0;
    template<typename T>
    void operator<< (const T& data) {
        std::stringstream str;
        str << data;
        log(str.str());
    };
};

class FileLogger : public ILogger {
public:
    explicit FileLogger(const std::string& filePath);
    virtual ~FileLogger() = default;

    void log(std::string_view message) override;
    void log(std::string_view message, LogLevel _loglevel) override;
private:
    std::ofstream file;
};

enum class LoggerTypes {
    General
};


class Logger {
public:
    static void registerLogger(LoggerTypes type, std::shared_ptr<ILogger> logger);
    //#TODO deprecate log without logLevel and use DEBUG as default loglevel
    //#TODO log function that takes message as rvalue reference to avoid copy (&&)
    static void log(LoggerTypes type, const std::string& message);
    static void log(LoggerTypes type, const std::string& message, LogLevel _loglevel);
    static std::vector<std::shared_ptr<ILogger>> getLogger(LoggerTypes type);
private:
    Logger() = default;
    ~Logger() = default;
    void _registerLogger(LoggerTypes type, std::shared_ptr<ILogger> logger);
    void _log(LoggerTypes type, const std::string& message) const;
    void _log(LoggerTypes type, const std::string& message, LogLevel _loglevel) const;
    static Logger& getInstance() { static Logger log; return log; }
    std::map<LoggerTypes, std::vector<std::shared_ptr<ILogger>>> registeredLoggers;
};