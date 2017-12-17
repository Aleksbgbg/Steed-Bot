namespace Steed.Bot.Commands
{
    using System.Text.RegularExpressions;

    internal abstract class BaseCommand
    {
        private protected readonly Regex regex;

        private protected BaseCommand(string regex) => this.regex = new Regex(regex, RegexOptions.Compiled);
    }
}