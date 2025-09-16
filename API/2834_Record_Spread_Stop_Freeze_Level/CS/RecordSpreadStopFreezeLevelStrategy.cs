using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Records spread, stop level and freeze level information for multiple securities into a log file.
/// </summary>
public class RecordSpreadStopFreezeLevelStrategy : Strategy
{
	private readonly StrategyParam<int> _recordPeriodMinutes;
	private readonly StrategyParam<string> _logFilePrefix;
	private readonly StrategyParam<IEnumerable<Security>> _additionalSecurities;
	private readonly StrategyParam<bool> _includePrimarySecurity;

	private readonly Dictionary<Security, SecuritySnapshot> _snapshots = [];
	private readonly object _sync = new();

	private List<Security> _monitoredSecurities = [];
	private Timer _timer;
	private string _logFilePath;

	// Level1 fields that may map to MetaTrader stop/freeze level concepts. They depend on the data provider.
	private static readonly Level1Fields? StopLevelField = TryGetField("StopLevel")
	?? TryGetField("MinStopPrice")
	?? TryGetField("StopPrice")
	?? TryGetField("StopDistance");

	private static readonly Level1Fields? FreezeLevelField = TryGetField("FreezeLevel")
	?? TryGetField("FreezePrice")
	?? TryGetField("FreezeDistance");

	/// <summary>
	/// Interval between log entries in minutes.
	/// </summary>
	public int RecordPeriodMinutes
	{
		get => _recordPeriodMinutes.Value;
		set => _recordPeriodMinutes.Value = value;
	}

	/// <summary>
	/// Prefix of the log file name.
	/// </summary>
	public string LogFilePrefix
	{
		get => _logFilePrefix.Value;
		set => _logFilePrefix.Value = value;
	}

	/// <summary>
	/// Additional securities that should be monitored together with the primary one.
	/// </summary>
	public IEnumerable<Security> AdditionalSecurities
	{
		get => _additionalSecurities.Value;
		set => _additionalSecurities.Value = value;
	}

