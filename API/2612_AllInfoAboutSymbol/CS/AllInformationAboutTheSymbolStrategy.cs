using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that collects and logs all available metadata about the configured security.
/// </summary>
public class AllInformationAboutTheSymbolStrategy : Strategy
{
	private static readonly string[] _securityPropertySkipList = new[]
	{
		"Id",
		"Code",
		"Name",
		"ShortName",
		"Type",
		"Class",
		"Currency",
		"Decimals",
		"CfiCode",
		"IsMarginTrading",
		"IsShortable",
		"UnderlyingSecurityId",
		"Lot",
		"VolumeStep",
		"MinVolume",
		"MaxVolume",
		"PriceStep",
		"StepPrice",
		"MinPrice",
		"MaxPrice",
		"MarginBuy",
		"MarginSell",
		"MarginLimit",
		"MarginMarket",
		"MarginIntraday",
		"MarginPremium",
		"Commission",
		"Multiplier",
		"Strike",
		"OptionType",
		"BinaryOptionType",
		"ExpirationDate",
		"SettlementDate",
		"BestBid",
		"BestAsk",
		"LastTrade",
		"OpenPrice",
		"HighPrice",
		"LowPrice",
		"ClosePrice",
		"LastPrice",
		"SettlementPrice",
		"OpenInterest",
		"LastChangeTime",
		"State",
		"Status",
		"TradingStatus",
		"PriceLimitType",
		"PriceLimitLow",
		"PriceLimitHigh",
		"ExtensionInfo",
		"Board"
	};

	private readonly StrategyParam<bool> _logBoardDetails;
	private readonly StrategyParam<bool> _logExtensionInfo;
	private readonly StrategyParam<bool> _logFullDump;

	/// <summary>
	/// Controls whether the exchange board information should be logged.
	/// </summary>
	public bool LogBoardDetails
	{
		get => _logBoardDetails.Value;
		set => _logBoardDetails.Value = value;
	}

	/// <summary>
	/// Controls whether the security extension info dictionary should be printed.
	/// </summary>
	public bool LogExtensionInfo
	{
		get => _logExtensionInfo.Value;
		set => _logExtensionInfo.Value = value;
	}

