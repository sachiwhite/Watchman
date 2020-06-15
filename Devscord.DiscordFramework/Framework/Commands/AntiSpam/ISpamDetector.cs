﻿using Devscord.DiscordFramework.Framework.Commands.AntiSpam.Models;
using Devscord.DiscordFramework.Framework.Commands.Parsing.Models;
using Devscord.DiscordFramework.Middlewares.Contexts;

namespace Devscord.DiscordFramework.Framework.Commands.AntiSpam
{
    public interface ISpamDetector
    {
        public IUserSafetyChecker UserSafetyChecker { get; set; }
        public SpamProbability GetSpamProbability(ServerMessagesCacheService serverMessagesCacheService, DiscordRequest request, Contexts contexts);
    }
}
