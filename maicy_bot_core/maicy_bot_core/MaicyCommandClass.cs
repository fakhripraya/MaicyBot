using Discord;
using Discord.Commands;
using Discord.WebSocket;
using maicy_bot_core.MiscData;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace maicy_bot_core
{
    public class MaicyCommandClass
    {
        private readonly DiscordSocketClient maicy_client;
        private readonly CommandService maicy_cmd_serv;
        private readonly IServiceProvider maicy_services;

        public MaicyCommandClass(DiscordSocketClient client, CommandService cmd, IServiceProvider services)
        {
            maicy_client = client;
            maicy_cmd_serv = cmd;
            maicy_services = services;
        }

        public async Task InitializeAsync()
        {
            await maicy_cmd_serv.AddModulesAsync(Assembly.GetEntryAssembly(), maicy_services);
            maicy_cmd_serv.Log += Maicy_cmd_serv_Log;
            maicy_client.MessageReceived += Maicy_handle_message;
        }

        private async Task Maicy_handle_message(SocketMessage msg)
        {
            var arg_pos = 0;

            if (Gvar.playlist_load_flag)
            {
                return;
            }

            if (msg.Author.IsBot)
            {
                return;
            }

            var user_message = msg as SocketUserMessage;

            if (user_message == null)
            {
                return;
            }

            //if (!user_message.HasStringPrefix("-", ref arg_pos)) // eh
            //{
            //    return;
            //}

            if (!user_message.HasStringPrefix("o", ref arg_pos)) // maicy
            {
                return;
            }

            //if (!user_message.HasStringPrefix("euy!", ref arg_pos)) // euy
            //{
            //    return;
            //}

            //if (!user_message.HasStringPrefix("cc", ref arg_pos)) // cave
            //{
            //    return;
            //}

            var context = new SocketCommandContext(maicy_client, user_message);
            var result = await maicy_cmd_serv.ExecuteAsync(context, arg_pos, maicy_services);
        }

        private Task Maicy_cmd_serv_Log(LogMessage LogMessage)
        {
            Console.WriteLine(LogMessage.Message);
            return Task.CompletedTask;
        }
    }
}
