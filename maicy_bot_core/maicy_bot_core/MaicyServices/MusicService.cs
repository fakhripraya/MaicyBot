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
using YoutubeExplode;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Models;

namespace maicy_bot_core.MaicyServices
{
    public class MusicService
    {
        private DiscordSocketClient maicy_client;
        private LavaRestClient lava_rest_client;
        private LavaSocketClient lava_socket_client;
        private LavaPlayer lava_player;
        private static SpotifyWebAPI _spotify;

        public MusicService(LavaRestClient lavaRestClient,
            LavaSocketClient lavaSocketClient,
            DiscordSocketClient client)
        {
            maicy_client = client;
            lava_rest_client = lavaRestClient;
            lava_socket_client = lavaSocketClient;
        }

        public Task InitializeAsync()
        {
            maicy_client.Ready += Maicy_client_Ready_async;
            lava_socket_client.Log += Lava_socket_client_Log;
            lava_socket_client.OnTrackFinished += Lava_socket_client_OnTrackFinished;
            lava_socket_client.OnTrackException += Lava_socket_client_OnTrackException;
            maicy_client.UserVoiceStateUpdated += Maicy_client_UserVoiceStateUpdated;
            maicy_client.Disconnected += Maicy_client_Disconnected;

            return Task.CompletedTask;
        }

        private Task Maicy_client_UserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
        {
            try
            {
                if (Gvar.current_client_channel == null)
                {
                    return Task.CompletedTask;
                }

                foreach (var bot in Gvar.current_client_channel.Users.ToList())
                {
                    if (bot.IsBot == false)
                    {
                        return Task.CompletedTask;
                    }
                    else
                    {
                        continue;
                    }
                }


                var text_channel = Gvar.current_client_text_channel as ITextChannel;

                clear_all_loop();
                lava_socket_client.DisconnectAsync(Gvar.current_client_channel as IVoiceChannel);
                Gvar.current_client_channel = null;
                Gvar.current_client_text_channel = null;

                text_channel.SendMessageAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription("All user left, Disconnecting.")
                    .WithCurrentTimestamp()
                    .Build());

                lava_player = null;
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Task.CompletedTask;
            }
        }

        private Task Lava_socket_client_OnTrackException(LavaPlayer player, LavaTrack track, string ex_msg)
        {
            clear_all_loop();
            lava_player = null;
            lava_socket_client.DisconnectAsync(player.VoiceChannel);

            player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription($"Track Error, {ex_msg} Disconnecting.")
                    .WithCurrentTimestamp()
                    .Build());

