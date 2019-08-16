using System;

namespace ConsoleArgumentParser
{
    public class CommandArgumentAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }

        public CommandArgumentAttribute(string name, string description = "")
        {
            Name = name;
            Description = description;
        }
    }
}