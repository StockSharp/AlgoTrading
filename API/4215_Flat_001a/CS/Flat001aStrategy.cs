using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Flat trading strategy that replicates the Flat_001a expert adviser for EURUSD H1.
/// Buys near the bottom quarter of a three-candle range and sells near the top quarter.
/// Applies fixed take-profit, adaptive stop-loss, and trailing-stop management.
/// </summary>
public class Flat001aStrategy : Strategy
{
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<int> _diffMinPoints;
	private readonly StrategyParam<int> _diffMaxPoints;
	private readonly StrategyParam<bool> _enableTimeFilter;
	private readonly StrategyParam<int> _openHour;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _securityCode;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;
	private decimal? _longEntry;
	private decimal? _shortEntry;
	private decimal _priceStep;
	private bool _isSecurityValid;
	private bool _rangeWarningIssued;

	/// <summary>
	/// Trailing-stop distance in points.
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Minimum three-candle range in points required for trading.
	/// </summary>
	public int DiffMinPoints
	{
		get => _diffMinPoints.Value;
		set => _diffMinPoints.Value = value;
	}

	/// <summary>
	/// Maximum three-candle range in points allowed for trading.
	/// </summary>
	public int DiffMaxPoints
	{
		get => _diffMaxPoints.Value;
		set => _diffMaxPoints.Value = value;
	}

	/// <summary>
	/// Enables or disables the trading hour filter.
	/// </summary>
	public bool EnableTimeFilter
	{
		get => _enableTimeFilter.Value;
		set => _enableTimeFilter.Value = value;
	}

