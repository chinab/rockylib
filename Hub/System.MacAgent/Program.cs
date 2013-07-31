using System;
using System.Net;

namespace System.MacAgent
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine("Hello World!");
			var http = new HttpClient(new Uri("http://www.baidu.com"));
			Console.Write(http.GetResponse().GetResponseText());
		}
	}
}