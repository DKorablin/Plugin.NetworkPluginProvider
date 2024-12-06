using System;
using System.IO;
using System.Net;
using System.Text;

namespace AlphaOmega.Web
{
	public class BinaryWebRequest : IDisposable
	{
		/// <summary>Минимальный размер буфера</summary>
		private const Int32 MinBufferLength = 1024;

		protected HttpWebRequest Request { get; set; }

		protected HttpWebResponse Response { get; set; }

		/// <summary>Создание экземпляра класса запроса Uri ссылки</summary>
		/// <param name="uri">Ссылка ресурс в интернете</param>
		/// <param name="useDefaultProxy">Использовать прокси аутентификацию</param>
		public BinaryWebRequest(String uri, Boolean useDefaultProxy)
			: this((HttpWebRequest)WebRequest.Create(uri))
		{
			if(useDefaultProxy)
				this.Request.Proxy = new WebProxy() { UseDefaultCredentials = true, };
		}

		public BinaryWebRequest(HttpWebRequest request)
		{
			this.Request = request ?? throw new ArgumentNullException(nameof(request));
			this.Request.UserAgent = "AlphaOmega.BinaryWebRequest";
		}

		/// <summary>Установка данных авторизации на проксю</summary>
		/// <param name="user">Пользователь</param>
		/// <param name="password">Пароль</param>
		/// <param name="domain">Домен</param>
		/// <returns>Успешность установки данных авторизации</returns>
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

		/// <summary>Получить поток ответа от сервера</summary>
		/// <returns>Поток данных от сервера</returns>
		public Stream GetResponseStream()
		{
			this.Response?.Close();
			this.Response = (HttpWebResponse)this.Request.GetResponse();
			return this.Response.GetResponseStream();
		}

		/// <summary>Получить ответ от сервера</summary>
		/// <returns>Массив байт полученного ответа</returns>
		public Byte[] GetResponse()
		{//TODO: Почему-то это было закомментировано. (Используется в \Plugins.HttpHarvester\Plugins.Explosm\DocumentWriteComics.cs)
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

		/// <summary>Получить ответ от сервера</summary>
		/// <param name="codePage">Кодовая страница, для корректного отображения страницы</param>
		/// <returns>Ответ сервера на запрос</returns>
		public String GetResponse(Int32 codePage)
			=> this.GetResponse(Encoding.GetEncoding(codePage));

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
		public void Dispose()
			=> this.Response?.Close();
	}
}