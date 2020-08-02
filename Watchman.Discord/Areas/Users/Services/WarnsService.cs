﻿using Devscord.DiscordFramework.Commons.Exceptions;
using Devscord.DiscordFramework.Commons.Extensions;
using Devscord.DiscordFramework.Framework.Commands.Responses;
using Devscord.DiscordFramework.Middlewares.Contexts;
using Devscord.DiscordFramework.Services;
using Devscord.DiscordFramework.Services.Factories;
using MongoDB.Driver;
using MongoDB.Driver.Core.Servers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watchman.Cqrs;
using Watchman.Discord.Areas.Users.BotCommands;
using Watchman.Discord.Areas.Users.BotCommands.Warns;
using Watchman.DomainModel.Messages;
using Watchman.DomainModel.Warns;
using Watchman.DomainModel.Warns.Commands;
using Watchman.DomainModel.Warns.Queries;

namespace Watchman.Discord.Areas.Protection.Services
{
    public class WarnsService
    {
        private readonly UsersService _usersService;
        private readonly MessagesServiceFactory _messagesServiceFactory;
        private readonly DirectMessagesService _directMessagesService;
        private readonly ICommandBus _commandBus;
        private readonly IQueryBus _queryBus;

        public WarnsService(UsersService usersService, MessagesServiceFactory messagesServiceFactory, DirectMessagesService directMessagesService, ICommandBus commandBus, IQueryBus queryBus)
        {
            this._messagesServiceFactory = messagesServiceFactory;
            this._directMessagesService = directMessagesService;
            this._usersService = usersService;
            this._commandBus = commandBus;
            this._queryBus = queryBus;
        }

        public Task AddWarnToUser(AddWarnCommand command, Contexts contexts, UserContext targetUser)
        {
            var addWarnEventCommand = new AddWarnEventCommand(contexts.User.Id, targetUser.Id, command.Reason, contexts.Server.Id);
            return this._commandBus.ExecuteAsync(addWarnEventCommand);
        }

        public async Task GetWarns(WarnsCommand command, Contexts contexts, UserContext mentionedUser, ulong serverId)
        {
            var messageService = this._messagesServiceFactory.Create(contexts);
            if (mentionedUser == null)
            {
                await messageService.SendResponse(x => x.UserNotFound(command.User.GetUserMention()));
            }
            else
            {
                var warnEvents = await GetWarnEvents(serverId, mentionedUser.Id);
                var warnKeyValues = WarnEventsToKeyValue(warnEvents, false, mentionedUser.Id);
                await messageService.SendEmbedMessage("Warnings", string.Empty, warnKeyValues);
            }
        }

        public async Task GetAllWarns(WarnsCommand command, Contexts contexts, UserContext mentionedUser, ulong serverId)
        {
            if (!contexts.User.IsAdmin)
            {
                throw new NotAdminPermissionsException();
            }
            if (mentionedUser == null)
            {
                await this._directMessagesService.TrySendMessage(contexts.User.Id, x => x.UserNotFound(command.User.GetUserMention()), contexts);
            }
            else
            {
                var warnEvents = await GetWarnEvents(serverId, mentionedUser.Id);
                var warnKeyValues = WarnEventsToKeyValue(warnEvents, true, mentionedUser.Id);
                await _directMessagesService.TrySendEmbedMessage(contexts.User.Id, "Warnings", string.Empty, warnKeyValues);
            }
        }

        public async Task<IEnumerable<WarnEvent>> GetWarnEvents(ulong serverId, ulong userId)
        {
            var query = new GetWarnEventsQuery(serverId, userId);
            var response = await this._queryBus.ExecuteAsync(query);
            return response.WarnEvents;
        }

        private IEnumerable<KeyValuePair<string, string>> WarnEventsToKeyValue(IEnumerable<WarnEvent> warns, bool showServer, ulong mentionedUser)
        {
            var warnEventPairs = new List<KeyValuePair<string, string>>();
            foreach (var warnEvent in warns)
            {
                var warnContentBuilder = new StringBuilder();
                warnContentBuilder.Append("Granted by: ").Append(warnEvent.GrantorId.GetUserMention())
                    .AppendLine().Append("Receiver: ").Append(warnEvent.ReceiverId.GetUserMention())
                    .AppendLine().Append("Reason: ").Append(warnEvent.Reason);
                if (showServer)
                {
                    warnContentBuilder.AppendLine().Append("ServerId: ").Append(warnEvent.ServerId.ToString());
                }
                var eventKeyValuePair = new KeyValuePair<string, string>(warnEvent.CreatedAt.ToString(), warnContentBuilder.ToString());
                warnEventPairs.Add(eventKeyValuePair);
            }
            if (warnEventPairs.Count() == 0)
            {
                warnEventPairs.Add(new KeyValuePair<string, string>("No content", $"User {mentionedUser.GetUserMention()} has no warns."));
            }
            return warnEventPairs;
        }
    }
}
