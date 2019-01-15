namespace ConsoleArgumentParser
{
    public delegate bool ParserDelegate<T>(string s, out T val);
}