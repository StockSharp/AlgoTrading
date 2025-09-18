using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Morning/Evening Star pattern strategy confirmed by RSI crosses.
/// Converts the MetaTrader "Expert_AMS_ES_RSI" logic to the StockSharp high level API.
/// </summary>
public class AmsEsRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _bodyAveragePeriod;
	private readonly StrategyParam<decimal> _longEntryRsi;
	private readonly StrategyParam<decimal> _shortEntryRsi;
	private readonly StrategyParam<decimal> _lowerExitRsi;
	private readonly StrategyParam<decimal> _upperExitRsi;

	private readonly List<ICandleMessage> _recentCandles = new();
	private decimal? _previousRsi;

	/// <summary>
	/// Type of candles used for pattern detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Number of candles used to evaluate the average body size.
	/// </summary>
	public int BodyAveragePeriod
	{
		get => _bodyAveragePeriod.Value;
		set => _bodyAveragePeriod.Value = value;
	}

	/// <summary>
	/// RSI threshold confirming a long entry.
	/// </summary>
	public decimal LongEntryRsi
	{
		get => _longEntryRsi.Value;
		set => _longEntryRsi.Value = value;
	}

	/// <summary>
	/// RSI threshold confirming a short entry.
	/// </summary>
	public decimal ShortEntryRsi
	{
		get => _shortEntryRsi.Value;
		set => _shortEntryRsi.Value = value;
	}

	/// <summary>
	/// Lower RSI boundary that triggers position exits when crossed upward.
	/// </summary>
	public decimal LowerExitRsi
	{
		get => _lowerExitRsi.Value;
		set => _lowerExitRsi.Value = value;
	}

	/// <summary>
	/// Upper RSI boundary that triggers position exits when crossed downward.
	/// </summary>
	public decimal UpperExitRsi
	{
		get => _upperExitRsi.Value;
		set => _upperExitRsi.Value = value;
	}

	/// <summary>
	/// Initializes default parameters.
	/// </summary>
	public AmsEsRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for candle analysis", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 47)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Number of bars used in the RSI calculation", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 70, 5);

		_bodyAveragePeriod = Param(nameof(BodyAveragePeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("Body Average", "Number of candles for the average body size", "Patterns")
		.SetCanOptimize(true)
		.SetOptimize(2, 6, 1);

		_longEntryRsi = Param(nameof(LongEntryRsi), 40m)
		.SetDisplay("Long RSI", "Maximum RSI value allowed for Morning Star entries", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(20m, 50m, 5m);

		_shortEntryRsi = Param(nameof(ShortEntryRsi), 60m)
		.SetDisplay("Short RSI", "Minimum RSI value required for Evening Star entries", "Signals")
		.SetCanOptimize(true)
		.SetOptimize(50m, 80m, 5m);

		_lowerExitRsi = Param(nameof(LowerExitRsi), 30m)
		.SetDisplay("Lower Exit", "RSI level closing short positions on upward crosses", "Exits")
		.SetCanOptimize(true)
		.SetOptimize(20m, 40m, 5m);

		_upperExitRsi = Param(nameof(UpperExitRsi), 70m)
		.SetDisplay("Upper Exit", "RSI level closing long positions on downward crosses", "Exits")
		.SetCanOptimize(true)
		.SetOptimize(60m, 80m, 5m);
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

		_recentCandles.Clear();
		_previousRsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod,
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(rsi, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		_recentCandles.Add(candle);

		var maxHistory = Math.Max(BodyAveragePeriod, 3) + 2;
		if (_recentCandles.Count > maxHistory)
		_recentCandles.RemoveAt(0);

		if (!IsPatternDataReady())
		{
			_previousRsi = rsiValue;
			return;
		}

		if (_previousRsi is null)
		{
			_previousRsi = rsiValue;
			return;
		}

		var previousRsi = _previousRsi.Value;
		var currentRsi = rsiValue;

		var c1 = _recentCandles[^1];
		var c2 = _recentCandles[^2];
		var c3 = _recentCandles[^3];

		var averageBody = CalculateAverageBody();

		var morningStar = IsMorningStar(c1, c2, c3, averageBody);
		var eveningStar = IsEveningStar(c1, c2, c3, averageBody);

		if (morningStar && currentRsi < LongEntryRsi && Position <= 0)
		{
			// Morning Star in oversold territory -> enter long.
			BuyMarket();
		}

		if (eveningStar && currentRsi > ShortEntryRsi && Position >= 0)
		{
			// Evening Star in overbought territory -> enter short.
			SellMarket();
		}

		var exitLong = Position > 0 &&
		((currentRsi < UpperExitRsi && previousRsi > UpperExitRsi) ||
		(currentRsi < LowerExitRsi && previousRsi > LowerExitRsi));

		if (exitLong)
		{
			// RSI crossed below one of the exit levels -> close the long position.
			ClosePosition();
		}

		var exitShort = Position < 0 &&
		((currentRsi > LowerExitRsi && previousRsi < LowerExitRsi) ||
		(currentRsi > UpperExitRsi && previousRsi < UpperExitRsi));

		if (exitShort)
		{
			// RSI crossed above one of the exit levels -> close the short position.
			ClosePosition();
		}

		_previousRsi = currentRsi;
	}

	private bool IsPatternDataReady()
	{
		if (_recentCandles.Count < 3)
		return false;

		return _recentCandles.Count >= BodyAveragePeriod;
	}

	private decimal CalculateAverageBody()
	{
		var count = Math.Min(BodyAveragePeriod, _recentCandles.Count);

		if (count == 0)
		return 0m;

		decimal sum = 0m;

		for (var i = 0; i < count; i++)
		{
			var candle = _recentCandles[^1 - i];
			sum += Math.Abs(candle.ClosePrice - candle.OpenPrice);
		}

		return sum / count;
	}

	private static bool IsMorningStar(ICandleMessage c1, ICandleMessage c2, ICandleMessage c3, decimal averageBody)
	{
		if (averageBody <= 0m)
		return false;

		var body3 = Math.Abs(c3.OpenPrice - c3.ClosePrice);
		var body2 = Math.Abs(c2.ClosePrice - c2.OpenPrice);

		if (c3.OpenPrice - c3.ClosePrice <= averageBody)
		return false;

		if (body2 >= averageBody * 0.5m)
		return false;

		if (!(c2.ClosePrice < c3.ClosePrice && c2.OpenPrice < c3.OpenPrice))
		return false;

		var midpoint = (c3.OpenPrice + c3.ClosePrice) / 2m;
		return c1.ClosePrice > midpoint;
	}

	private static bool IsEveningStar(ICandleMessage c1, ICandleMessage c2, ICandleMessage c3, decimal averageBody)
	{
		if (averageBody <= 0m)
		return false;

		var body3 = Math.Abs(c3.ClosePrice - c3.OpenPrice);
		var body2 = Math.Abs(c2.ClosePrice - c2.OpenPrice);

		if (c3.ClosePrice - c3.OpenPrice <= averageBody)
		return false;

		if (body2 >= averageBody * 0.5m)
		return false;

		if (!(c2.ClosePrice > c3.ClosePrice && c2.OpenPrice > c3.OpenPrice))
		return false;

		var midpoint = (c3.OpenPrice + c3.ClosePrice) / 2m;
		return c1.ClosePrice < midpoint;
	}
}
