using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;
using maicy_bot_core.MiscData;

namespace maicy_bot_core.MaicyServices
{
    public class UtilityService
    {
        private DiscordSocketClient maicy_client;

        public UtilityService(DiscordSocketClient client)
        {
            maicy_client = client;
        }

        public async Task send_async(SocketGuildUser user,string guild_name, string channel_name, string message)
        {
            var guild_list = maicy_client
                .Guilds
                .ToList();

            var guild = guild_list
                .Where(x => x.Name.Contains(guild_name))
                .FirstOrDefault();

            var channel_list = guild
                .Channels
                .ToList();

            var channel = channel_list
                .Where(x => x.Name.Contains(channel_name))
                .FirstOrDefault();

            await maicy_client
                .GetGuild(guild.Id)
                .GetTextChannel(channel.Id)
                .SendMessageAsync(message);
        }
    }
}
