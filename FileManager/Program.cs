using System;
using Thuja;
using Thuja.Widgets;

namespace FileManager
{
    class Program
    {
        static void Main(string[] args)
        {
            new MainLoop(
                new Label("Hello world!")
            ).Start();
        }
    }
}