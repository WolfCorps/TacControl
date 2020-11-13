#include "ModuleACE.hpp"

#include "Game/GameManager.hpp"
#include "Networking/NetworkController.hpp"
#include "Networking/Serialize.hpp"

void ModuleACE::Explosive::Serialize(JsonArchive& ar) {
    ar.Serialize("id", netId);
    ar.Serialize("code", explosiveCode);
    ar.Serialize("detonator", detonator);
}


void ModuleACE::ModuleInit() {}

void ModuleACE::OnGameMessage(const std::vector<std::string_view>& function, const std::vector<std::string_view>& arguments) {
    auto func = function[0];
    if (func == "AvailExp") {
        std::vector<Explosive> newExplosives;

        for (auto& it : arguments) {
            Explosive newExp;

            auto subArgs = Util::split(it, '\n');

            newExp.netId = subArgs[0];
            auto expCodeStr = subArgs[1];
            auto spaceOffs = expCodeStr.find_last_of(' ');
            auto codeStr = expCodeStr.substr(spaceOffs + 1);
            newExp.explosiveCode = Util::parseArmaNumber(codeStr);
            newExp.explosiveClass = subArgs[2];
            newExp.detonator = subArgs[3];

            newExplosives.emplace_back(std::move(newExp));
        }

        explosives = newExplosives;


        GNetworkController.SendStateUpdate("ACE");
    }
}

void ModuleACE::OnNetMessage(std::span<std::string_view> function, const nlohmann::json& arguments, const std::function<void(std::string_view)>& replyFunc) {

    if (function[0] == "Detonate") {
        std::map<std::string, std::string> detonatorToId;

        for (auto& it : arguments["explosives"]) {
            std::string_view name = it;

            auto found = std::find_if(explosives.begin(), explosives.end(), [&name](const Explosive& exp) {
                return exp.netId == name;
                });
            if (found == explosives.end()) continue;


            detonatorToId[found->detonator] += ";" + found->netId;
        }

        for (auto& [det, exp] : detonatorToId)
            GGameManager.SendMessage("ACE.Cmd.Detonate", fmt::format("{}\n{}", det, exp));
    }
}

void ModuleACE::SerializeState(JsonArchive& ar) {
    ar.Serialize("exp", explosives);
}