	/// <summary>
	/// Controls whether a full property dump should follow the grouped sections.
	/// </summary>
	public bool LogFullPropertyDump
	{
		get => _logFullDump.Value;
		set => _logFullDump.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters with descriptive UI metadata.
	/// </summary>
	public AllInformationAboutTheSymbolStrategy()
	{
		_logBoardDetails = Param(nameof(LogBoardDetails), true)
			.SetDisplay("Log exchange board", "Include exchange board information in the log output.", "Output");

		_logExtensionInfo = Param(nameof(LogExtensionInfo), true)
			.SetDisplay("Log extension info", "Print values stored in the security extension info dictionary.", "Output");

		_logFullDump = Param(nameof(LogFullPropertyDump), true)
			.SetDisplay("Log full property dump", "After the grouped sections, print every available security property via reflection.", "Output");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var security = Security ?? throw new InvalidOperationException("Security must be assigned before starting the strategy.");

		// Provide a clear marker that the collection process has started.
		LogInfo($"Collecting detailed information about security '{security.Id}'.");

		LogSecurityOverview(security);
		LogTradingParameters(security);
		LogMarketSnapshot(security);

		if (LogExtensionInfo)
		{
			LogSecurityExtensionInfo(security);
		}

		if (LogFullPropertyDump)
		{
			LogFullSecurityDump(security);
		}

		if (LogBoardDetails && security.Board is not null)
		{
			LogBoardDetailsInternal(security.Board);
		}

		LogInfo("Information snapshot is finished.");
	}

	private void LogSecurityOverview(Security security)
	{
		LogInfo("Overview:");
		LogValue(security, "\t", "Id", "Identifier");
		LogValue(security, "\t", "Code", "Code");
		LogValue(security, "\t", "Name", "Name");
		LogValue(security, "\t", "ShortName", "Short name");
		LogValue(security, "\t", "Type", "Type");
		LogValue(security, "\t", "Class", "Class");
		LogValue(security, "\t", "Currency", "Currency");
		LogValue(security, "\t", "Decimals", "Decimals");
		LogValue(security, "\t", "CfiCode", "CFI code");
		LogValue(security, "\t", "IsMarginTrading", "Margin trading enabled");
		LogValue(security, "\t", "IsShortable", "Short selling allowed");
		LogValue(security, "\t", "UnderlyingSecurityId", "Underlying security id");
		LogValue(security, "\t", "Sector", "Sector");
		LogValue(security, "\t", "Industry", "Industry");
	}

	private void LogTradingParameters(Security security)
	{
		LogInfo("Trading parameters:");
		LogValue(security, "\t", "Lot", "Lot size");
		LogValue(security, "\t", "VolumeStep", "Volume step");
		LogValue(security, "\t", "MinVolume", "Min volume");
		LogValue(security, "\t", "MaxVolume", "Max volume");
		LogValue(security, "\t", "PriceStep", "Price step");
		LogValue(security, "\t", "StepPrice", "Step price value");
		LogValue(security, "\t", "MinPrice", "Minimum price");
		LogValue(security, "\t", "MaxPrice", "Maximum price");
		LogValue(security, "\t", "MarginBuy", "Margin (buy)");
		LogValue(security, "\t", "MarginSell", "Margin (sell)");
		LogValue(security, "\t", "MarginLimit", "Margin (limit)");
		LogValue(security, "\t", "MarginMarket", "Margin (market)");
		LogValue(security, "\t", "MarginIntraday", "Margin (intraday)");
		LogValue(security, "\t", "MarginPremium", "Margin (premium)");
		LogValue(security, "\t", "Commission", "Commission");
		LogValue(security, "\t", "Multiplier", "Multiplier");
		LogValue(security, "\t", "Strike", "Strike price");
		LogValue(security, "\t", "OptionType", "Option type");
		LogValue(security, "\t", "BinaryOptionType", "Binary option type");
		LogValue(security, "\t", "ExpirationDate", "Expiration date");
		LogValue(security, "\t", "SettlementDate", "Settlement date");
	}

	private void LogMarketSnapshot(Security security)
	{
		LogInfo("Market snapshot:");
		LogValue(security, "\t", "BestBid", "Best bid");
		LogValue(security, "\t", "BestAsk", "Best ask");
		LogValue(security, "\t", "LastTrade", "Last trade");
		LogValue(security, "\t", "OpenPrice", "Session open");
		LogValue(security, "\t", "HighPrice", "Session high");
		LogValue(security, "\t", "LowPrice", "Session low");
		LogValue(security, "\t", "ClosePrice", "Previous close");
		LogValue(security, "\t", "LastPrice", "Last price");
		LogValue(security, "\t", "SettlementPrice", "Settlement price");
		LogValue(security, "\t", "OpenInterest", "Open interest");
		LogValue(security, "\t", "LastChangeTime", "Last change time");
		LogValue(security, "\t", "State", "State");
		LogValue(security, "\t", "Status", "Status");
		LogValue(security, "\t", "TradingStatus", "Trading status");
		LogValue(security, "\t", "PriceLimitType", "Price limit type");
		LogValue(security, "\t", "PriceLimitLow", "Price limit low");
		LogValue(security, "\t", "PriceLimitHigh", "Price limit high");
	}

	private void LogSecurityExtensionInfo(Security security)
	{
		var property = security.GetType().GetProperty("ExtensionInfo", BindingFlags.Instance | BindingFlags.Public);

		if (property is null || !property.CanRead || property.GetIndexParameters().Length > 0)
		{
			LogInfo("Security extension info property is not available.");
			return;
		}

		object? value;

		try
		{
			value = property.GetValue(security);
		}
		catch (Exception exception)
		{
			LogInfo($"Security extension info could not be read: <error: {exception.Message}>");
			return;
		}

		if (value is IDictionary dictionary && dictionary.Count > 0)
		{
			LogInfo("Security extension info:");

			foreach (DictionaryEntry entry in dictionary)
			{
				LogInfo($"\t{FormatValue(entry.Key)}: {FormatValue(entry.Value)}");
			}
		}
		else
		{
			LogInfo("Security extension info: <empty>");
		}
	}

	private void LogFullSecurityDump(Security security)
	{
		LogInfo("Full property dump:");
		LogProperties(security, "\t", _securityPropertySkipList);
	}

	private void LogBoardDetailsInternal(ExchangeBoard board)
	{
		LogInfo($"Exchange board details for '{board.Code}':");
		LogValue(board, "\t", "Code", "Code");
		LogValue(board, "\t", "Name", "Name");
		LogValue(board, "\t", "Exchange", "Exchange");
		LogValue(board, "\t", "Country", "Country");
		LogValue(board, "\t", "TimeZone", "Time zone");
		LogValue(board, "\t", "SecurityClasses", "Security classes");
		LogValue(board, "\t", "IsSupportMarketOrders", "Supports market orders");
		LogValue(board, "\t", "IsSupportStopOrders", "Supports stop orders");
		LogValue(board, "\t", "IsSupportStopLimitOrders", "Supports stop-limit orders");
		LogValue(board, "\t", "IsSupportOddLots", "Supports odd lots");
		LogValue(board, "\t", "IsSupportMargin", "Supports margin trading");
		LogValue(board, "\t", "DeliveryBoard", "Delivery board");
		LogValue(board, "\t", "SettlementMode", "Settlement mode");

		var skip = new[]
		{
			"Code",
			"Name",
			"Exchange",
			"Country",
			"TimeZone",
			"SecurityClasses",
			"IsSupportMarketOrders",
			"IsSupportStopOrders",
			"IsSupportStopLimitOrders",
			"IsSupportOddLots",
			"IsSupportMargin",
			"DeliveryBoard",
			"SettlementMode"
		};

		LogProperties(board, "\t", skip);
	}

	private void LogProperties(object instance, string indent, string[]? skipProperties)
	{
		var type = instance.GetType();
		var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

		if (properties.Length == 0)
		{
			LogInfo($"{indent}<no properties>");
			return;
		}

		Array.Sort(properties, CompareProperties);

		foreach (var property in properties)
		{
			if (!property.CanRead || property.GetIndexParameters().Length > 0)
				continue;

			if (ShouldSkip(property.Name, skipProperties))
				continue;

			object? value;

			try
			{
				value = property.GetValue(instance);
			}
			catch (Exception exception)
			{
				LogInfo($"{indent}{property.Name}: <error: {exception.Message}>");
				continue;
			}

			LogInfo($"{indent}{property.Name}: {FormatValue(value)}");
		}
	}

	private void LogValue(object instance, string indent, string propertyName, string label)
	{
		var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

		if (property is null || !property.CanRead || property.GetIndexParameters().Length > 0)
			return;

		object? value;

		try
		{
			value = property.GetValue(instance);
		}
		catch (Exception exception)
		{
			LogInfo($"{indent}{label}: <error: {exception.Message}>");
			return;
		}

		LogInfo($"{indent}{label}: {FormatValue(value)}");
	}

	private static int CompareProperties(PropertyInfo left, PropertyInfo right)
	{
		return string.Compare(left.Name, right.Name, StringComparison.Ordinal);
	}

	private static bool ShouldSkip(string name, string[]? skipProperties)
	{
		if (skipProperties is null || skipProperties.Length == 0)
			return false;

		foreach (var skip in skipProperties)
		{
			if (string.Equals(name, skip, StringComparison.Ordinal))
				return true;
		}

		return false;
	}

	private string FormatValue(object? value)
	{
		if (value is null)
			return "<null>";

		switch (value)
		{
			case string str:
				return string.IsNullOrEmpty(str) ? "<empty>" : str;
			case DateTime dateTime:
				return dateTime.ToString("O", CultureInfo.InvariantCulture);
			case DateTimeOffset dateTimeOffset:
				return dateTimeOffset.ToString("O", CultureInfo.InvariantCulture);
			case TimeSpan timeSpan:
				return timeSpan.ToString();
			case decimal decimalValue:
				return decimalValue.ToString(CultureInfo.InvariantCulture);
			case double doubleValue:
				return doubleValue.ToString(CultureInfo.InvariantCulture);
			case float floatValue:
				return floatValue.ToString(CultureInfo.InvariantCulture);
			case Security security:
				return string.IsNullOrEmpty(security.Id) ? security.ToString() ?? "<Security>" : security.Id;
			case ExchangeBoard board:
				return string.IsNullOrEmpty(board.Code) ? board.ToString() ?? "<Board>" : board.Code;
			case IDictionary dictionary:
				return FormatDictionary(dictionary);
			case IEnumerable enumerable when value is not string:
				return FormatEnumerable(enumerable);
			case IFormattable formattable:
				return formattable.ToString(null, CultureInfo.InvariantCulture);
			default:
				return value.ToString() ?? "<null>";
		}
	}

	private string FormatDictionary(IDictionary dictionary)
	{
		if (dictionary.Count == 0)
			return "<empty>";

		var builder = new StringBuilder();

		foreach (DictionaryEntry entry in dictionary)
		{
			if (builder.Length > 0)
				builder.Append(", ");

			builder.Append('[');
			builder.Append(FormatValue(entry.Key));
			builder.Append(": ");
			builder.Append(FormatValue(entry.Value));
			builder.Append(']');
		}

		return builder.ToString();
	}

	private string FormatEnumerable(IEnumerable enumerable)
	{
		var builder = new StringBuilder();

		foreach (var item in enumerable)
		{
			if (builder.Length > 0)
				builder.Append(", ");

			builder.Append(FormatValue(item));
		}

		return builder.Length == 0 ? "<empty>" : builder.ToString();
	}
}