	/// <summary>
	/// Starting hour of the trading window (0-23).
	/// </summary>
	public int OpenHour
	{
		get => _openHour.Value;
		set => _openHour.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Expected security code. Leave empty to skip validation.
	/// </summary>
	public string SecurityCode
	{
		get => _securityCode.Value;
		set => _securityCode.Value = value;
	}

	/// <summary>
	/// Initializes parameters with defaults matching the original expert adviser.
	/// </summary>
	public Flat001aStrategy()
	{
		_trailingStopPoints = Param(nameof(TrailingStopPoints), 6)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Trailing Stop (points)", "Trailing distance in points", "Risk");

		_diffMinPoints = Param(nameof(DiffMinPoints), 18)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Range Min (points)", "Minimum three-candle range", "Range Filter");

		_diffMaxPoints = Param(nameof(DiffMaxPoints), 28)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Range Max (points)", "Maximum three-candle range", "Range Filter");

		_enableTimeFilter = Param(nameof(EnableTimeFilter), true)
		.SetDisplay("Enable Time Filter", "Restrict trading hours", "Trading Window");

		_openHour = Param(nameof(OpenHour), 0)
		.SetDisplay("Opening Hour", "Starting hour (0-23)", "Trading Window");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 8)
		.SetGreaterThanOrEqual(0)
		.SetDisplay("Take Profit (points)", "Take-profit distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for range analysis", "General");

		_securityCode = Param(nameof(SecurityCode), "EURUSD")
		.SetDisplay("Security Code", "Expected instrument code", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highest = null!;
		_lowest = null!;
		_priceStep = 0m;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_longEntry = null;
		_shortEntry = null;
		_isSecurityValid = true;
		_rangeWarningIssued = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		{
			LogWarning("PriceStep is not set for the selected security. Using 1 as a fallback value.");
			_priceStep = 1m;
		}

		_highest = new Highest { Length = 3 };
		_lowest = new Lowest { Length = 3 };

		_isSecurityValid = string.IsNullOrWhiteSpace(SecurityCode) ||
		(Security?.Code?.Equals(SecurityCode, StringComparison.InvariantCultureIgnoreCase) ?? false);

		if (!_isSecurityValid)
		{
			LogWarning($"Security code '{Security?.Code ?? "<null>"}' does not match expected '{SecurityCode}'. Strategy will stay idle.");
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_highest, _lowest, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_isSecurityValid)
		return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
		return;

		if (DiffMinPoints >= DiffMaxPoints)
		{
			if (!_rangeWarningIssued)
			{
				LogWarning("DiffMaxPoints must be greater than DiffMinPoints to create a valid channel.");
				_rangeWarningIssued = true;
			}
			return;
		}

		_rangeWarningIssued = false;

		ManageTrailing(candle);

		if (CheckExit(candle))
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (EnableTimeFilter && !IsWithinTradingWindow(candle.OpenTime))
		return;

		var range = highestValue - lowestValue;
		if (range <= 0m)
		return;

		var diffPoints = range / _priceStep;
		if (diffPoints <= DiffMinPoints || diffPoints >= DiffMaxPoints)
		return;

		if (Position != 0)
		return;

		var closePrice = candle.ClosePrice;
		var quarterRange = range / 4m;
		var lowerBound = lowestValue + quarterRange;
		var upperBound = highestValue - quarterRange;

		if (closePrice > lowestValue && closePrice <= lowerBound)
		{
			EnterLong(closePrice, range, lowestValue);
		}
		else if (closePrice < highestValue && closePrice >= upperBound)
		{
			EnterShort(closePrice, range, highestValue);
		}
	}

	private void EnterLong(decimal entryPrice, decimal range, decimal lowestValue)
	{
		BuyMarket();

		_longEntry = entryPrice;
		_longStop = lowestValue - range / 3m;
		_longTake = TakeProfitPoints > 0 ? entryPrice + TakeProfitPoints * _priceStep : (decimal?)null;

		_shortEntry = null;
		_shortStop = null;
		_shortTake = null;
	}

	private void EnterShort(decimal entryPrice, decimal range, decimal highestValue)
	{
		SellMarket();

		_shortEntry = entryPrice;
		_shortStop = highestValue + range / 3m;
		_shortTake = TakeProfitPoints > 0 ? entryPrice - TakeProfitPoints * _priceStep : (decimal?)null;

		_longEntry = null;
		_longStop = null;
		_longTake = null;
	}

	private void ManageTrailing(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0)
		return;

		var trailingDistance = TrailingStopPoints * _priceStep;

		if (Position > 0 && _longEntry.HasValue && trailingDistance > 0m)
		{
			var profit = candle.ClosePrice - _longEntry.Value;
			if (profit > trailingDistance)
			{
				var newStop = candle.ClosePrice - trailingDistance;
				if (!_longStop.HasValue || newStop > _longStop.Value)
				{
					_longStop = newStop;
				}
			}
		}
		else if (Position < 0 && _shortEntry.HasValue && trailingDistance > 0m)
		{
			var profit = _shortEntry.Value - candle.ClosePrice;
			if (profit > trailingDistance)
			{
				var newStop = candle.ClosePrice + trailingDistance;
				if (!_shortStop.HasValue || newStop < _shortStop.Value)
				{
					_shortStop = newStop;
				}
			}
		}
	}

	private bool CheckExit(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				ClosePosition();
				ResetRiskLevels();
				return true;
			}

			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				ClosePosition();
				ResetRiskLevels();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				ClosePosition();
				ResetRiskLevels();
				return true;
			}

			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				ClosePosition();
				ResetRiskLevels();
				return true;
			}
		}
		else
		{
			if (_longStop.HasValue || _shortStop.HasValue)
			{
				ResetRiskLevels();
			}
		}

		return false;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var hour = time.Hour;
		var openHour = OpenHour;

		if (openHour < 0)
		{
			openHour = 0;
		}
		else if (openHour > 23)
		{
			openHour = 23;
		}

		var secondHour = openHour == 23 ? 23 : openHour + 1;
		return hour >= openHour && hour <= secondHour;
	}

	private void ResetRiskLevels()
	{
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_longEntry = null;
		_shortEntry = null;
	}
}
