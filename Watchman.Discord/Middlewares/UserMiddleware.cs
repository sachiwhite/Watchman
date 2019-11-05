﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Watchman.Discord.Framework.Architecture.Middlewares;
using Watchman.Discord.Middlewares.Contexts;

namespace Watchman.Discord.Middlewares
{
    public class UserMiddleware : IMiddleware<UserContext>
    {
        public UserContext Process(SocketMessage data)
        {
            var user = (SocketGuildUser)data.Author;
            var roles = user.Roles.Select(x => x.Name);
            return new UserContext(user.Id, user.ToString(), roles);
        }
    }
}
