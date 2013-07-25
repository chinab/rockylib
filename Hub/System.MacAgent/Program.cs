using System;

namespace System.MacAgent
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Hello World!");
			Hub.LogDebug ("Hello World!");
			Console.Read ();
		}
	}
}