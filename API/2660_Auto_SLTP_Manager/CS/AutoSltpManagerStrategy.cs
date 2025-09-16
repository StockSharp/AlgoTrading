namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Automatically trails stop-loss and take-profit levels for active positions based on external configuration.
/// </summary>
public class AutoSltpManagerStrategy : Strategy
{
	private sealed record SlTpSettings(decimal StopLossDistance, decimal TakeProfitDistance);

	private readonly StrategyParam<string> _configurationPath;
	private readonly StrategyParam<TimeSpan> _updateInterval;

	private readonly Dictionary<Sides, SlTpSettings> _settings = new();

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private DateTimeOffset _nextUpdateTime;

	public AutoSltpManagerStrategy()
	{
		_configurationPath = Param(nameof(ConfigurationPath), Path.Combine("AutoSLTP", "AutoSLTP.txt"))
		.SetDisplay("Configuration Path", "Path to AutoSLTP configuration file", "General");

		_updateInterval = Param(nameof(UpdateInterval), TimeSpan.FromSeconds(10))
		.SetDisplay("Update Interval", "Minimum time between trailing updates", "General");
	}

	/// <summary>
	/// Path to the configuration file that defines symbol specific stop-loss and take-profit distances.
	/// </summary>
	public string ConfigurationPath
	{
		get => _configurationPath.Value;
		set => _configurationPath.Value = value;
	}

	/// <summary>
	/// Minimum delay between consecutive trailing recalculations.
	/// </summary>
	public TimeSpan UpdateInterval
	{
		get => _updateInterval.Value;
		set => _updateInterval.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Ticks)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_settings.Clear();
		ResetLevels();
		_nextUpdateTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		LoadConfiguration();

		SubscribeTrades()
		.Bind(ProcessTrade)
		.Start();
	}

	private void LoadConfiguration()
	{
		_settings.Clear();

		var path = ConfigurationPath;

		if (string.IsNullOrWhiteSpace(path))
		throw new InvalidOperationException("Configuration path is empty.");

		path = Path.GetFullPath(path);

		if (!File.Exists(path))
		throw new FileNotFoundException($"Configuration file '{path}' not found.", path);

		var lines = File.ReadAllLines(path);
		var matched = false;

		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i].Trim();

			if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
			continue;

			var parts = line.Split('*');

			if (parts.Length != 4)
			throw new InvalidOperationException($"Line {i + 1} has invalid format. Expected four fields separated by '*'.");

			var symbol = parts[0].Trim();

			if (!IsSymbolMatch(symbol))
			continue;

			matched = true;

			var typeText = parts[1].Trim();
			if (!TryParseSide(typeText, out var side))
			throw new InvalidOperationException($"Line {i + 1} has unknown position type '{typeText}'.");

			if (!decimal.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sl) || sl <= 0m)
			throw new InvalidOperationException($"Line {i + 1} has invalid stop loss value '{parts[2]}'.");

			if (!decimal.TryParse(parts[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var tp) || tp <= 0m)
			throw new InvalidOperationException($"Line {i + 1} has invalid take profit value '{parts[3]}'.");

			var stopDistance = ConvertToPriceOffset(sl);
			var takeDistance = ConvertToPriceOffset(tp);

			_settings[side] = new SlTpSettings(stopDistance, takeDistance);
		}

		if (!matched)
		throw new InvalidOperationException("No matching configuration entries found for the selected security.");
	}

	private bool IsSymbolMatch(string symbol)
	{
		return string.Equals(symbol, Security?.Id, StringComparison.OrdinalIgnoreCase)
		|| string.Equals(symbol, Security?.Code, StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryParseSide(string value, out Sides side)
	{
		switch (value.ToUpperInvariant())
		{
		case "POSITION_TYPE_BUY":
			side = Sides.Buy;
			return true;
		case "POSITION_TYPE_SELL":
			side = Sides.Sell;
			return true;
		default:
			side = default;
			return false;
		}
	}

	private decimal ConvertToPriceOffset(decimal pips)
	{
		var step = Security?.PriceStep ?? 1m;

		if (step <= 0m)
		step = 1m;

		var adjust = 1m;
		var decimals = GetDecimals(step);

		if (decimals == 3 || decimals == 5)
		adjust = 10m;

		return pips * step * adjust;
	}

	private static int GetDecimals(decimal value)
	{
		value = Math.Abs(value);

		var decimals = 0;

		while (value != Math.Truncate(value) && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice ?? 0m;

		if (price <= 0m)
		return;

		var time = trade.ServerTime != default ? trade.ServerTime : trade.LocalTime;

		if (time != default)
		{
			if (_nextUpdateTime != default && time < _nextUpdateTime)
			return;

			_nextUpdateTime = time + UpdateInterval;
		}

		var position = Position;

		if (position > 0m)
		{
			UpdateLong(price, position);
			ResetShortLevels();
		}
		else if (position < 0m)
		{
			UpdateShort(price, position);
			ResetLongLevels();
		}
		else
		{
			ResetLevels();
		}
	}

	private void UpdateLong(decimal price, decimal position)
	{
		if (!_settings.TryGetValue(Sides.Buy, out var settings))
		return;

		var volume = Math.Abs(position);

		if (volume <= 0m)
		return;

		if (_longStop is decimal stop && price <= stop)
		{
			SellMarket(volume);
			ResetLongLevels();
			return;
		}

		if (_longTake is decimal take && price >= take)
		{
			SellMarket(volume);
			ResetLongLevels();
			return;
		}

		if (settings.StopLossDistance > 0m)
		{
			var candidate = price - settings.StopLossDistance;
			if (_longStop is null || candidate > _longStop.Value)
			_longStop = candidate;
		}

		if (settings.TakeProfitDistance > 0m)
		{
			var candidate = price + settings.TakeProfitDistance;
			if (_longTake is null || candidate > _longTake.Value)
			_longTake = candidate;
		}
	}

	private void UpdateShort(decimal price, decimal position)
	{
		if (!_settings.TryGetValue(Sides.Sell, out var settings))
		return;

		var volume = Math.Abs(position);

		if (volume <= 0m)
		return;

		if (_shortStop is decimal stop && price >= stop)
		{
			BuyMarket(volume);
			ResetShortLevels();
			return;
		}

		if (_shortTake is decimal take && price <= take)
		{
			BuyMarket(volume);
			ResetShortLevels();
			return;
		}

		if (settings.StopLossDistance > 0m)
		{
			var candidate = price + settings.StopLossDistance;
			if (_shortStop is null || candidate < _shortStop.Value)
			_shortStop = candidate;
		}

		if (settings.TakeProfitDistance > 0m)
		{
			var candidate = price - settings.TakeProfitDistance;
			if (_shortTake is null || candidate < _shortTake.Value)
			_shortTake = candidate;
		}
	}

	private void ResetLevels()
	{
		ResetLongLevels();
		ResetShortLevels();
	}

	private void ResetLongLevels()
	{
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortLevels()
	{
		_shortStop = null;
		_shortTake = null;
	}
}
