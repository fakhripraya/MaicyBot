using Discord;
using Discord.Commands;
using Discord.WebSocket;
using maicy_bot_core.MaicyServices;
using maicy_bot_core.MiscData;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Victoria;

namespace maicy_bot_core.MaicyModule
{
    public class MusicModule : ModuleBase<SocketCommandContext>
    {
        private MusicService maicy_music_service;

        public MusicModule(MusicService music_service)
        {
            maicy_music_service = music_service;
        }

        [Command("Join"), Alias("Connect", "cn", "masok", "sokin", "masuk")]
        public async Task Join()
        {
            var user = Context.User as SocketGuildUser;

            if (user.VoiceChannel is null)
            {
                await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription("You need to connect to a voice channel.")
                    .WithCurrentTimestamp()
                    .Build());
                return;
            }
            else
            {
                var reply_msg = await maicy_music_service.connect_async(user.VoiceChannel, Context.Channel as ITextChannel);

                await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(reply_msg)
                    .WithCurrentTimestamp()
                    .Build());
            }
        }

        [Command("Leave"), Alias("dc", "disconnect", "kluar", "keluar", "caw")]
        public async Task Leave()
        {
            var user = Context.User as SocketGuildUser;

            if (user.VoiceChannel is null)
            {
                await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription("Please join the voice channel the bot is in to make it leave.")
                    .WithCurrentTimestamp()
                    .Build());
            }
            else
            {
                var reply_msg = await maicy_music_service.leave_async(user.VoiceChannel, Context.Channel as ITextChannel);

                await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(reply_msg)
                    .WithCurrentTimestamp()
                    .Build());
            }
        }

        [Command("Play"), Alias("p", "main", "mainken")]
        public async Task Play([Remainder]string search)
        {
            var user = Context.User as SocketGuildUser;

            if (user.VoiceChannel is null)
            {
                await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription("You need to connect to a voice channel.")
                    .WithCurrentTimestamp()
                    .Build());

                return;
            }

            await maicy_music_service.play_async(
                search,
                Context.Guild.Id,
                user.VoiceChannel,
                Context.Channel as ITextChannel,
                user.VoiceChannel.Name, "YT", user.Id);
        }

        [Command("Restart")]
        public async Task Restart()
        {
            var user = Context.User as SocketGuildUser;

            if (user.VoiceChannel is null)
            {
                await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription("You need to connect to a voice channel.")
                    .WithCurrentTimestamp()
                    .Build());

                return;
            }

            var reply_msg = await maicy_music_service.restart_async(user.VoiceChannel, Context.Channel as ITextChannel);

            await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(reply_msg)
                    .WithCurrentTimestamp()
                    .Build());
        }

        [Command("Spotify"), Alias("sp", "spotifa", "spoti")]
        public async Task Spotify([Remainder]string search)
        {
            var user = Context.User as SocketGuildUser;

            if (user.VoiceChannel is null)
            {
                await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription("You need to connect to a voice channel.")
                    .WithCurrentTimestamp()
                    .Build());

                return;
            }

            await maicy_music_service.play_async(
                search,
                Context.Guild.Id,
                user.VoiceChannel,
                Context.Channel as ITextChannel,
                user.VoiceChannel.Name, "SP", user.Id);
        }

        [Command("soundcloud"), Alias("sc", "sonclod", "sonklod")]
        public async Task Soundcloud([Remainder]string search)
        {
            var user = Context.User as SocketGuildUser;

            if (user.VoiceChannel is null)
            {
                await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription("You need to connect to a voice channel.")
                    .WithCurrentTimestamp()
                    .Build());

                return;
            }

            await maicy_music_service.play_async(
                search,
                Context.Guild.Id,
                user.VoiceChannel,
                Context.Channel as ITextChannel,
                user.VoiceChannel.Name, "SC", user.Guild.Id);
        }

        [Command("Clear"), Alias("s", "cl", "stop", "bersihken")]
        public async Task Stop()
        {
            var user = Context.User as SocketGuildUser;

            string reply_msg = await maicy_music_service.clear_not_async(user.VoiceChannel);

            await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(reply_msg)
                    .WithCurrentTimestamp()
                    .Build());
        }

        [Command("Remove"), Alias("r", "cabut", "cabutken")]
        public async Task Remove(int index)
        {
            var user = Context.User as SocketGuildUser;

            if (user.VoiceChannel is null)
            {
                await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription("You need to connect to a voice channel.")
                    .WithCurrentTimestamp()
                    .Build());

                return;
            }

            await maicy_music_service.remove_async(index, user.VoiceChannel, Context.Channel as ITextChannel);
        }

        [Command("Pause"), Alias("ps", "henti", "hentiken", "hentikeun", "sebat", "sebatdl", "sebatdulu")]
        public async Task Pause()
        {
            var user = Context.User as SocketGuildUser;
            string reply_msg = await maicy_music_service.pause_async(user.VoiceChannel);

            await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(reply_msg)
                    .WithCurrentTimestamp()
                    .Build());
        }

        [Command("Resume"), Alias("con", "res", "lanjut", "lanjutken", "lanjutkeun", "gasken", "gaskeun", "skuy")]
        public async Task Resume()
        {
            var user = Context.User as SocketGuildUser;
            string reply_msg = await maicy_music_service.resume_async(user.VoiceChannel);

            await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(reply_msg)
                    .WithCurrentTimestamp()
                    .Build());
        }

        [Command("Skip"), Alias("n", "next")]
        public async Task Skip()
        {
            var user = Context.User as SocketGuildUser;
            var reply_msg = await maicy_music_service.skip_async(user.VoiceChannel);

            await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(reply_msg)
                    .WithCurrentTimestamp()
                    .Build());
        }

        [Command("Volume"), Alias("v", "vol", "suara")]
        public async Task Volume(int vol)
        {
            var user = Context.User as SocketGuildUser;
            string reply_msg = await maicy_music_service.set_volume_async(vol, user.VoiceChannel);

            await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(reply_msg)
                    .WithCurrentTimestamp()
                    .Build());
        }

        [Command("Earrape")]
        public async Task Earrape()
        {
            var user = Context.User as SocketGuildUser;
            string reply_msg = await maicy_music_service.set_Earrape(user.VoiceChannel);

            await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(reply_msg)
                    .WithCurrentTimestamp()
                    .Build());
        }

        [Command("Loop"), Alias("lp", "repeat", "rp")]
        public async Task Loop()
        {
            var user = Context.User as SocketGuildUser;

            if (user.VoiceChannel is null)
            {
                await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription("You need to connect to a voice channel.")
                    .WithCurrentTimestamp()
                    .Build());

                return;
            }

            string reply_msg = maicy_music_service.player_check(user.VoiceChannel);

            await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(reply_msg)
                    .WithCurrentTimestamp()
                    .Build());
        }

        [Command("Now"), Alias("np", "nowplaying", "sekarang")]
        public async Task Now()
        {
            await maicy_music_service.now_async(Context.Channel as ITextChannel);
        }

        [Command("Lyrics"), Alias("ly", "Lyric")]
        public async Task Lyric()
        {

            string reply_msg = await maicy_music_service.lyric_async();

            await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(reply_msg)
                    .WithCurrentTimestamp()
                    .Build());
        }

        [Command("Queue"), Alias("q", "antrean")]
        public async Task Queue()
        {
            Embed reply_msg = maicy_music_service.queue_async(null);
            await ReplyAsync(default, default, reply_msg);
        }

        [Command("Page"), Alias("halaman")]
        public async Task Page(int? input_page)
        {
            Embed reply_msg = maicy_music_service.queue_async(input_page);
            await ReplyAsync(default, default, reply_msg);
        }

        [Command("Shuffle"), Alias("sh", "acak", "everydayimshuffling")]
        public async Task Shuffle()
        {
            var user = Context.User as SocketGuildUser;
            string reply_msg = maicy_music_service.shuffle_async(user.VoiceChannel);

            await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(reply_msg)
                    .WithCurrentTimestamp()
                    .Build());
        }

        [Command("Autoplay"), Alias("Auto", "otomatis")]
        public async Task Autoplay()
        {
            var user = Context.User as SocketGuildUser;
            string reply_msg = maicy_music_service.auto_check(user.VoiceChannel);

            await ReplyAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(reply_msg)
                    .WithCurrentTimestamp()
                    .Build());
        }
    }
}
