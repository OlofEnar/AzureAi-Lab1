using Azure.AI.Language.QuestionAnswering;
using Azure;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AiQnA
{
	public class QnA
	{
		private Uri endpoint;
		private AzureKeyCredential credential;
		private string projectName = "CatWiki";
		private string deploymentName = "production";

		private QuestionAnsweringClient client;
		private QuestionAnsweringProject project;

		private static string cogSvcKey;
		private static string cogSvcRegion;
		private static string translatorEndpoint = "https://api.cognitive.microsofttranslator.com";

		public QnA()
		{
			// Prompt the user for the keys and endpoint
			Console.WriteLine("Please enter the Azure QnA Endpoint URL:");
			string endpointString = Console.ReadLine();

			Console.WriteLine("Please enter the Azure QnA Key:");
			string azureQnaKey = Console.ReadLine();

			Console.WriteLine("Please enter the Cognitive Service Key:");
			cogSvcKey = Console.ReadLine();
			cogSvcRegion = "westeurope";

			endpoint = new Uri(endpointString);
			credential = new AzureKeyCredential(azureQnaKey);

			client = new QuestionAnsweringClient(endpoint, credential);
			project = new QuestionAnsweringProject(projectName, deploymentName);

			Run().GetAwaiter().GetResult();
		}

		public async Task Run()
		{
			// Set console encoding to unicode
			Console.InputEncoding = Encoding.Unicode;
			Console.OutputEncoding = Encoding.Unicode;

			Console.Clear();
			Console.WriteLine("Ask a question in any language about cats, or type 'exit' to quit.");

			while (true)
			{
				Console.WriteLine("Question: ");
				string question = Console.ReadLine();

				if (question.ToLower() == "exit")
				{
					break;
				}

				try
				{
					// Get the answer from the QnA service
					Response<AnswersResult> response = await client.GetAnswersAsync(question, project);
					foreach (KnowledgeBaseAnswer answer in response.Value.Answers)
					{
						// Detect the language
						string language = await GetLanguage(question);
						Console.WriteLine("\nQuestion language: " + language + "\n");
						Console.WriteLine($"Question: {question}\n");
						Console.WriteLine($"Answer: {answer.Answer}\n");

						// If not English, translate the answer
						if (language != "en")
						{
							string translatedText = await Translate(answer.Answer, language);
							Console.WriteLine($"\nTranslation to {language}: " + translatedText + "\n");
						}
						else
						{
							Console.WriteLine("The question is already in english.\n");
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Request error: {ex.Message}");
				}
			}
		}

		private async Task<string> GetLanguage(string text)
		{
			string language = "en";

			// Use the Translator detect function
			object[] body = new object[] { new { Text = text } };
			var requestBody = JsonConvert.SerializeObject(body);
			using (var client = new HttpClient())
			{
				using (var request = new HttpRequestMessage())
				{
					// Build the request
					string path = "/detect?api-version=3.0";
					request.Method = HttpMethod.Post;
					request.RequestUri = new Uri(translatorEndpoint + path);
					request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
					request.Headers.Add("Ocp-Apim-Subscription-Key", cogSvcKey);
					request.Headers.Add("Ocp-Apim-Subscription-Region", cogSvcRegion);

					// Send the request and get response
					HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
					// Read response as a string
					string responseContent = await response.Content.ReadAsStringAsync();

					// Parse JSON array and get language
					JArray jsonResponse = JArray.Parse(responseContent);
					language = (string)jsonResponse[0]["language"];
				}
			}

			// return the language
			return language;
		}

		private async Task<string> Translate(string text, string targetLanguage)
		{
			string translation = "";

			// Use the Translator translate function
			object[] body = new object[] { new { Text = text } };
			var requestBody = JsonConvert.SerializeObject(body);
			using (var client = new HttpClient())
			{
				using (var request = new HttpRequestMessage())
				{
					// Build the request
					string path = $"/translate?api-version=3.0&to={targetLanguage}";
					request.Method = HttpMethod.Post;
					request.RequestUri = new Uri(translatorEndpoint + path);
					request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
					request.Headers.Add("Ocp-Apim-Subscription-Key", cogSvcKey);
					request.Headers.Add("Ocp-Apim-Subscription-Region", cogSvcRegion);

					// Send the request and get response
					HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
					// Read response as a string
					string responseContent = await response.Content.ReadAsStringAsync();

					// Parse JSON array and get translation
					JArray jsonResponse = JArray.Parse(responseContent);
					translation = (string)jsonResponse[0]["translations"][0]["text"];
				}
			}

			// Return the translation
			return translation;
		}
	}
}
