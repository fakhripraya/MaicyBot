using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Victoria.Entities;
using YoutubeExplode.Models;

namespace maicy_bot_core.MiscData
{
    public static class Gvar
    {
        //Lava Player Loop Flag
        public static bool loop_flag { get; set; }
        public static LavaTrack loop_track { get; set; }
        public static List<Victoria.Queue.IQueueObject> list_loop_track { get; set; }
        public static int first_track { get; set; }

        //playlist
        public static bool playlist_load_flag { get; set; }

        //current user channel
        public static SocketChannel current_client_channel { get; set; }

        //current user text channel
        public static ITextChannel current_client_text_channel { get; set; }

        //toggle auto play
        public static bool toggle_auto { get; set; }
    }
}
