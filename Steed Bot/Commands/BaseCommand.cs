namespace Steed.Bot.Commands
{
    using System.Text.RegularExpressions;

    internal abstract class BaseCommand
    {
        private protected readonly Regex regex;

        private protected BaseCommand(string name, string regex)
        {
            Name = name;
            this.regex = new Regex(regex, RegexOptions.Compiled);
        }

        internal string Name { get; }

        internal Match Match(string command) => regex.Match(command);
    }
}