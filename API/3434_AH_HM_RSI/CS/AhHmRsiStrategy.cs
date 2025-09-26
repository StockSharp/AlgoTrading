using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert "Expert_AH_HM_RSI".
/// Identifies hammer or hanging man candles and confirms them with an RSI filter before trading.
/// </summary>
public class AhHmRsiStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volumeParam;
	private readonly StrategyParam<int> _rsiPeriodParam;
	private readonly StrategyParam<int> _maPeriodParam;
	private readonly StrategyParam<decimal> _hammerRsiThresholdParam;
	private readonly StrategyParam<decimal> _hangingManRsiThresholdParam;
	private readonly StrategyParam<decimal> _lowerExitLevelParam;
	private readonly StrategyParam<decimal> _upperExitLevelParam;
	private readonly StrategyParam<DataType> _candleTypeParam;

	private readonly RelativeStrengthIndex _rsi = new();
	private readonly SimpleMovingAverage _sma = new();

	private ICandleMessage _previousCandle;
	private decimal? _previousRsi;
	private decimal? _previousSma;

	/// <summary>
	/// Initializes strategy parameters with defaults identical to the original expert.
	/// </summary>
	public AhHmRsiStrategy()
	{
		_volumeParam = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Order size used for entries", "Trading")
			.SetCanOptimize(true);

		_rsiPeriodParam = Param(nameof(RsiPeriod), 33)
			.SetDisplay("RSI Period", "Length of the RSI confirmation filter", "Indicators")
			.SetCanOptimize(true);

		_maPeriodParam = Param(nameof(MaPeriod), 2)
			.SetDisplay("MA Period", "Length of the smoothing average used to detect the trend", "Indicators")
			.SetCanOptimize(true);

		_hammerRsiThresholdParam = Param(nameof(HammerRsiThreshold), 40m)
			.SetDisplay("Hammer RSI", "RSI level that enables long trades after a hammer", "Filters")
			.SetCanOptimize(true);

		_hangingManRsiThresholdParam = Param(nameof(HangingManRsiThreshold), 60m)
			.SetDisplay("Hanging Man RSI", "RSI level that enables short trades after a hanging man", "Filters")
			.SetCanOptimize(true);

		_lowerExitLevelParam = Param(nameof(LowerExitLevel), 30m)
			.SetDisplay("RSI Lower Exit", "RSI level used for exit cross checks", "Risk")
			.SetCanOptimize(true);

		_upperExitLevelParam = Param(nameof(UpperExitLevel), 70m)
			.SetDisplay("RSI Upper Exit", "RSI level used for exit cross checks", "Risk")
			.SetCanOptimize(true);

		_candleTypeParam = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(1)))
			.SetDisplay("Candle Type", "Timeframe used for detecting candle patterns", "Data")
			.SetCanOptimize(false);
	}

	/// <summary>
	/// Base order volume used for entries.
	/// </summary>
	public decimal Volume
	{
		get => _volumeParam.Value;
		set => _volumeParam.Value = value;
	}

	/// <summary>
	/// Length of the RSI confirmation filter.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriodParam.Value;
		set => _rsiPeriodParam.Value = value;
	}

	/// <summary>
	/// Length of the moving average used for the trend filter in candle pattern detection.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriodParam.Value;
		set => _maPeriodParam.Value = value;
	}

	/// <summary>
	/// RSI threshold that must be met to validate a hammer candle.
	/// </summary>
	public decimal HammerRsiThreshold
	{
		get => _hammerRsiThresholdParam.Value;
		set => _hammerRsiThresholdParam.Value = value;
	}

	/// <summary>
	/// RSI threshold that must be met to validate a hanging man candle.
	/// </summary>
	public decimal HangingManRsiThreshold
	{
		get => _hangingManRsiThresholdParam.Value;
		set => _hangingManRsiThresholdParam.Value = value;
	}

	/// <summary>
	/// Lower RSI level used to detect exit crosses.
	/// </summary>
	public decimal LowerExitLevel
	{
		get => _lowerExitLevelParam.Value;
		set => _lowerExitLevelParam.Value = value;
	}

	/// <summary>
	/// Upper RSI level used to detect exit crosses.
	/// </summary>
	public decimal UpperExitLevel
	{
		get => _upperExitLevelParam.Value;
		set => _upperExitLevelParam.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_rsi.Length = RsiPeriod;
		_sma.Length = MaPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!candle.HighPrice.HasValue || !candle.LowPrice.HasValue || !candle.OpenPrice.HasValue || !candle.ClosePrice.HasValue)
			return;

		var currentRsi = rsiValue;
		var previousRsi = _previousRsi;
		var previousSma = _previousSma;
		var previousCandle = _previousCandle;

		var hasHammer = previousSma.HasValue && previousCandle != null && IsHammer(candle, previousCandle, previousSma.Value);
		var hasHangingMan = previousSma.HasValue && previousCandle != null && IsHangingMan(candle, previousCandle, previousSma.Value);

		var crossAboveLower = previousRsi.HasValue && currentRsi > LowerExitLevel && previousRsi.Value < LowerExitLevel;
		var crossBelowLower = previousRsi.HasValue && currentRsi < LowerExitLevel && previousRsi.Value > LowerExitLevel;
		var crossAboveUpper = previousRsi.HasValue && currentRsi > UpperExitLevel && previousRsi.Value < UpperExitLevel;
		var crossBelowUpper = previousRsi.HasValue && currentRsi < UpperExitLevel && previousRsi.Value > UpperExitLevel;

		if (hasHammer && currentRsi < HammerRsiThreshold && Position <= 0)
		{
			LogInfo($"Hammer confirmed by RSI {currentRsi:F2}. Going long.");
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (hasHangingMan && currentRsi > HangingManRsiThreshold && Position >= 0)
		{
			LogInfo($"Hanging man confirmed by RSI {currentRsi:F2}. Going short.");
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0 && (crossBelowUpper || crossBelowLower))
		{
			LogInfo($"RSI exit cross detected at {currentRsi:F2}. Closing long position.");
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && (crossAboveLower || crossAboveUpper))
		{
			LogInfo($"RSI exit cross detected at {currentRsi:F2}. Closing short position.");
			BuyMarket(Math.Abs(Position));
		}

		_previousRsi = currentRsi;
		_previousSma = smaValue;
		_previousCandle = candle;
	}

	private static bool IsHammer(ICandleMessage current, ICandleMessage previous, decimal trendAverage)
	{
		if (!previous.HighPrice.HasValue || !previous.LowPrice.HasValue || !previous.OpenPrice.HasValue || !previous.ClosePrice.HasValue)
			return false;

		var high = current.HighPrice.Value;
		var low = current.LowPrice.Value;
		var open = current.OpenPrice.Value;
		var close = current.ClosePrice.Value;

		var range = high - low;
		if (range <= 0)
			return false;

		var midpoint = (high + low) / 2m;
		if (midpoint >= trendAverage)
			return false;

		var upperThird = high - range / 3m;
		var minOpenClose = Math.Min(open, close);
		if (minOpenClose <= upperThird)
			return false;

		return close < previous.ClosePrice.Value && open < previous.OpenPrice.Value;
	}

	private static bool IsHangingMan(ICandleMessage current, ICandleMessage previous, decimal trendAverage)
	{
		if (!previous.HighPrice.HasValue || !previous.LowPrice.HasValue || !previous.OpenPrice.HasValue || !previous.ClosePrice.HasValue)
			return false;

		var high = current.HighPrice.Value;
		var low = current.LowPrice.Value;
		var open = current.OpenPrice.Value;
		var close = current.ClosePrice.Value;

		var range = high - low;
		if (range <= 0)
			return false;

		var midpoint = (high + low) / 2m;
		if (midpoint <= trendAverage)
			return false;

		var upperThird = high - range / 3m;
		var minOpenClose = Math.Min(open, close);
		if (minOpenClose <= upperThird)
			return false;

		return close > previous.ClosePrice.Value && open > previous.OpenPrice.Value;
	}
}
