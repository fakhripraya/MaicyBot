using Discord;
using Discord.Commands;
using Discord.WebSocket;
using maicy_bot_core.MaicyServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace maicy_bot_core.MaicyModule
{
    public class SecretModule : ModuleBase<SocketCommandContext>
    {
        private SecretService maicy_secret_service;

        public SecretModule(SecretService secret_service)
        {
            maicy_secret_service = secret_service;
        }

        //Secret Command
        [Command("OmAi")]
        public async Task OmAi()
        {
            var reply_msg = maicy_secret_service.om_ai_async();
            await ReplyAsync(reply_msg);
        }
    }
}
