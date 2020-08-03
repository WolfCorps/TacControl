#pragma once
#include "Util/Thread.hpp"

class ModuleLogitechG15 : public Thread {

public:
    void Run() override;
    void Init() override;
};


inline ModuleLogitechG15 GModuleLogitechG15;