	/// <summary>
	/// Monitor the strategy primary security.
	/// </summary>
	public bool IncludePrimarySecurity
	{
		get => _includePrimarySecurity.Value;
		set => _includePrimarySecurity.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RecordSpreadStopFreezeLevelStrategy"/>.
	/// </summary>
	public RecordSpreadStopFreezeLevelStrategy()
	{
		_recordPeriodMinutes = Param(nameof(RecordPeriodMinutes), 1)
		.SetGreaterThanZero()
		.SetDisplay("Record period", "Logging interval in minutes", "Logging");

		_logFilePrefix = Param(nameof(LogFilePrefix), "MktData")
		.SetDisplay("Log prefix", "File name prefix for generated logs", "Logging");

		_additionalSecurities = Param<IEnumerable<Security>>(nameof(AdditionalSecurities), [])
		.SetDisplay("Additional securities", "Extra securities to monitor", "Logging");

		_includePrimarySecurity = Param(nameof(IncludePrimarySecurity), true)
		.SetDisplay("Include primary", "Monitor the strategy primary security", "Logging");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		StopTimer();

		lock (_sync)
		{
			_snapshots.Clear();
			_monitoredSecurities = [];
			_logFilePath = null;
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();
		StopTimer();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var securities = new List<Security>();

		// Add the main strategy security when requested.
		if (IncludePrimarySecurity && Security != null)
		securities.Add(Security);

		// Append explicitly supplied securities while avoiding duplicates.
		if (AdditionalSecurities != null)
		{
			foreach (var security in AdditionalSecurities)
			{
				if (security != null && !securities.Contains(security))
				securities.Add(security);
			}
		}

		if (securities.Count == 0)
		throw new InvalidOperationException("No securities configured for monitoring.");

		lock (_sync)
		{
			_monitoredSecurities = securities;

			foreach (var security in _monitoredSecurities)
			{
				_snapshots[security] = new SecuritySnapshot();
			}
		}

		// Subscribe to Level1 data for every monitored security.
		foreach (var security in securities)
		{
			SubscribeLevel1(security)
			.Bind(message => ProcessLevel1(security, message))
			.Start();
		}

		PrepareLogFile(securities);

		var interval = TimeSpan.FromMinutes(RecordPeriodMinutes);

		if (interval <= TimeSpan.Zero)
		throw new InvalidOperationException("Record period must be greater than zero.");

		lock (_sync)
		{
			// Timer is started after the log file has been prepared to avoid missing the header.
			_timer = new Timer(OnTimer, null, interval, interval);
		}
	}

	private void ProcessLevel1(Security security, Level1ChangeMessage message)
	{
		lock (_sync)
		{
			if (!_snapshots.TryGetValue(security, out var snapshot))
			return;

			// Track best bid/ask to compute the spread.
			if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
			snapshot.BestBid = bid;

			if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
			snapshot.BestAsk = ask;

			// Stop and freeze levels are optional - they depend on the provider.
			if (StopLevelField is Level1Fields stopField && message.Changes.TryGetValue(stopField, out var stopObj))
			{
				var stopValue = ToDecimal(stopObj);
				if (stopValue != null)
				snapshot.StopLevel = stopValue;
			}

			if (FreezeLevelField is Level1Fields freezeField && message.Changes.TryGetValue(freezeField, out var freezeObj))
			{
				var freezeValue = ToDecimal(freezeObj);
				if (freezeValue != null)
				snapshot.FreezeLevel = freezeValue;
			}
		}
	}

	private void OnTimer(object state)
	{
		string line;

		lock (_sync)
		{
			if (string.IsNullOrEmpty(_logFilePath) || _monitoredSecurities.Count == 0)
			return;

			var sb = new StringBuilder();

			// Local time uses the workstation clock.
			sb.Append(DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture));
			sb.Append(';');

			// Server time comes from the trading connector.
			sb.Append(CurrentTime.ToString("O", CultureInfo.InvariantCulture));
			sb.Append(';');

			var isConnected = Connector?.IsConnected ?? false;
			sb.Append(isConnected ? "True" : "False");
			sb.Append(';');

			foreach (var security in _monitoredSecurities)
			{
				var snapshot = _snapshots[security];
				var spread = snapshot.GetSpread();

				sb.Append(spread?.ToString(CultureInfo.InvariantCulture) ?? "N/A");
				sb.Append(';');

				sb.Append(snapshot.StopLevel?.ToString(CultureInfo.InvariantCulture) ?? "N/A");
				sb.Append(';');

				sb.Append(snapshot.FreezeLevel?.ToString(CultureInfo.InvariantCulture) ?? "N/A");
				sb.Append(';');
			}

			if (sb.Length > 0 && sb[^1] == ';')
			sb.Length--;

			sb.AppendLine();
			line = sb.ToString();
		}

		// Writing outside of the lock prevents blocking data updates by slow disk operations.
		File.AppendAllText(_logFilePath, line, Encoding.UTF8);
	}

	private void PrepareLogFile(IEnumerable<Security> securities)
	{
		var baseDirectory = AppDomain.CurrentDomain.BaseDirectory ?? Environment.CurrentDirectory;
		var logsDirectory = Path.Combine(baseDirectory, "Logs");
		Directory.CreateDirectory(logsDirectory);

		var accountName = Portfolio?.Name ?? string.Empty;

		if (string.IsNullOrEmpty(accountName))
		accountName = Id.ToString();

		accountName = SanitizeFileName(accountName);

		var fileName = $"{LogFilePrefix}_Acc_{accountName}.csv";
		var logPath = Path.Combine(logsDirectory, fileName);

		var backupDirectory = Path.Combine(logsDirectory, "BUP");
		Directory.CreateDirectory(backupDirectory);
		var backupPath = Path.Combine(backupDirectory, fileName);

		if (File.Exists(backupPath))
		File.Delete(backupPath);

		if (File.Exists(logPath))
		{
			File.Copy(logPath, backupPath, true);
			File.Delete(logPath);
		}

using (var writer = new StreamWriter(logPath, false, Encoding.UTF8))
		{
			var header = BuildHeader(securities);
			writer.WriteLine(header);
		}

		lock (_sync)
		{
			_logFilePath = logPath;
		}
	}

	private static string BuildHeader(IEnumerable<Security> securities)
	{
		var sb = new StringBuilder();
		sb.Append("TimeLocal;TimeServer;IsConnected;");

		foreach (var security in securities)
		{
			var identifier = SanitizeFileName(security.Id);
			sb.Append(identifier).Append("_Spread;");
			sb.Append(identifier).Append("_StopLevel;");
			sb.Append(identifier).Append("_FreezeLevel;");
		}

		if (sb.Length > 0 && sb[^1] == ';')
		sb.Length--;

		return sb.ToString();
	}

	private void StopTimer()
	{
		Timer timer;

		lock (_sync)
		{
			timer = _timer;
			_timer = null;
		}

		timer?.Dispose();
	}

	private static Level1Fields? TryGetField(string name)
	{
		return Enum.TryParse(name, out Level1Fields field) ? field : null;
	}

	private static decimal? ToDecimal(object value)
	{
		return value switch
		{
			decimal dec => dec,
			double dbl => (decimal)dbl,
			float fl => (decimal)fl,
			long l => l,
			int i => i,
			short s => s,
			byte b => b,
			null => null,
			IConvertible convertible => Convert.ToDecimal(convertible, CultureInfo.InvariantCulture),
			_ => null
		};
	}

	private static string SanitizeFileName(string input)
	{
		if (string.IsNullOrEmpty(input))
		return "Unknown";

		var invalid = Path.GetInvalidFileNameChars();
		var builder = new StringBuilder(input.Length);

		foreach (var ch in input)
		{
			var isInvalid = false;

			for (var i = 0; i < invalid.Length; i++)
			{
				if (invalid[i] != ch)
				continue;

				isInvalid = true;
				break;
			}

			builder.Append(isInvalid ? '_' : ch);
		}

		return builder.ToString();
	}

	private sealed class SecuritySnapshot
	{
		public decimal? BestBid { get; set; }
		public decimal? BestAsk { get; set; }
		public decimal? StopLevel { get; set; }
		public decimal? FreezeLevel { get; set; }

		// Calculates the current spread when both bid and ask are known.
		public decimal? GetSpread()
		{
			if (BestBid is decimal bid && BestAsk is decimal ask)
			return ask - bid;

			return null;
		}
	}
}
