using System;

namespace ConsoleArgumentParser
{
    public class CommandArgumentAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public CommandArgumentAttribute(string name, string description = "")
        {
            Name = name;
            Description = description;
        }
    }
}