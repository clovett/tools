using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Octokit;

namespace CreateGithubRelease
{
    class Program
    {

        static async Task Main(string[] args)
        {
			Directory.SetCurrentDirectory(@"d:\git\lovettchris\XmlNotepad");
			var creds = new Credentials("lovettchris", "Just27Frogs.");
			var github = new GitHubClient(new ProductHeaderValue("XmlNotepad"));
			github.Credentials = creds;
			var user = await github.User.Get("lovettchris");
			var release = await github.Repository.Release.GetLatest("lovettchris", "XmlNotepad");
            Console.WriteLine("Found latest release {0}", release.Name);

		}
		public static async Task<string> DownloadFileAsync(string url, string installerPath, long? maxDownloadSize = null)
		{
			var httpClient = new HttpClient();
			HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
			int redirectCount = 0;
			while (response.StatusCode == HttpStatusCode.Found && redirectCount < 2)
			{
				Uri location = response.Headers.Location;
				response = await httpClient.GetAsync(location, HttpCompletionOption.ResponseHeadersRead);
				redirectCount++;
			}
			if (!response.IsSuccessStatusCode)
			{
				throw new HttpRequestException(await response.Content.ReadAsStringAsync(), null, response.StatusCode);
			}
			string fileName = Path.GetFileName(url.Split('?').Last());
			string text = response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
			if (!Directory.Exists(installerPath))
			{
				Directory.CreateDirectory(installerPath);
			}
			string targetFile = Path.Combine(installerPath, text ?? fileName);
			long? contentLength = response.Content.Headers.ContentLength;
			if (contentLength > maxDownloadSize)
			{
				throw new Exception("Download exceeds maximum size " + maxDownloadSize.Value);
			}
			if (!File.Exists(targetFile) || new FileInfo(targetFile).Length != contentLength)
			{
				File.Delete(targetFile);
				using FileStream targetFileStream = File.OpenWrite(targetFile);
				await (await response.Content.ReadAsStreamAsync()).CopyToAsync(targetFileStream);
			}
			return targetFile;
		}
	}
}
