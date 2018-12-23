using System;

namespace ConsoleArgumentParser
{
    public class CommandAttribute : Attribute
    {
        public string Name { get; set; }
        
        public string Description { get; set; }

        public CommandAttribute(string name)
        {
            Name = name;
        }

        public CommandAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}