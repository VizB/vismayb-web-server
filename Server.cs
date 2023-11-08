using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Vismay.WebServer {
	/// <summary>
	/// My web server
	/// </summary>
	public static class Server {
		private static HttpListener listener;
		public static  int MaxSimultaneousConnections = 20;
		private static Semaphore sem = new Semaphore(MaxSimultaneousConnections, MaxSimultaneousConnections);
		
		/// <summary>
		/// Starts the Web Server
		/// </summary>
		public static void Start()
		{
			List<IPAddress> localHostIPs = GetLocalHostIPs();
			listener = InitializeListener(localHostIPs);
			Start(listener);			
		}
		
		/// <summary>
		/// Begin listening to connections on a separate worker thread.
		/// </summary>
		private static void Start(HttpListener listener)
		{
			listener.Start();
			Task.Run(() => RunServer(listener));
		}
		
		/// <summary>
		/// Start awaiting for connections, up to the MaxSimultaneousConnections value. Runs on separate thread.
		/// </summary>
		private static void RunServer(HttpListener httpListener)
		{
			while (true)
			{
				sem.WaitOne();
				StartConnectionListener(listener);
			}	
		}
		
		/// <summary>
		/// Await connections
		/// </summary>
		private static async void StartConnectionListener(HttpListener httpListener)
		{
			// Wait for a connection and return to caller while we wait.
			var context = await listener.GetContextAsync();
			
			// Release the semaphore so that another listener can be
			// immediately started up.
			sem.Release();
			Log(context.Request);
			
			// We have a connection, do something...
			const string response = "<html><head><meta http-equiv='content-type' content='text/html; charset=utf-8'/>" +
			                        "</head>Hello Browser!</html>";
			var encoded = Encoding.UTF8.GetBytes(response);
			context.Response.ContentLength64 = encoded.Length;
			context.Response.OutputStream.Write(encoded, 0, encoded.Length);
			context.Response.OutputStream.Close();
		}

		/// <summary>
		/// Returns a list of IP addresses assigned to localhost network devices, for ex: hardwired ethernet, wireless, etc.
		/// </summary>
		private static List<IPAddress> GetLocalHostIPs()
		{
			var host = Dns.GetHostEntry(Dns.GetHostName());
			var ret = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToList();
			return ret;
		}

		private static HttpListener InitializeListener(List<IPAddress> localHostIPs)
		{
			var httpListener = new HttpListener();
			httpListener.Prefixes.Add("http://localhost/");

			// Listen to IP address as well.
			localHostIPs.ForEach(ip =>
			{
				Console.WriteLine("Listening to IP " + "http://" + ip.ToString() + "/");
				listener.Prefixes.Add("http://" + ip.ToString() + "/");
			});

			return httpListener;
		}

		public static void Log(HttpListenerRequest request)
		{
			Console.WriteLine(request.RemoteEndPoint + " " + request.HttpMethod + " /" + 
			                  request.Url?.AbsoluteUri.PadRight(3, ' '));
			
		}
	}
}
