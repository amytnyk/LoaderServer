using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoaderServer
{
	class Response
	{
		public string state;
		public string description;
		public string data;

		public Response(string state, string description, string data)
		{
			this.state = state;
			this.description = description;
			this.data = data;
		}
	}
	
	class SaveResponse
	{
		public string state;
		public bool last_chunk;
	}

	class Program
	{
		static void add()
		{
			while (true)
			{
				percentage++;
				Thread.Sleep(100);
			}
		}

		public static int percentage = 0;
		public static StringBuilder sb;
		public static string text;

		/*public void Handle(IAsyncResult result)
		{
			HttpListener listener = (HttpListener)result.AsyncState;
			try
			{
				HttpListenerContext context = listener.EndGetContext(result);
				StreamWriter writer = new StreamWriter(context.Response.OutputStream);
				writer.Write("<html><body>");
				writer.Write("This call was handled in " + Thread.CurrentThread.ManagedThreadId);
				Thread.Sleep(new TimeSpan(0, 0, 5));
				writer.Write("<br>");
				writer.Write("Done");
				writer.Write("</body></html>");
				writer.Close();
				context.Response.OutputStream.Close();
				context.Response.Close();
			}
			finally
			{
				listener.BeginGetContext(new AsyncCallback(Handle), listener);
			}
		}*/

		static void Main(string[] args)
		{
			if (!HttpListener.IsSupported)
			{
				Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
				return;
			}

			HttpListener listener = new HttpListener();
			listener.Prefixes.Add(@"http://localhost:8000/");

			StreamReader sr = new StreamReader("text.txt");
			text = sr.ReadToEnd();
			sr.Close();

			Thread t = new Thread(add);
			t.Start();

			//while (true)
			//{
			//	listener.BeginGetContext(new AsyncCallback(Handle), listener);
			//	break;
			//}

			sb = new StringBuilder();

			while (true)
			{
				listener.Start();
				HttpListenerContext context = listener.GetContext();
				HttpListenerRequest request = context.Request;
				HttpListenerResponse response = context.Response;
				response.Headers.Add("Access-Control-Allow-Origin", "*");
				response.Headers.Add("Access-Control-Allow-Methods", "POST, GET");
				response.ContentType = "application/x-www-form-urlencoded";
				if (request.QueryString["action"] == "save")
				{
					string new_text = request.QueryString["text"];
					sb.Append(new_text);
					if (request.QueryString["chunk"] == "last")
					{
						text = sb.ToString();
						Console.WriteLine("New text - " + sb);
						sb.Clear();
					}
					string JSONstring = JsonConvert.SerializeObject(new SaveResponse { state = "Saved", last_chunk = request.QueryString["chunk"] == "last" });
					string JSONPstring = string.Format("{0}({1});", request.QueryString["callback"], JSONstring);
					byte[] buffer = Encoding.UTF8.GetBytes(JSONPstring);
					response.ContentLength64 = buffer.Length;
					Stream output = response.OutputStream;
					output.Write(buffer, 0, buffer.Length);
				}
				else
				{
					Response response_obj = null;
					if (percentage > 100)
						response_obj = new Response("Loaded", "Loaded", text);
					else if (percentage > 60)
						response_obj = new Response("Loading", "Last preparations", Convert.ToString(percentage) + "%");
					else if (percentage > 30)
						response_obj = new Response("Loading", "Processing", Convert.ToString(percentage) + "%");
					else
						response_obj = new Response("Loading", "Downloading", Convert.ToString(percentage) + "%");
					string responseString = JsonConvert.SerializeObject(response_obj);
					byte[] buffer = Encoding.UTF8.GetBytes(responseString);
					response.ContentLength64 = buffer.Length;
					Stream output = response.OutputStream;
					output.Write(buffer, 0, buffer.Length);
					output.Close();
				}
			}
		}
	}
}
