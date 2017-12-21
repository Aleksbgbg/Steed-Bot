namespace Steed.Bot.Commands.Admin
{
    using System.Threading.Tasks;

    internal class AdminCommandActions
    {
#if !DEBUG
        private const ulong UpdateAnnouncementChannel = 389408309532164096;
#else
        private const ulong UpdateAnnouncementChannel = 370522989894303744; // Bot Testing channel, for testing and debugging
#endif // !DEBUG

        [Command("announce update")]
        public async Task AnnounceUpdate(CommandContext context) => await (await context.DiscordClient.GetChannelAsync(UpdateAnnouncementChannel)).SendMessageAsync($"@everyone\n**A new update of Steed is available now! Open Steed and the update will be installed.**\n{context.Bot.DownloadFile("updatelog.txt")}");
    }
}