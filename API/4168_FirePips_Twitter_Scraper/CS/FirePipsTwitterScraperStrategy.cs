using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "firepips_.mq4" script that downloads the FirePips web page and searches for order identifiers.
/// </summary>
public class FirePipsTwitterScraperStrategy : Strategy
{
	private readonly StrategyParam<string> _requestUrl;
	private readonly StrategyParam<string> _searchText;
	private readonly StrategyParam<string> _outputFileName;
	private readonly StrategyParam<int> _requestTimeout;

	/// <summary>
	/// URL that will be downloaded when the strategy starts.
	/// </summary>
	public string RequestUrl
	{
		get => _requestUrl.Value;
		set => _requestUrl.Value = value;
	}

	/// <summary>
	/// Substring that will be searched within the downloaded content.
	/// </summary>
	public string SearchText
	{
		get => _searchText.Value;
		set => _searchText.Value = value;
	}

	/// <summary>
	/// Name of the local file where the response is saved.
	/// </summary>
	public string OutputFileName
	{
		get => _outputFileName.Value;
		set => _outputFileName.Value = value;
	}

	/// <summary>
	/// Maximum HTTP request duration expressed in milliseconds.
	/// </summary>
	public int RequestTimeout
	{
		get => _requestTimeout.Value;
		set => _requestTimeout.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FirePipsTwitterScraperStrategy"/> class.
	/// </summary>
	public FirePipsTwitterScraperStrategy()
	{
		_requestUrl = Param(nameof(RequestUrl), "http://twitter.com/FirePips")
			.SetDisplay("Request URL", "HTTP address downloaded during OnStarted.", "Download");

		_searchText = Param(nameof(SearchText), "Order ID: 7")
			.SetDisplay("Search Text", "Substring searched inside the downloaded content.", "Download");

		_outputFileName = Param(nameof(OutputFileName), "SavedFromInternet.htm")
			.SetDisplay("Output File Name", "Local file that receives the raw HTTP response.", "Files");

		_requestTimeout = Param(nameof(RequestTimeout), 30000)
			.SetRange(1000, 600000)
			.SetDisplay("Request Timeout", "Maximum HTTP request duration in milliseconds.", "Download")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Task.Run(DownloadAndProcessAsync);
	}

	private async Task DownloadAndProcessAsync()
	{
		try
		{
			using var httpClient = new HttpClient
			{
				Timeout = TimeSpan.FromMilliseconds(RequestTimeout)
			};

			AddInfoLog("Requesting {0}.", RequestUrl);

			using var response = await httpClient.GetAsync(RequestUrl).ConfigureAwait(false);
			response.EnsureSuccessStatusCode();

			var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

			if (string.IsNullOrEmpty(content))
			{
				AddWarningLog("Received empty response from {0}.", RequestUrl);
				return;
			}

			AddInfoLog("Downloaded {0} characters from {1}.", content.Length, RequestUrl);

			ProcessContent(content);
		}
		catch (Exception ex)
		{
			AddErrorLog("HTTP download failed. {0}", ex);
		}
		finally
		{
			Stop();
		}
	}

	private void ProcessContent(string content)
	{
		var occurrences = FindOccurrences(content, SearchText);

		if (occurrences.Count == 0)
		{
			AddInfoLog("The search text "{0}" was not found in the response.", SearchText);
		}
		else
		{
			foreach (var index in occurrences)
			{
				AddInfoLog("Found "{0}" at character index {1}.", SearchText, index);
			}
		}

		var filePath = Path.GetFullPath(OutputFileName);
		File.WriteAllText(filePath, content, Encoding.UTF8);

		AddInfoLog("Saved downloaded content to {0}.", filePath);

		var firstToken = ExtractFirstToken(content);

		if (int.TryParse(firstToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
		{
			AddInfoLog("Parsed integer value {0} from the beginning of the file.", parsedValue);
		}
		else if (!string.IsNullOrWhiteSpace(firstToken))
		{
			AddInfoLog("The beginning of the file contains non-numeric data: "{0}".", firstToken);
		}
		else
		{
			AddInfoLog("No token could be extracted from the beginning of the downloaded content.");
		}
	}

	private static List<int> FindOccurrences(string content, string searchText)
	{
		var result = new List<int>();

		if (string.IsNullOrEmpty(searchText))
			return result;

		var index = 0;

		while ((index = content.IndexOf(searchText, index, StringComparison.Ordinal)) != -1)
		{
			result.Add(index);
			index += searchText.Length;
		}

		return result;
	}

	private static string ExtractFirstToken(string content)
	{
		if (string.IsNullOrEmpty(content))
			return string.Empty;

		var builder = new StringBuilder();

		foreach (var ch in content)
		{
			if (ch == ';')
			break;

			if (char.IsControl(ch))
			{
				if (builder.Length > 0)
				break;

				continue;
			}

			builder.Append(ch);
		}

		return builder.ToString().Trim();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		AddInfoLog("FirePips Twitter scraper completed.");
	}
}