            Console.WriteLine($"Track Error, {ex_msg} Disconnecting.");
            return Task.CompletedTask;
        }

        private Task Maicy_client_Disconnected(Exception ex)
        {
            Console.WriteLine(ex.Message);
            return Task.CompletedTask;
        }

        //clear all
        public void clear_all_loop()
        {
            Gvar.loop_track = null;
            Gvar.list_loop_track = null;
            Gvar.loop_flag = false;
            Gvar.first_track = 0;
            Gvar.toggle_auto = false;

            return;
        }

        //on song finish
        private async Task Lava_socket_client_OnTrackFinished(
            LavaPlayer player,
            LavaTrack track,
            TrackEndReason reason)
        {
            try
            {
                if (player.IsPlaying)
                {
                    if (reason == TrackEndReason.Replaced)
                    {
                        await now_async(default);
                    }

                    return;
                }

                if (!player.IsPaused && player.IsPlaying)
                {
                    return;
                }

                if (Gvar.loop_flag is true && (Gvar.loop_track != null || Gvar.list_loop_track != null))
                {
                    if (!player.Queue.TryDequeue(out var item)
                    || !(item is LavaTrack next_track))
                    {
                        if (!player.IsPlaying && Gvar.loop_track != null)
                        {
                            await player.PlayAsync(Gvar.loop_track);

                            foreach (var loop_item in Gvar.list_loop_track)
                            {
                                player.Queue.Enqueue(loop_item);
                            }
                        }

                        if (!player.IsPlaying && Gvar.loop_track == null)
                        {
                            foreach (var loop_item in Gvar.list_loop_track)
                            {
                                if (!player.IsPlaying)
                                {
                                    await player.PlayAsync(loop_item as LavaTrack);
                                    continue;
                                }

                                player.Queue.Enqueue(loop_item);
                            }
                        }

                        await now_async(default);
                    }
                    else
                    {
                        await player.PlayAsync(next_track);
                        await now_async(default);
                    }
                }
                else
                {
                    if (!player.Queue.TryDequeue(out var item)
                        || !(item is LavaTrack next_track))
                    {
                        if (Gvar.toggle_auto is true)
                        {
                            var results = await lava_rest_client.SearchYouTubeAsync(track.Title);

                            if (results.LoadType == LoadType.NoMatches
                            || results.LoadType == LoadType.LoadFailed
                            || results.Tracks.Count() == 0)
                            {
                                results = await lava_rest_client.SearchYouTubeAsync(track.Author);

                                if (results.LoadType == LoadType.NoMatches
                                    || results.LoadType == LoadType.LoadFailed
                                    || results.Tracks.Count() == 0)
                                {
                                    await player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                                        .WithColor(Color.Green)
                                                        .WithDescription("Autoplay matching failed , trying to fetch a next song from current queue")
                                                        .WithCurrentTimestamp()
                                                        .Build());
                                }
                            }

                            if (results.LoadType != LoadType.NoMatches
                            || results.LoadType != LoadType.LoadFailed
                            || results.Tracks.Count() != 0)
                            {
                                Random random = new Random();
                                int track_random = random.Next(0, results.Tracks.Count());

                                track = results.Tracks.ElementAtOrDefault(track_random);
                                await player.PlayAsync(track);
                                await now_async(default);
                                return;
                            }
                        }

                        await player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                            .WithColor(Color.Green)
                            .WithDescription("There are no more tracks in the queue. Disconnecting")
                            .WithCurrentTimestamp()
                            .Build());

                        clear_all_loop();
                        lava_player = null;
                        Gvar.current_client_channel = null;
                        Gvar.current_client_text_channel = null;
                        await lava_socket_client.DisconnectAsync(player.VoiceChannel);
                        return;
                    }

                    await player.PlayAsync(next_track);
                    await now_async(default);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                await player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription($"Error {ex.Message} Disconnecting")
                    .WithCurrentTimestamp()
                    .Build());

                clear_all_loop();
                lava_player = null;
                Gvar.current_client_channel = null;
                Gvar.current_client_text_channel = null;
                await lava_socket_client.DisconnectAsync(player.VoiceChannel);
                return;
            }
        }

        //get spotify access token
        public async Task get_access()
        {
            CredentialsAuth auth = new CredentialsAuth("56894be43189492a881161efd8963cb0", "06a0a3c3331247c4bf4f2a5f979a3d11");
            Token token = await auth.GetToken();
            _spotify = new SpotifyWebAPI()
            {
                AccessToken = token.AccessToken,
                TokenType = token.TokenType
            };
            return;
        }

        //player loop check
        public string player_check(SocketVoiceChannel voice_channel)
        {
            if (lava_player == null)
            {
                return "There are no track to loop";
            }

            if (lava_player.VoiceChannel != voice_channel)
            {
                return "Please join the voice channel the bot is in to trigger loop";
            }

            if (Gvar.loop_flag is true)
            {
                clear_all_loop();
                return "Loop Off";
            }
            else
            {
                Gvar.loop_track = lava_player.CurrentTrack;
                Gvar.list_loop_track = lava_player.Queue.Items.ToList();
                Gvar.loop_flag = true;
                return "Loop On";
            }
        }

        //autoplay check
        public string auto_check(SocketVoiceChannel voice_channel)
        {
            if (lava_player == null)
            {
                return "There are no track playing at this time.";
            }

            if (lava_player.VoiceChannel != voice_channel)
            {
                return "Please join the voice channel the bot is in to toggle autoplay";
            }

            if (Gvar.toggle_auto is true)
            {
                Gvar.toggle_auto = false;
                return "Autoplay Off";
            }
            else
            {
                Gvar.toggle_auto = true;
                return "Autoplay On";
            }
        }

        //join
        public async Task<string> connect_async(SocketVoiceChannel voice_channel, ITextChannel text_channel)
        {
            try
            {
                Gvar.current_client_text_channel = text_channel;
                Gvar.current_client_channel = maicy_client.GetChannel(voice_channel.Id);
                await lava_socket_client.ConnectAsync(voice_channel, text_channel);

                if (lava_socket_client.GetPlayer(voice_channel.Guild.Id) != null)
                {
                    foreach (var item in lava_socket_client.GetPlayer(voice_channel.Guild.Id)
                    .VoiceChannel
                    .PermissionOverwrites)
                    {
                        if (item.TargetId == maicy_client.CurrentUser.Id)
                        {
                            if (item.Permissions.Connect.ToString() == "Deny")
                            {
                                return "im not allowed to join your voice channel.";
                            }
                        }
                    }
                }

                return $"Successfully connected to {voice_channel.Name}";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return $"Error ,{ex.Message}";
            }
        }

        //leave
        public async Task<string> leave_async(SocketVoiceChannel voice_channel, ITextChannel text_channel)
        {
            try
            {
                if (lava_player != null)
                {
                    if (lava_player.VoiceChannel != voice_channel)
                    {
                        return "Music player still active , Please join the voice channel the bot is in to make it leave.";
                    }
                }

                Gvar.current_client_text_channel = null;
                Gvar.current_client_channel = null;
                clear_all_loop();
                lava_player = null;
                await lava_socket_client.DisconnectAsync(voice_channel);

                return $"Successfully disconnected from {voice_channel.Name}";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return $"Error , {ex.Message}";
            }
        }

        //restart
        public async Task<string> restart_async(SocketVoiceChannel voice_channel, ITextChannel text_channel)
        {
            try
            {
                if (lava_player != null)
                {
                    if (lava_player.VoiceChannel != voice_channel)
                    {
                        return "Music player still active , Please join the voice channel the bot is in to make it restart.";
                    }
                }

                clear_all_loop();
                lava_player = null;
                Gvar.current_client_channel = null;
                Gvar.current_client_text_channel = null;
                await lava_socket_client.DisconnectAsync(voice_channel);
                await lava_socket_client.ConnectAsync(voice_channel, text_channel);

                if (lava_socket_client.GetPlayer(voice_channel.Guild.Id) != null)
                {
                    foreach (var item in lava_socket_client.GetPlayer(voice_channel.Guild.Id)
                    .VoiceChannel
                    .PermissionOverwrites)
                    {
                        if (item.TargetId == maicy_client.CurrentUser.Id)
                        {
                            if (item.Permissions.Connect.ToString() == "Deny")
                            {
                                await leave_async(voice_channel, text_channel);
                                return "Successfuly restart but im not allowed to join your voice channel.";
                            }
                        }
                    }
                }

                Gvar.current_client_channel = maicy_client.GetChannel(voice_channel.Id);
                Gvar.current_client_text_channel = maicy_client.GetChannel(text_channel.Id) as ITextChannel;
                return "Successfully restart.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return $"Error , {ex.Message}";
            }
        }

        //play music from youtube
        public async Task play_async(
            string search,
            ulong guild_id,
            SocketVoiceChannel voice_channel,
            ITextChannel channel,
            string voice_channel_name,
            string type,
            ulong user_guild_id)
        {
            try
            {
                Gvar.current_client_channel = maicy_client.GetChannel(voice_channel.Id);
                var lava_client_id = Gvar.current_client_channel
                    .Users
                    .Select(x => x)
                    .Where(x => x.IsBot == true && x.Id == maicy_client.CurrentUser.Id)
                    .FirstOrDefault();

                if (lava_client_id == null)
                {
                    Gvar.current_client_text_channel = channel;
                    Gvar.current_client_channel = maicy_client.GetChannel(voice_channel.Id);

                    await connect_async(voice_channel, channel);

                    if (lava_socket_client.GetPlayer(voice_channel.Guild.Id) != null)
                    {
                        foreach (var item in lava_socket_client.GetPlayer(voice_channel.Guild.Id)
                        .VoiceChannel
                        .PermissionOverwrites)
                        {
                            if (item.TargetId == maicy_client.CurrentUser.Id)
                            {
                                if (item.Permissions.Connect.ToString() == "Deny")
                                {
                                    await channel.SendMessageAsync(default, default, new EmbedBuilder()
                                        .WithColor(Color.Green)
                                        .WithDescription("im not allowed to join your voice channel.")
                                        .WithCurrentTimestamp()
                                        .Build());
                                    await leave_async(voice_channel, channel);
                                    return;
                                }
                            }
                        }
                    }

                    lava_player = lava_socket_client.GetPlayer(guild_id);
                    await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                        .WithColor(Color.Green)
                                        .WithDescription($"Successfully connected to {voice_channel_name}")
                                        .WithCurrentTimestamp()
                                        .Build());
                }
                else
                {
                    lava_player = lava_socket_client.GetPlayer(guild_id);
                }

                if (lava_player.VoiceChannel != voice_channel)
                {
                    await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                        .WithColor(Color.Green)
                                        .WithDescription("Please join the voice channel the bot is in to queue track.")
                                        .WithCurrentTimestamp()
                                        .Build());
                    return;
                }

                SearchResult results = null; //initialize biar seneng
                if (type == "YT")
                {
                    if (search.Contains("playlist?list="))
                    {
                        results = await lava_rest_client.SearchTracksAsync(search);

                        if (results.LoadType == LoadType.NoMatches
                        || results.LoadType == LoadType.LoadFailed
                        || results.Tracks.Count() == 0)
                        {
                            await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                        .WithColor(Color.Green)
                                        .WithDescription("No matches found. try more specific")
                                        .WithCurrentTimestamp()
                                        .Build());
                            return;
                        }

                        await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                        .WithColor(Color.Green)
                                        .WithDescription($"Adding {results.PlaylistInfo.Name} playlist to the queue. Please wait.")
                                        .WithCurrentTimestamp()
                                        .Build());

                        Gvar.playlist_load_flag = true;

                        foreach (var item in results.Tracks)
                        {
                            if (lava_player.IsPlaying)
                            {
                                lava_player.Queue.Enqueue(item);

                                if (Gvar.list_loop_track != null)
                                {
                                    Gvar.list_loop_track.Add(item);
                                }
                                else
                                {
                                    Gvar.list_loop_track = lava_player.Queue.Items.ToList();
                                }
                            }
                            else
                            {
                                await lava_player.PlayAsync(item);
                                Gvar.loop_track = item;
                            }
                        }

                        await now_async(default);
                        await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                        .WithColor(Color.Green)
                                        .WithDescription($"{results.PlaylistInfo.Name} playlist has been added to the queue")
                                        .WithCurrentTimestamp()
                                        .Build());

                        Gvar.playlist_load_flag = false;
                        return;
                    }
                    else
                    {
                        results = await lava_rest_client.SearchYouTubeAsync(search);
                    }
                }
                else if (type == "SC")
                {
                    string[] collection = search.Split('/');
                    results = await lava_rest_client.SearchSoundcloudAsync(collection.LastOrDefault());
                }
                else if (type == "SP")
                {
                    await get_access();

                    if (search.Contains("https://open.spotify.com/playlist/"))
                    {
                        string[] collection = search.Split('/');

                        string[] spotify_id = collection[collection.Count() - 1].Split("?si=");

                        FullPlaylist sp_playlist = _spotify.GetPlaylist(spotify_id[0], fields: "", market: "");

                        if (sp_playlist.Tracks.Total > 200)
                        {
                            await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                            .WithColor(Color.Green)
                                            .WithDescription("Cannot add a playlist with more than 200 songs in it")
                                            .WithCurrentTimestamp()
                                            .Build());
                            return;
                        }

                        var temp_msg = await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                            .WithColor(Color.Green)
                                            .WithDescription($"Adding {sp_playlist.Owner.DisplayName} playlist to the queue. Please wait.")
                                            .WithCurrentTimestamp()
                                            .Build());

                        Gvar.playlist_load_flag = true;

                        int load_count = 0;

                        foreach (var sp_item in sp_playlist.Tracks.Items)
                        {
                            if (load_count % 5 == 0)
                            {
                                if (load_count > 0)
                                {
                                    await temp_msg.DeleteAsync();
                                    temp_msg = await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                            .WithColor(Color.Green)
                                            .WithDescription($"{load_count} / {sp_playlist.Tracks.Items.Count()} tracks loaded.")
                                            .WithCurrentTimestamp()
                                            .Build());
                                }
                            }

                            results = await lava_rest_client.SearchYouTubeAsync(sp_item.Track.Name + " " + sp_item.Track.Artists.FirstOrDefault().Name);

                            if (results.LoadType == LoadType.NoMatches
                                || results.LoadType == LoadType.LoadFailed
                                || results.Tracks.Count() == 0)
                            {
                                load_count++;
                                continue;
                            }

                            if (lava_player.IsPlaying)
                            {
                                lava_player.Queue.Enqueue(results.Tracks.FirstOrDefault());

                                if (Gvar.list_loop_track != null)
                                {
                                    Gvar.list_loop_track.Add(results.Tracks.FirstOrDefault());
                                }
                                else
                                {
                                    Gvar.list_loop_track = lava_player.Queue.Items.ToList();
                                }
                            }
                            else
                            {
                                await lava_player.PlayAsync(results.Tracks.FirstOrDefault());
                                Gvar.loop_track = results.Tracks.FirstOrDefault();
                            }
                            load_count++;
                        }

                        await temp_msg.DeleteAsync();
                        await now_async(default);
                        await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                            .WithColor(Color.Green)
                                            .WithDescription($"{sp_playlist.Owner.DisplayName} playlist has been added to the queue")
                                            .WithCurrentTimestamp()
                                            .Build());

                        Gvar.playlist_load_flag = false;
                        return;
                    }
                    else if (search.Contains("https://open.spotify.com/album/"))
                    {
                        string[] collection = search.Split('/');

                        string[] spotify_id = collection[collection.Count() - 1].Split("?si=");

                        FullAlbum sp_album = _spotify.GetAlbum(spotify_id[0], market: "");

                        if (sp_album.Tracks.Total > 200)
                        {
                            await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                            .WithColor(Color.Green)
                                            .WithDescription("Cannot add an album with more than 200 songs in it")
                                            .WithCurrentTimestamp()
                                            .Build());
                            return;
                        }

                        var temp_msg = await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                            .WithColor(Color.Green)
                                            .WithDescription($"Adding {sp_album.Name} album to the queue. Please wait.")
                                            .WithCurrentTimestamp()
                                            .Build());

                        Gvar.playlist_load_flag = true;

                        int load_count = 0;

                        foreach (var sp_item in sp_album.Tracks.Items)
                        {
                            if (load_count % 5 == 0)
                            {
                                if (load_count > 0)
                                {
                                    await temp_msg.DeleteAsync();
                                    temp_msg = await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                            .WithColor(Color.Green)
                                            .WithDescription($"{load_count} / {sp_album.Tracks.Items.Count()} tracks loaded.")
                                            .WithCurrentTimestamp()
                                            .Build());
                                }
                            }

                            results = await lava_rest_client.SearchYouTubeAsync(sp_item.Name + " " + sp_item.Artists.FirstOrDefault().Name);

                            if (results.LoadType == LoadType.NoMatches
                                || results.LoadType == LoadType.LoadFailed
                                || results.Tracks.Count() == 0)
                            {
                                load_count++;
                                continue;
                            }

                            if (lava_player.IsPlaying)
                            {
                                lava_player.Queue.Enqueue(results.Tracks.FirstOrDefault());

                                if (Gvar.list_loop_track != null)
                                {
                                    Gvar.list_loop_track.Add(results.Tracks.FirstOrDefault());
                                }
                                else
                                {
                                    Gvar.list_loop_track = lava_player.Queue.Items.ToList();
                                }
                            }
                            else
                            {
                                await lava_player.PlayAsync(results.Tracks.FirstOrDefault());
                                Gvar.loop_track = results.Tracks.FirstOrDefault();
                            }
                            load_count++;
                        }

                        await temp_msg.DeleteAsync();
                        await now_async(default);
                        await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                            .WithColor(Color.Green)
                                            .WithDescription($"{sp_album.Name} album has been added to the queue")
                                            .WithCurrentTimestamp()
                                            .Build());

                        Gvar.playlist_load_flag = false;
                        return;
                    }
                    else if (search.Contains("https://open.spotify.com/track/"))
                    {
                        string[] collection = search.Split('/');

                        string[] spotify_id = collection[collection.Count() - 1].Split("?si=");

                        FullTrack sp_track = _spotify.GetTrack(spotify_id[0], market: "");

                        results = await lava_rest_client.SearchYouTubeAsync(sp_track.Artists.FirstOrDefault().Name + " " + sp_track.Name);
                    }
                }

                if (results.LoadType == LoadType.NoMatches
                    || results.LoadType == LoadType.LoadFailed
                    || results.Tracks.Count() == 0)
                {
                    await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                        .WithColor(Color.Green)
                                        .WithDescription("No matches found. try more specific")
                                        .WithCurrentTimestamp()
                                        .Build());

                    return;
                }

                var track = results.Tracks.FirstOrDefault();

                if (lava_player.IsPlaying)
                {
                    lava_player.Queue.Enqueue(track);

                    if (Gvar.list_loop_track != null)
                    {
                        Gvar.list_loop_track.Add(track);
                    }
                    else
                    {
                        Gvar.list_loop_track = lava_player.Queue.Items.ToList();
                    }

                    await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                        .WithColor(Color.Green)
                                        .WithDescription($"{track.Title} has been added to the queue")
                                        .WithCurrentTimestamp()
                                        .Build());

                }
                else
                {
                    await lava_player.PlayAsync(track);
                    Gvar.loop_track = track;
                    await now_async(default);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                await channel.SendMessageAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription($"Error , {ex.Message}")
                    .WithCurrentTimestamp()
                    .Build());

                return;
            }
        }

        //remove at
        public async Task remove_async(int index, SocketVoiceChannel voice_channel, ITextChannel text_channel)
        {
            if (lava_player == null)
            {
                await text_channel.SendMessageAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription("There are no track playing at this time.")
                    .WithCurrentTimestamp()
                    .Build());

                return;
            }

            if (lava_player.VoiceChannel != voice_channel)
            {
                await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription("Please join the voice channel the bot is in to remove track.")
                    .WithCurrentTimestamp()
                    .Build());

                return;
            }

            if (lava_player.Queue.Count == 0
                && lava_player.CurrentTrack == null
                && Gvar.loop_track == null
                && Gvar.list_loop_track.Count == 0)
            {
                await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription("Queue is empty.")
                    .WithCurrentTimestamp()
                    .Build());

                return;
            }

            try
            {
                if (Gvar.loop_flag)
                {
                    if (index == 1)
                    {
                        try
                        {
                            if (Gvar.loop_track == null)
                            {
                                if (lava_player.CurrentTrack == Gvar.list_loop_track.FirstOrDefault())
                                {
                                    await lava_player.SkipAsync();
                                }
                            }
                            else
                            {
                                if (lava_player.CurrentTrack == Gvar.loop_track)
                                {
                                    await lava_player.SkipAsync();
                                }
                            }
                        }
                        finally
                        {
                            if (Gvar.loop_track == null)
                            {
                                Gvar.list_loop_track.RemoveAt(0);
                            }
                            else
                            {
                                Gvar.loop_track = null;
                            }

                            if (Gvar.first_track == 0)
                            {
                                Gvar.first_track = 1;
                            }
                        }
                    }
                    else
                    {
                        var ele = Gvar.list_loop_track.ElementAtOrDefault(index + Gvar.first_track - 2);

                        if (index + Gvar.first_track - 2 == -1)
                        {
                            await lava_player.SkipAsync();
                        }
                        else if (index + Gvar.first_track - 2 >= 0)
                        {
                            if (lava_player.Queue.Count > 0)
                            {
                                lava_player.Queue.Remove(ele);
                            }
                        }

                        Gvar.list_loop_track.Remove(ele);
                    }
                }
                else
                {
                    if (true)
                    {
                        if (lava_player.Queue.Count == 0)
                        {
                            await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                                .WithColor(Color.Green)
                                .WithDescription("Queue is empty.")
                                .WithCurrentTimestamp()
                                .Build());

                            return;
                        }
                    }

                    var removed_track = lava_player.Queue.RemoveAt(index - 1);
                }

                await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription($"Selected track has been removed")
                    .WithCurrentTimestamp()
                    .Build());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await lava_player.TextChannel.SendMessageAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription($"Error while removing track , {ex.Message}")
                    .WithCurrentTimestamp()
                    .Build());

                return;
            }
        }

        //lyric
        public async Task<string> lyric_async()
        {
            try
            {
                if (lava_player == null)
                {
                    return "There are no track playing at this time.";
                }

                if (!lava_player.IsPlaying)
                {
                    return "There are no track playing at this time.";
                }

                var lyric = await lava_player.CurrentTrack.FetchLyricsAsync();

                if (lyric == "" || lyric == null)
                {
                    return "Can't find lyric.";
                }

                var embed = new EmbedBuilder
                {
                    Title = $"By : {lava_player.CurrentTrack.Author}\n" +
                            $"Title : {lava_player.CurrentTrack.Title}"
                };

                if (lyric.ToCharArray().Count() >= 2048)
                {
                    int embed_page = 0;
                    List<string> lyric_list = new List<string>();
                    string lyric_array = "";

                    for (int i = 0; i < lyric.ToCharArray().Count(); i++)
                    {
                        lyric_array += lyric.ToCharArray().ElementAtOrDefault(i);
                        if (i > 0)
                        {
                            if (i % 2047 == 0)
                            {
                                embed_page++;
                                lyric_list.Add(lyric_array);
                                lyric_array = "";
                            }
                        }
                    }

                    for (int i = 0; i < embed_page; i++)
                    {
                        await lava_player.TextChannel
                        .SendMessageAsync(default, default, embed
                        .WithColor(Color.Green)
                        .WithDescription(lyric_list.ElementAtOrDefault(i))
                        .WithCurrentTimestamp()
                        .Build());
                    }
                }
                else
                {
                    await lava_player.TextChannel
                    .SendMessageAsync(default, default, embed
                    .WithColor(Color.Green)
                    .WithDescription(lyric)
                    .WithCurrentTimestamp()
                    .Build());
                }

                return "Lyrics loaded";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return $"Error , {ex.Message}";
            }
        }

        //now
        public async Task now_async(ITextChannel text_channel)
        {
            try
            {
                var return_embed = new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription("There are no track playing at this time.")
                    .WithCurrentTimestamp()
                    .Build();

                if (lava_player == null)
                {
                    await text_channel.SendMessageAsync(default, default, return_embed);
                    return;
                }

                if (!lava_player.IsPlaying)
                {
                    await lava_player.TextChannel.SendMessageAsync(default, default, return_embed);
                    return;
                }

                var thumbnail = await lava_player.CurrentTrack.FetchThumbnailAsync();

                var current_track_author = lava_player.CurrentTrack.Author;
                var current_track_title = lava_player.CurrentTrack.Title;
                var current_track_length = lava_player.CurrentTrack.Length;
                var current_track_url = lava_player.CurrentTrack.Uri;
                string desc = null;

                if (current_track_url.ToString().ToUpper().Contains("YOUTUBE"))
                {
                    desc = "Youtube";
                }
                else if (current_track_url.ToString().ToUpper().Contains("SOUNDCLOUD"))
                {
                    desc = "Soundcloud";
                }
                var current_hour = lava_player.CurrentTrack.Position.Hours;
                var current_minute = lava_player.CurrentTrack.Position.Minutes;
                var current_second = lava_player.CurrentTrack.Position.Seconds;

                var hour = lava_player.CurrentTrack.Length.Hours;
                var minute = lava_player.CurrentTrack.Length.Minutes;
                var second = lava_player.CurrentTrack.Length.Seconds;

                string s_hour = lava_player.CurrentTrack.Length.Hours.ToString(),
                    s_minute = lava_player.CurrentTrack.Length.Minutes.ToString(),
                    s_second = lava_player.CurrentTrack.Length.Seconds.ToString();

                string s_hour_current = lava_player.CurrentTrack.Position.Hours.ToString(),
                    s_minute_current = lava_player.CurrentTrack.Position.Minutes.ToString(),
                    s_second_current = lava_player.CurrentTrack.Position.Seconds.ToString();

                if (hour < 10)
                {
                    s_hour = "0" + hour.ToString();
                }

                if (minute < 10)
                {
                    s_minute = "0" + minute.ToString();
                }

                if (second < 10)
                {
                    s_second = "0" + second.ToString();
                }

                if (current_hour < 10)
                {
                    s_hour_current = "0" + current_hour.ToString();
                }

                if (current_minute < 10)
                {
                    s_minute_current = "0" + current_minute.ToString();
                }

                if (current_second < 10)
                {
                    s_second_current = "0" + current_second.ToString();
                }

                var embed = new EmbedBuilder
                {
                    Description = $"By : {current_track_author}" +
                                  $"\nSource : {desc}" +
                                  $"\nVolume : {lava_player.CurrentVolume}"
                };
                var ready = embed.AddField("Duration",
                    s_hour_current + ":" +
                    s_minute_current + ":" +
                    s_second_current +
                    " / " +
                    s_hour + ":" +
                    s_minute + ":" +
                    s_second)
                    .WithAuthor("Now Playing")
                    .WithColor(Color.Green)
                    .WithTitle(current_track_title)
                    .WithUrl(current_track_url.ToString())
                    .WithThumbnailUrl(thumbnail)
                    .WithCurrentTimestamp()
                    .Build();

                await lava_player.TextChannel.SendMessageAsync(default, default, ready);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                await text_channel.SendMessageAsync(default, default, new EmbedBuilder()
                    .WithColor(Color.Green)
                    .WithDescription(ex.Message)
                    .WithCurrentTimestamp()
                    .Build());
                return;
            }
        }

        //queue
        public Embed queue_async(int? input_page)
        {
            try
            {
                string now_playing_title = "";

                if (lava_player == null)
                {
                    var ready = new EmbedBuilder()
                        .WithAuthor("Queue")
                        .WithDescription("```bash\n\"Now Playing\"\n```\n"
                        + $"```There are no currently playing track right now```"
                        + "\n\n```bash\n\"Queue List\"\n```\n"
                        + "```"
                        + "There are no more tracks in the queue"
                        + "```")
                        .WithColor(Color.Green)
                        .WithFooter($"Loop Status : {Gvar.loop_flag.ToString()}\n" + $"There are total {0} tracks in the queue")
                        .WithCurrentTimestamp()
                        .Build();

                    return ready;
                }

                if (!lava_player.IsPlaying)
                {
                    now_playing_title = "There are no currently playing track right now";
                }
                else
                {
                    now_playing_title = lava_player.CurrentTrack.Title;
                }

                string queue_string = "";
                int queue_count = 0;
                var queue_list = lava_player.Queue.Items.ToList();

                if (lava_player.Queue.Count == 0)
                {
                    if (Gvar.loop_flag == true)
                    {
                        queue_string = "";
                    }
                    else
                    {
                        queue_string = "There are no more tracks in the queue";
                    }
                }

                if (Gvar.loop_flag == false)
                {
                    int page = 0;
                    int queue_index = 0;
                    int queue_track_index = 0;
                    LavaTrack[,] queue_list_array = new LavaTrack[1000, 10];

                    foreach (var queue_item in queue_list)
                    {
                        queue_list_array[queue_index, queue_track_index] = queue_item as LavaTrack;
                        if (queue_track_index == 9)
                        {
                            queue_index++;
                            queue_track_index = 0;
                        }
                        else
                        {
                            queue_track_index++;
                        }
                    }

                    if (!(input_page is null))
                    {
                        if (input_page <= 0)
                        {
                            return new EmbedBuilder()
                                .WithColor(Color.Green)
                                .WithDescription("Please input the correct page number")
                                .Build();
                        }

                        if (input_page > queue_index + 1)
                        {
                            return new EmbedBuilder()
                                .WithColor(Color.Green)
                                .WithDescription("Please input the correct page number")
                                .Build();
                        }

                        page = (int)input_page - 1;
                    }

                    for (int i = 0; i <= 9; i++)
                    {
                        if (queue_list_array[page, i] is null)
                        {
                            continue;
                        }
                        var next_track = queue_list_array[page, i];
                        queue_string += $"{(page * 10) + queue_count + 1}. " + $"{next_track.Title}\n\n";
                        queue_count++;
                    }

                    if (lava_player.Queue.Items.ToList().Count() == 0)
                    {
                        queue_string = "There are no more tracks in the queue";
                    }

                    var ready = new EmbedBuilder()
                        .WithAuthor("Queue")
                        .WithDescription(
                        "```bash\n\"Now Playing\"\n```\n" +
                        $"```{now_playing_title}```" +
                        "\n\n```bash\n\"Queue List\"\n```\n" +
                        "```" +
                        queue_string +
                        "```")
                        .WithColor(Color.Green)
                        .WithFooter(
                        $"Loop Status : {Gvar.loop_flag.ToString()}\n" +
                        $"Current Page : {(page + 1).ToString()} / {(queue_index + 1).ToString()}\n" +
                        $"There are total {queue_list.Count()} tracks in the queue"
                        )
                        .WithCurrentTimestamp()
                        .Build();

                    return ready;
                }
                else
                {
                    queue_count = 1;
                    queue_list = Gvar.list_loop_track;

                    if (lava_player.Queue.Items.ToList().Count() == 0 && queue_list.Count() == 0 && Gvar.loop_track == null)
                    {
                        queue_string = "There are no more tracks in the queue";
                    }

                    int page = 0;
                    int queue_index = 0;
                    int queue_track_index = 0;
                    LavaTrack[,] queue_list_array = new LavaTrack[1000, 10];

                    foreach (var queue_item in queue_list)
                    {
                        queue_list_array[queue_index, queue_track_index] = queue_item as LavaTrack;
                        if (queue_track_index == 9)
                        {
                            queue_index++;
                            queue_track_index = 0;
                        }
                        else
                        {
                            queue_track_index++;
                        }
                    }

                    if (!(input_page is null))
                    {
                        if (input_page <= 0)
                        {
                            return new EmbedBuilder()
                                .WithColor(Color.Green)
                                .WithDescription("Please input the correct page number")
                                .Build();
                        }

                        if (input_page > queue_index + 1)
                        {
                            return new EmbedBuilder()
                                .WithColor(Color.Green)
                                .WithDescription("Please input the correct page number")
                                .Build();
                        }

                        page = (int)input_page - 1;
                    }

                    if (page == 0)
                    {
                        //halaman pertama
                        if (Gvar.loop_track != null)
                        {
                            queue_string += $"{queue_count}. " + $"{Gvar.loop_track.Title}\n\n";
                        }
                        else
                        {
                            queue_count = 0;
                        }

                        for (int i = 0; i <= 8; i++)
                        {
                            if (queue_list_array[page, i] is null)
                            {
                                continue;
                            }
                            var next_track = queue_list_array[page, i];
                            queue_string += $"{(page * 10) + queue_count + 1}. " + $"{next_track.Title}\n\n";
                            queue_count++;
                        }
                    }
                    else
                    {
                        var next_track = queue_list_array[page - 1, 9];
                        queue_string += $"{(page * 10) + queue_count}. " + $"{next_track.Title}\n\n";
                        queue_count++;
                        //halaman selanjutnya
                        for (int i = 0; i <= 8; i++)
                        {
                            if (queue_list_array[page, i] is null)
                            {
                                continue;
                            }
                            next_track = queue_list_array[page, i];
                            queue_string += $"{(page * 10) + queue_count}. " + $"{next_track.Title}\n\n";
                            queue_count++;
                        }
                    }

                    int plus_satu = 0;

                    if (lava_player.CurrentTrack != null)
                    {
                        plus_satu = 1;
                    }

                    var ready = new EmbedBuilder()
                        .WithAuthor("Queue")
                        .WithDescription(
                        "```bash\n\"Now Playing\"\n```\n" +
                        $"```{now_playing_title}```" +
                        "\n\n```bash\n\"Looping Queue List\"\n```\n" +
                        "```" +
                        queue_string +
                        "```")
                        .WithColor(Color.Green)
                        .WithFooter(
                        $"Loop Status : {Gvar.loop_flag.ToString()}\n" +
                        $"Current Page : {(page + 1).ToString()} / {(queue_index + 1).ToString()}\n" +
                        $"There are total {queue_list.Count() + plus_satu} tracks in the queue"
                        )
                        .WithCurrentTimestamp()
                        .Build();

                    return ready;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new EmbedBuilder()
                .WithColor(Color.Green)
                .WithDescription($"Error , {ex.Message} try restarting the player or Contact PakPres#8360 asap")
                .Build();
            }
        }

        //clear
        public async Task<string> clear_not_async(SocketVoiceChannel voice_channel)
        {
            try
            {
                if (lava_player == null)
                {
                    return "There are no track playing at this time.";
                }

                if (lava_player.VoiceChannel != voice_channel)
                {
                    return "Please join the voice channel the bot is in to clear tracks.";
                }

                await lava_player.StopAsync();
                lava_player.Queue.Clear();
                lava_player = null;
                clear_all_loop();

                return "Tracks cleared.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return $"Error , {ex.Message}";
            }
        }

        //skip
        public async Task<string> skip_async(SocketVoiceChannel voice_channel)
        {
            try
            {
                if (lava_player == null)
                {
                    return "I need to be connected to a channel first";
                }

                if (lava_player.VoiceChannel != voice_channel)
                {
                    return "Please join the voice channel the bot is in to skip track.";
                }

                var old_track = lava_player.CurrentTrack;
                if (lava_player.IsPlaying && lava_player.Queue.Count == 0)
                {
                    await lava_player.StopAsync();
                    return $"Successfully skipped {old_track.Title}";
                }

                if (lava_player == null || lava_player.Queue.Count == 0)
                {
                    return "Nothing in queue";
                }


                if (Gvar.toggle_auto)
                {
                    await lava_player.StopAsync();
                }
                else
                {
                    await lava_player.SkipAsync();
                }

                return $"Successfully skipped {old_track.Title}";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return $"Error , {ex.Message}";
            }
        }

        //volume adjustment
        public async Task<string> set_volume_async(int vol, SocketVoiceChannel voice_channel)
        {
            try
            {
                if (lava_player == null)
                {
                    return "I need to be connected to a channel first";
                }

                if (lava_player.VoiceChannel != voice_channel)
                {
                    return "Please join the voice channel the bot is in to set player volume.";
                }

                if (vol < 0 || vol > 100)
                {
                    return "Volume must between 0 - 100";
                }

                await lava_player.SetVolumeAsync(vol);
                return $"Volume set to {vol}";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return $"Error , {ex.Message}";
            }
        }

        //volume earrape
        public async Task<string> set_Earrape(SocketVoiceChannel voice_channel)
        {
            try
            {
                if (lava_player == null)
                {
                    return "I need to be connected to a channel first";
                }

                if (lava_player.VoiceChannel != voice_channel)
                {
                    return "Please join the voice channel the bot is in to EARRAPE!!!!.";
                }

                await lava_player.SetVolumeAsync(1000);
                return "Earraping y'all";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return $"Error , {ex.Message}";
            }
        }

        //pause
        public async Task<string> pause_async(SocketVoiceChannel voice_channel)
        {
            try
            {
                if (lava_player == null)
                {
                    return "There are no track playing at this time.";
                }

                if (lava_player.VoiceChannel != voice_channel)
                {
                    return "Please join the voice channel the bot is in to pause the player.";
                }

                if (lava_player.IsPaused == true)
                {
                    return "Track already paused.";
                }

                await lava_player.PauseAsync();
                return "Player Paused.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return $"Error , {ex.Message}";
            }
        }

        //resume
        public async Task<string> resume_async(SocketVoiceChannel voice_channel)
        {
            try
            {
                if (lava_player == null)
                {
                    return "There are no track playing at this time.";
                }

                if (lava_player.VoiceChannel != voice_channel)
                {
                    return "Please join the voice channel the bot is in to resume the player.";
                }

                if (lava_player.IsPaused != true)
                {
                    return "Track still playing.";
                }

                await lava_player.ResumeAsync();
                return "Track resumed.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return $"Error , {ex.Message}";
            }
        }

        //shuffle
        public string shuffle_async(SocketVoiceChannel voice_channel)
        {
            try
            {
                if (lava_player == null)
                {
                    return "There are no track playing at this time.";
                }

                if (lava_player.VoiceChannel != voice_channel)
                {
                    return "Please join the voice channel the bot is in to shuffle the queue.";
                }

                if (Gvar.loop_flag)
                {
                    lava_player.Queue.Shuffle();
                }
                else
                {
                    lava_player.Queue.Shuffle();
                    Gvar.list_loop_track = lava_player.Queue.Items.ToList();
                }

                return "Track shuffled.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return $"Error , {ex.Message}";
            }
        }

        private Task Lava_socket_client_Log(LogMessage Logmessage)
        {
            Console.WriteLine(Logmessage.Message);
            return Task.CompletedTask;
        }

        private async Task Maicy_client_Ready_async()
        {
            await lava_socket_client.StartAsync(maicy_client, new Configuration()
            {
                AutoDisconnect = false
            });
        }
    }
}
