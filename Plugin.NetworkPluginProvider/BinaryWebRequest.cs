using System;
using System.IO;
using System.Net;
using System.Text;

namespace AlphaOmega.Web
{
	/// <summary>HttpWebRequest facade to read response as Byte[] or String</summary>
	public class BinaryWebRequest : IDisposable
	{
		/// <summary>Minimum buffer size</summary>
		private const Int32 MinBufferLength = 1024;

		/// <summary>The request instance</summary>
		protected HttpWebRequest Request { get; set; }

		/// <summary>The response instance</summary>
		protected HttpWebResponse Response { get; set; }

		/// <summary>Creating an instance of the Uri link request class</summary>
		/// <param name="uri">Link resource on the internet</param>
		/// <param name="useDefaultProxy">Use proxy authentication</param>
		public BinaryWebRequest(String uri, Boolean useDefaultProxy)
			: this((HttpWebRequest)WebRequest.Create(uri))
		{
			if(useDefaultProxy)
				this.Request.Proxy = new WebProxy() { UseDefaultCredentials = true, };
		}

		/// <summary>Create instance if <see cref="BinaryWebRequest"/> with webRequest instance</summary>
		/// <param name="request">The request instance</param>
		/// <exception cref="ArgumentNullException">request shoud not be null</exception>
		public BinaryWebRequest(HttpWebRequest request)
		{
			this.Request = request ?? throw new ArgumentNullException(nameof(request));
			this.Request.UserAgent = "AlphaOmega.BinaryWebRequest";
		}

		/// <summary>Setting proxy authorization data</summary>
		/// <param name="user">User</param>
		/// <param name="password">Password</param>
		/// <param name="domain">Domain</param>
		/// <returns>Success of setting authorization data</returns>
		public Boolean SetCredentials(String user, String password, String domain)
		{
			if(String.IsNullOrEmpty(user)
				|| String.IsNullOrEmpty(password)
				|| String.IsNullOrEmpty(domain))
				return false;
			else
			{
				this.Request.Proxy.Credentials = new NetworkCredential(user, password, domain);
				return true;
			}
		}

		/// <summary>Get the response stream from the server</summary>
		/// <returns>Data stream from the server</returns>
		public Stream GetResponseStream()
		{
			this.Response?.Close();
			this.Response = (HttpWebResponse)this.Request.GetResponse();
			return this.Response.GetResponseStream();
		}

		/// <summary>Get a response from the server</summary>
		/// <returns>Byte array of the received response</returns>
		public Byte[] GetResponse()
		{//TODO: For some reason, this was commented out. (Used in \Plugins.HttpHarvester\Plugins.Explosm\DocumentWriteComics.cs)
			Byte[] buffer = new Byte[4 * MinBufferLength];
			using(Stream stream = this.GetResponseStream())
			{
				using(MemoryStream memory = new MemoryStream())
				{
					Int32 count = 0;
					do
					{
						count = stream.Read(buffer, 0, buffer.Length);
						memory.Write(buffer, 0, count);
					} while(count != 0);
					return memory.ToArray();
				}
			}
		}

		/// <summary>Copies the contents of input to output. Doesn't close either stream.</summary>
		public void CopyTo(Stream output)
		{
			Byte[] buffer = new Byte[8 * MinBufferLength];
			Int32 len;
			while((len = this.GetResponseStream().Read(buffer, 0, buffer.Length)) > 0)
				output.Write(buffer, 0, len);
		}

		/// <summary>Get a response from the server</summary>
		/// <param name="codePage">Code page for displaying the page correctly</param>
		/// <returns>Server response to the request</returns>
		public String GetResponse(Int32 codePage)
			=> this.GetResponse(Encoding.GetEncoding(codePage));

		/// <summary>Get response from server with specific encoding</summary>
		/// <param name="codePage">The codepage to use to decode payload</param>
		/// <returns>Body as String</returns>
		public String GetResponse(Encoding codePage)
		{
			using(StreamReader reader = new StreamReader(this.GetResponseStream(), codePage))
				return reader.ReadToEnd();
			/*StringBuilder result = new StringBuilder();
			using(StreamReader reader = new StreamReader(this.GetResponseStream(), codePage))
			{
				Char[] buffer = new Char[MinBufferLength];
				Int32 count;
				do
				{
					count = reader.Read(buffer, 0, MinBufferLength);
					if(count > 0)
						result.Append(buffer);
				} while(count > 0);
			}
			return result.ToString();*/
		}

		/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
		public void Dispose()
			=> this.Response?.Close();
	}
}