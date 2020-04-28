﻿using System;
using System.Threading.Tasks;
using Devscord.DiscordFramework.Middlewares.Contexts;
using Devscord.DiscordFramework.Services;
using Serilog;
using Watchman.Discord.Areas.Statistics.Services;

namespace Watchman.Discord.Areas.Initialization.Services
{
    public class InitializationService
    {
        private readonly MuteRoleInitService _muteRoleInitService;
        private readonly UsersRolesService _usersRolesService;
        private readonly ServerScanningService _serverScanningService;
        private readonly CyclicStatisticsGeneratorService _cyclicStatisticsGeneratorService;
        private readonly ResponsesInitService _responsesInitService;

        public InitializationService(MuteRoleInitService muteRoleInitService, UsersRolesService usersRolesService, ServerScanningService serverScanningService, CyclicStatisticsGeneratorService cyclicStatisticsGeneratorService, ResponsesInitService responsesInitService)
        {
            _muteRoleInitService = muteRoleInitService;
            _usersRolesService = usersRolesService;
            _serverScanningService = serverScanningService;
            _cyclicStatisticsGeneratorService = cyclicStatisticsGeneratorService;
            _responsesInitService = responsesInitService;
        }

        public async Task InitServer(DiscordServerContext server)
        {
            await ResponsesInit(server);
            await MuteRoleInit(server);
            var lastInitDate = GetLastInitDate(server);
            await ReadServerMessagesHistory(server, lastInitDate);
            await _cyclicStatisticsGeneratorService.GenerateStatsForDaysBefore(server, lastInitDate);
            await NotifyDomainAboutInit(server);
        }

        private async Task ResponsesInit(DiscordServerContext server)
        {
            await _responsesInitService.InitServerResponses(server);
        }

        private async Task MuteRoleInit(DiscordServerContext server)
        {
            var mutedRole = _usersRolesService.GetRoleByName(UsersRolesService.MUTED_ROLE_NAME, server);

            if (mutedRole == null)
            {
                await _muteRoleInitService.InitForServer(server);
            }

            Log.Information($"Mute role initialized: {server.Name}");
        }

        private DateTime GetLastInitDate(DiscordServerContext server)
        {

        }

        private async Task ReadServerMessagesHistory(DiscordServerContext server, DateTime lastInitDate)
        {
            foreach (var textChannel in server.TextChannels)
            {
                await _serverScanningService.ScanChannelHistory(server, textChannel, lastInitDate);
            }

            Log.Information($"Read messages history: {server.Name}");
        }

        private async Task NotifyDomainAboutInit(DiscordServerContext server)
        {

        }
    }
}
