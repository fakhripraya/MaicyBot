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
    public class Utility : ModuleBase<SocketCommandContext>
    {
        private UtilityService maicy_utility_service;

        public Utility(UtilityService utility_service)
        {
            maicy_utility_service = utility_service;
        }

        [Command("Help"), Alias("h", "helep", "tolong")]
        public async Task Help()
        {
            var embed = new EmbedBuilder
            {
                Title = "Help",
            };
            var ready = embed
                .WithColor(Color.Green)
                .WithDescription(
                "MUSIC COMMAND\n\n\n" +
                "`Join` [Join current user channel]\n" +
                "`Leave` [Leave channel]\n" +
                "`Restart` [Restart the music player]\n" +
                "\n" +
                "`Play` [Play song/playlist from YouTube]\n" +
                "`Soundcloud` [Play song from Soundcloud]\n" +
                "`Spotify` [Play song/playlist/album from Spotify]\n" +
                "\n" +
                "`Resume` [Resume current playback]\n" +
                "`Pause` [Pause current playback]\n" +
                "\n" +
                "`Clear` [Stop and Clear all tracks]\n" +
                "`Skip` [Skip current playback]\n" +
                "`Remove` [Remove the selected track]\n" +
                "\n" +
                "`Volume` [Set playback Volumes]\n" +
                "`Loop` [Loop the whole tracks]\n" +
                "`Autoplay` [Toggle autoplay]\n" +
                "\n" +
                "`Now` [Get current track info]\n" +
                "`Lyrics` [Search Lyrics]\n" +
                "\n" +
                "`Queue` [Get tracks queue info]\n" +
                "`Page` [Get selected queue page info]\n" +
                "`Shuffle` [Shuffle the queue randomly]\n" +
                "\n\n" +
                "UTILITY\n\n" +
                "`Help` [summon this command]")
                .WithFooter("This bot is still a WIP , Please contact : PakPres#8360 for any feedback")
                .WithCurrentTimestamp()
                .Build();

            await ReplyAsync(default, default, ready);
        }

        [Command("Send")]
        public async Task Send(string guild_name, string channel_name, string message)
        {
            var user = Context.User as SocketGuildUser;

            await maicy_utility_service.send_async(user, guild_name, channel_name, message);
        }

        [Command("Kick")]
        public async Task Kick(IGuildUser userAccount, string reason)
        {
            //belum pasti
            if (true)
            {
                await ReplyAsync("Command Unavailable");
            }

            var user = Context.User as SocketGuildUser;
            var role = (user as IGuildUser).Guild.Roles.FirstOrDefault(x => x.Name == "Marshall");
            if (user.GuildPermissions.KickMembers)
            {
                await userAccount.KickAsync(reason);
                await Context.Channel.SendMessageAsync($"The user `{userAccount}` has been kicked, for {reason}");
            }
            else
            {
                await Context.Channel.SendMessageAsync("No permissions for kicking a user.");
            }
        }
    }
}
