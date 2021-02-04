using Discord;
using Discord.WebSocket;
using PartyBot.Handlers;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Victoria;
using Victoria.EventArgs;
using Victoria.Enums;
using Victoria.Responses.Rest;

namespace PartyBot.Services
{
    public sealed class LavaLinkAudio
    {
        private readonly LavaNode _lavaNode;
        bool isLoop = false;
        bool isShuffle = false;
        int max = 0;
        SearchResponse Search;

        public LavaLinkAudio(LavaNode lavaNode)
            => _lavaNode = lavaNode;

        public async Task<Embed> JoinAsync(IGuild guild, IVoiceState voiceState, ITextChannel textChannel)
        {
            if (_lavaNode.HasPlayer(guild))
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Join", "I'm already connected to a voice channel!");
            }

            if (voiceState.VoiceChannel is null)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Join", "You must be connected to a voice channel!");
            }

            try
            {
                await _lavaNode.JoinAsync(voiceState.VoiceChannel, textChannel);
                return await EmbedHandler.CreateBasicEmbed("Music, Join", $"Joined {voiceState.VoiceChannel.Name}.", Color.Green);
            }
            catch (Exception ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Join", ex.Message);
            }
        }

        /*This is ran when a user uses either the command Join or Play
            I decided to put these two commands as one, will probably change it in future. 
            Task Returns an Embed which is used in the command call.. */
        public async Task<Embed> PlayAsync(SocketGuildUser user, IGuild guild, string query)
        {
            //Check If User Is Connected To Voice Cahnnel.
            if (user.VoiceChannel == null)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Join/Play", "You Must First Join a Voice Channel.");
            }

            //Check the guild has a player available.
            if (!_lavaNode.HasPlayer(guild))
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Play", "I'm not connected to a voice channel.");
            }

            try
            {
                //Get the player for that guild.
                var player = _lavaNode.GetPlayer(guild);

                Search = Uri.IsWellFormedUriString(query, UriKind.Absolute) ?
                    await _lavaNode.SearchAsync(query)
                    : await _lavaNode.SearchYouTubeAsync(query);

                //If we couldn't find anything, tell the user.
                if (Search.LoadStatus == LoadStatus.NoMatches)
                {
                    return await EmbedHandler.CreateErrorEmbed("Music", $"I wasn't able to find anything for {query}.");
                }

                //Get the first track from the search results.
                //TODO: Add a 1-5 list for the user to pick from. (Like Fredboat)
                var tracks = "";
                max = (int)MathF.Min(11, Search.Tracks.Count);
                for (int i = 0; i < max; i++)
                {
                    tracks += $"{i + 1}: {Search.Tracks.ElementAtOrDefault(i).Title}\n";
                }
                return await EmbedHandler.CreateBasicEmbed($"총 {max}개의 노래를 찾았어", $"{tracks}" +
                    $"\n\n 노래는 \"{GlobalData.Config.DefaultPrefix}p 숫자\"로 선택할 수 있어", Color.Blue);
            }

            //If after all the checks we did, something still goes wrong. Tell the user about it so they can report it back to us.
            catch (Exception ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Play", ex.Message);
            }

        }
        public async Task<Embed> ChoosePlayAsync(SocketGuildUser user, IGuild guild, string select)
        {
            var _select = int.Parse(select);
            //Check If User Is Connected To Voice Cahnnel.
            if (user.VoiceChannel == null)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Join/Play", "You Must First Join a Voice Channel.");
            }

            //Check the guild has a player available.
            if (!_lavaNode.HasPlayer(guild))
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Play", "I'm not connected to a voice channel.");
            }

            if(Search.Tracks == null)
            {
                return await EmbedHandler.CreateErrorEmbed("Music", $"먼저 노래를 검색해줘");
            }

            if (Search.Tracks.Count == 0)
            {
                return await EmbedHandler.CreateErrorEmbed("Music", $"검색된 노래가 없어");
            }

            if(_select > Search.Tracks.Count || _select > max)
            {
                return await EmbedHandler.CreateErrorEmbed("Music", $"목록에 존재하지 않는 노래야");
            }

            try
            {
                var player = _lavaNode.GetPlayer(guild);
                LavaTrack track = Search.Tracks.ElementAtOrDefault(_select - 1);

                //If the Bot is already playing music, or if it is paused but still has music in the playlist, Add the requested track to the queue.
                if (player.Track != null && player.PlayerState is PlayerState.Playing || player.PlayerState is PlayerState.Paused)
                {
                    player.Queue.Enqueue(track);
                    await LoggingService.LogInformationAsync("Music", $"{track.Title} has been added to the music queue.");
                    return await EmbedHandler.CreateBasicEmbed("Music", $"{track.Title} has been added to queue.", Color.Blue);
                }

                //Player was not playing anything, so lets play the requested track.
                await player.PlayAsync(track);
                await LoggingService.LogInformationAsync("Music", $"Bot Now Playing: {track.Title}\nUrl: {track.Url}");
                return await EmbedHandler.CreateBasicEmbed("Music", $"\"{track.Title}\"를 재생하고 있어\nUrl: {track.Url}", Color.Blue);
            }
            //Tell the user about the error so they can report it back to us.
            catch (InvalidOperationException ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Leave", ex.Message);
            }
        }

        /*This is ran when a user uses the command Leave.
            Task Returns an Embed which is used in the command call. */
        public async Task<Embed> LeaveAsync(IGuild guild)
        {
            try
            {
                //Get The Player Via GuildID.
                var player = _lavaNode.GetPlayer(guild);

                //if The Player is playing, Stop it.
                if (player.PlayerState is PlayerState.Playing)
                {
                    await player.StopAsync();
                }

                //Leave the voice channel.
                await _lavaNode.LeaveAsync(player.VoiceChannel);

                await LoggingService.LogInformationAsync("Music", $"Bot has left.");
                return await EmbedHandler.CreateBasicEmbed("Music", $"I've left. Thank you for playing moosik.", Color.Blue);
            }
            //Tell the user about the error so they can report it back to us.
            catch (InvalidOperationException ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Leave", ex.Message);
            }
        }

        /*This is ran when a user uses the command List 
            Task Returns an Embed which is used in the command call. */
        public async Task<Embed> ListAsync(IGuild guild)
        {
            try
            {
                /* Create a string builder we can use to format how we want our list to be displayed. */
                var descriptionBuilder = new StringBuilder();

                /* Get The Player and make sure it isn't null. */
                var player = _lavaNode.GetPlayer(guild);
                if (player == null)
                    return await EmbedHandler.CreateErrorEmbed("Music, List", $"Could not aquire player.\nAre you using the bot right now? check{GlobalData.Config.DefaultPrefix}Help for info on how to use the bot.");

                if (player.PlayerState is PlayerState.Playing)
                {
                    /*If the queue count is less than 1 and the current track IS NOT null then we wont have a list to reply with.
                        In this situation we simply return an embed that displays the current track instead. */
                    if (player.Queue.Count < 1 && player.Track != null)
                    {
                        return await EmbedHandler.CreateBasicEmbed($"Now Playing: {player.Track.Title}", "Nothing Else Is Queued.", Color.Blue);
                    }
                    else
                    {
                        /* Now we know if we have something in the queue worth replying with, so we itterate through all the Tracks in the queue.
                         *  Next Add the Track title and the url however make use of Discords Markdown feature to display everything neatly.
                            This trackNum variable is used to display the number in which the song is in place. (Start at 2 because we're including the current song.*/
                        var trackNum = 2;
                        foreach (LavaTrack track in player.Queue)
                        {
                            descriptionBuilder.Append($"{trackNum}: [{track.Title}]({track.Url}) - {track.Id}\n");
                            trackNum++;
                        }
                        return await EmbedHandler.CreateBasicEmbed("Music Playlist", $"Now Playing: [{player.Track.Title}]({player.Track.Url}) \n{descriptionBuilder}", Color.Blue);
                    }
                }
                else
                {
                    return await EmbedHandler.CreateErrorEmbed("Music, List", "Player doesn't seem to be playing anything right now. If this is an error, Please Contact Draxis.");
                }
            }
            catch (Exception ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, List", ex.Message);
            }

        }

        /*This is ran when a user uses the command Skip 
            Task Returns an Embed which is used in the command call. */
        public async Task<Embed> SkipTrackAsync(IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);
                /* Check if the player exists */
                if (player == null)
                    return await EmbedHandler.CreateErrorEmbed("Music, List", $"Could not aquire player.\nAre you using the bot right now? check{GlobalData.Config.DefaultPrefix}Help for info on how to use the bot.");
                /* Check The queue, if it is less than one (meaning we only have the current song available to skip) it wont allow the user to skip.
                     User is expected to use the Stop command if they're only wanting to skip the current song. */
                try
                {
                    /* Save the current song for use after we skip it. */
                    var currentTrack = player.Track;
                    /* Skip the current song. */
                    await player.SeekAsync(currentTrack.Duration.Subtract(TimeSpan.FromSeconds(2)));
                    await LoggingService.LogInformationAsync("Music", $"Bot skipped: {currentTrack.Title}");
                    return await EmbedHandler.CreateBasicEmbed("Music Skip", $"I have successfully skiped {currentTrack.Title}", Color.Blue);
                }
                catch (Exception ex)
                {
                    return await EmbedHandler.CreateErrorEmbed("Music, Skip", ex.Message);
                }
            }
            catch (Exception ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Skip", ex.Message);
            }
        }

        /*This is ran when a user uses the command Stop 
            Task Returns an Embed which is used in the command call. */
        public async Task<Embed> StopAsync(IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);

                if (player == null)
                    return await EmbedHandler.CreateErrorEmbed("Music, List", $"Could not aquire player.\nAre you using the bot right now? check{GlobalData.Config.DefaultPrefix}Help for info on how to use the bot.");

                /* Check if the player exists, if it does, check if it is playing.
                     If it is playing, we can stop.*/
                if (player.PlayerState is PlayerState.Playing)
                {
                    await player.StopAsync();
                }

                await LoggingService.LogInformationAsync("Music", $"Bot has stopped playback.");
                return await EmbedHandler.CreateBasicEmbed("Music Stop", "I Have stopped playback & the playlist has been cleared.", Color.Blue);
            }
            catch (Exception ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music, Stop", ex.Message);
            }
        }

        /*This is ran when a user uses the command Volume 
            Task Returns a String which is used in the command call. */
        public async Task<string> SetVolumeAsync(IGuild guild, int volume)
        {
            if (volume > 150 || volume <= 0)
            {
                return $"Volume must be between 1 and 150.";
            }
            try
            {
                var player = _lavaNode.GetPlayer(guild);
                await player.UpdateVolumeAsync((ushort)volume);
                await LoggingService.LogInformationAsync("Music", $"Bot Volume set to: {volume}");
                return $"Volume has been set to {volume}.";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> PauseAsync(IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);
                if (!(player.PlayerState is PlayerState.Playing))
                {
                    await player.PauseAsync();
                    return $"There is nothing to pause.";
                }

                await player.PauseAsync();
                return $"**Paused:** {player.Track.Title}, what a bamboozle.";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> ShuffleAsync(IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);
                if (player.Queue.Count == 0)
                {
                    return $"섞을 트랙이 없어";
                }
                isShuffle = !isShuffle;
                if (isShuffle)
                {
                    player.Queue.Shuffle();
                    return $"이제부터 계속 트랙을 섞을 거야";
                }
                else
                {
                    return $"더 이상 트랙을 섞지 않을 거야";
                }
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
        }
        public async Task<string> LoopAsync(IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);
                if (player.Queue.Count == 0 && player.Track == null)
                {
                    return $"반복할 트랙이 없어";
                }
                isLoop = !isLoop;
                if (isLoop)
                {
                    return $"이제부터 트랙을 반복할 거야";
                }
                else
                {
                    return $"더 이상 트랙을 반복하지 않을 거야";
                }
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
        }
        public async Task<Embed> DeleteAsync(IGuild guild, string select)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);
                var _select = int.Parse(select) - 2;

                if (_select > player.Queue.Count - 1)
                {
                    return await EmbedHandler.CreateErrorEmbed("Music", $"PlayList에 존재하지 않는 노래야");
                }

                var track = player.Queue.ElementAt(_select) as LavaTrack;

                var EmbedText = $"[\"{track.Title}\"]({track.Url})";

                player.Queue.RemoveAt(_select);

                return await EmbedHandler.CreateBasicEmbed("삭제한 노래", EmbedText, Color.Blue);
            }
            catch (InvalidOperationException ex)
            {
                return await EmbedHandler.CreateErrorEmbed("Music", ex.Message);
            }
        }

        public async Task<string> ResumeAsync(IGuild guild)
        {
            try
            {
                var player = _lavaNode.GetPlayer(guild);

                if (player.PlayerState is PlayerState.Paused)
                { 
                    await player.ResumeAsync(); 
                }


                if (player.Queue.Count > 0)
                {
                    return $"**Resumed:** {player.Track.Title}";
                }
                else
                {
                    return "다시 재생할 트랙이 없어";
                }
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }
        }

        public async Task TrackEnded(TrackEndedEventArgs args)
        {

            if (isShuffle && args.Player.Queue.Count > 0)
            {
                args.Player.Queue.Shuffle();
            }

            if (isLoop && args.Player.Queue.Count > 0)
            {
                args.Player.Queue.Enqueue(args.Track);
            }

            if (!args.Reason.ShouldPlayNext())
            {
                return;
            }

            if (!args.Player.Queue.TryDequeue(out var queueable))
            {
                //await args.Player.TextChannel.SendMessageAsync("Playback Finished.");
                return;
            }

            if (!(queueable is LavaTrack track))
            {
                await args.Player.TextChannel.SendMessageAsync("Next item in queue is not a track.");
                return;
            }
            await args.Player.PlayAsync(track);
            await args.Player.TextChannel.SendMessageAsync(
                embed: await EmbedHandler.CreateBasicEmbed("Now Playing", $"[\"{track.Title}\"]({track.Url})", Color.Blue));
        }
    }
}
