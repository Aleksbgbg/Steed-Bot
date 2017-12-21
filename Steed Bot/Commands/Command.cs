namespace SteedBot.Commands
{
    using System;

    internal class Command : BaseCommand
    {
        private readonly string syntax;

        private readonly string description;

        internal Command(string name, string syntax, string description, string regex) : base(name, regex)
        {
            this.syntax = syntax;
            this.description = description;
        }

        internal string HelpText => $"`{Name}`: {description}\nSyntax:```{syntax}```";

        internal int ComputeLevensteinDistance(string source)
        {
            int[,] distances = new int[source.Length + 1, Name.Length + 1];

            if (source.Length == 0) return Name.Length;

            if (Name.Length == 0) return source.Length;

            for (int column = -1; column < source.Length; distances[++column, 0] = column)
            {
            }

            for (int row = -1; row < Name.Length; distances[0, ++row] = row)
            {
            }

            for (int column = 1; column <= source.Length; ++column)
                for (int row = 1; row <= Name.Length; ++row)
                {
                    int previousColumn = column - 1;
                    int previousRow = row - 1;

                    distances[column, row] = Math.Min(Math.Min(
                        distances[previousColumn, row] + 1,
                        distances[column, previousRow] + 1),
                        distances[previousColumn, previousRow] + (Name[previousRow] == source[previousColumn] ? 0 : 1)
                        );
                }

            return distances[source.Length, Name.Length];
        }
    }
}