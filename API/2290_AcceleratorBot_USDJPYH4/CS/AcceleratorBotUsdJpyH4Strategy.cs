using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Accelerator bot strategy converted from MQL4.
/// Combines ADX, Stochastic oscillator and multi-timeframe AC momentum.
/// </summary>
public class AcceleratorBotUsdJpyH4Strategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _trailPoints;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _x1;
	private readonly StrategyParam<int> _x2;
	private readonly StrategyParam<int> _x3;
	private readonly StrategyParam<DataType> _candleType;

	private readonly SimpleMovingAverage _aoFastH4 = new() { Length = 5 };
	private readonly SimpleMovingAverage _aoSlowH4 = new() { Length = 34 };
	private readonly SimpleMovingAverage _acMaH4 = new() { Length = 5 };
	private readonly SimpleMovingAverage _aoFastD1 = new() { Length = 5 };
	private readonly SimpleMovingAverage _aoSlowD1 = new() { Length = 34 };
	private readonly SimpleMovingAverage _acMaD1 = new() { Length = 5 };
	private readonly SimpleMovingAverage _aoFastW1 = new() { Length = 5 };
	private readonly SimpleMovingAverage _aoSlowW1 = new() { Length = 34 };
	private readonly SimpleMovingAverage _acMaW1 = new() { Length = 5 };

	private decimal _acH4;
	private decimal _acD1;
	private decimal _acW1;

	private readonly List<ICandleMessage> _candles = new();

	private decimal? _entryPrice;
	private decimal? _takePrice;
	private decimal? _stopPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;

	/// <summary> Stop loss distance in points. </summary>
	public int StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	/// <summary> Take profit distance in points. </summary>
	public int TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	/// <summary> Trailing stop distance in points. </summary>
	public int TrailPoints { get => _trailPoints.Value; set => _trailPoints.Value = value; }
	/// <summary> ADX calculation period. </summary>
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }
	/// <summary> Minimum ADX value to use trend based rules. </summary>
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	/// <summary> Weekly AC weight. </summary>
	public int X1 { get => _x1.Value; set => _x1.Value = value; }
	/// <summary> Daily AC weight. </summary>
	public int X2 { get => _x2.Value; set => _x2.Value = value; }
	/// <summary> H4 AC weight. </summary>
	public int X3 { get => _x3.Value; set => _x3.Value = value; }
	/// <summary> Candle type for base timeframe. </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public AcceleratorBotUsdJpyH4Strategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 750)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 9999)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Take profit distance in points", "Risk");

		_trailPoints = Param(nameof(TrailPoints), 0)
		.SetDisplay("Trailing", "Trailing stop distance in points", "Risk");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
		.SetGreaterThanZero()
		.SetDisplay("ADX Threshold", "Minimum ADX to use trend rules", "Indicators");

		_x1 = Param(nameof(X1), 0)
		.SetDisplay("AC Weight W1", "Weight for weekly AC", "Momentum");
		_x2 = Param(nameof(X2), 150)
		.SetDisplay("AC Weight D1", "Weight for daily AC", "Momentum");
		_x3 = Param(nameof(X3), 500)
		.SetDisplay("AC Weight H4", "Weight for H4 AC", "Momentum");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Base timeframe for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
		(Security, CandleType),
		(Security, TimeSpan.FromDays(1).TimeFrame()),
		(Security, TimeSpan.FromDays(7).TimeFrame())
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_acH4 = _acD1 = _acW1 = 0m;
		_candles.Clear();
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var stochastic = new StochasticOscillator
		{
			K = { Length = 8 },
			D = { Length = 3 },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(adx, stochastic, ProcessCandle)
		.Start();

		var daySub = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		daySub.Bind(ProcessDay).Start();

		var weekSub = SubscribeCandles(TimeSpan.FromDays(7).TimeFrame());
		weekSub.Bind(ProcessWeek).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessDay(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateAc(candle, _aoFastD1, _aoSlowD1, _acMaD1, ref _acD1);
	}

	private void ProcessWeek(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateAc(candle, _aoFastW1, _aoSlowW1, _acMaW1, ref _acW1);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxVal, IIndicatorValue stochVal)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateAc(candle, _aoFastH4, _aoSlowH4, _acMaH4, ref _acH4);

		_candles.Add(candle);
		if (_candles.Count > 4)
		_candles.RemoveAt(0);

		var adxTyped = (AverageDirectionalIndexValue)adxVal;
		if (adxTyped.MovingAverage is not decimal adx)
		return;

		var stochTyped = (StochasticOscillatorValue)stochVal;
		if (stochTyped.K is not decimal stochK || stochTyped.D is not decimal stochD)
		return;

		var algo = X1 * _acW1 + X2 * _acD1 + X3 * _acH4;
		var allowLong = !IsEveningStar() && !IsDojiCandle() && !IsBearishEngulfing();
		var allowShort = !IsMorningStar() && !IsDojiCandle() && !IsBullishEngulfing();

		if (Position == 0)
		{
			if (adx > AdxThreshold && candle.ClosePrice < candle.OpenPrice && algo > 0 && allowLong)
			{
				BuyMarket();
				InitializePositionState(candle.ClosePrice, true);
			}
			else if (adx > AdxThreshold && candle.ClosePrice > candle.OpenPrice && algo < 0 && allowShort)
			{
				SellMarket();
				InitializePositionState(candle.ClosePrice, false);
			}
			else if (adx < AdxThreshold && candle.ClosePrice < candle.OpenPrice && stochK > stochD && allowLong)
			{
				BuyMarket();
				InitializePositionState(candle.ClosePrice, true);
			}
			else if (adx < AdxThreshold && candle.ClosePrice > candle.OpenPrice && stochK < stochD && allowShort)
			{
				SellMarket();
				InitializePositionState(candle.ClosePrice, false);
			}
		}
		else if (Position > 0)
		{
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
			var trail = _highestPrice - TrailPoints;
			_stopPrice = TrailPoints > 0 ? (_stopPrice is decimal sl ? Math.Max(sl, trail) : trail) : _stopPrice;

			if ((_stopPrice is decimal stop && candle.LowPrice <= stop) ||
			(_takePrice is decimal take && candle.HighPrice >= take) ||
			(adx > AdxThreshold && algo < 0) ||
			(adx < AdxThreshold && stochK < stochD))
			{
				SellMarket(Position);
				ResetPositionState();
			}
		}
		else if (Position < 0)
		{
			_lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);
			var trail = _lowestPrice + TrailPoints;
			_stopPrice = TrailPoints > 0 ? (_stopPrice is decimal sl ? Math.Min(sl, trail) : trail) : _stopPrice;

			if ((_stopPrice is decimal stop && candle.HighPrice >= stop) ||
			(_takePrice is decimal take && candle.LowPrice <= take) ||
			(adx > AdxThreshold && algo > 0) ||
			(adx < AdxThreshold && stochK > stochD))
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}
	}

	private static void UpdateAc(ICandleMessage candle, SimpleMovingAverage fast, SimpleMovingAverage slow, SimpleMovingAverage acMa, ref decimal ac)
	{
		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var fastVal = fast.Process(median);
		var slowVal = slow.Process(median);
		if (!fastVal.IsFinal || !slowVal.IsFinal)
		return;
		var ao = fastVal.ToDecimal() - slowVal.ToDecimal();
		var maVal = acMa.Process(ao);
		if (!maVal.IsFinal)
		return;
		ac = ao - maVal.ToDecimal();
	}

	private void InitializePositionState(decimal price, bool isLong)
	{
		_entryPrice = price;
		if (isLong)
		{
			_takePrice = price + TakeProfitPoints;
			_stopPrice = price - StopLossPoints;
			_highestPrice = price;
			_lowestPrice = 0m;
		}
		else
		{
			_takePrice = price - TakeProfitPoints;
			_stopPrice = price + StopLossPoints;
			_lowestPrice = price;
			_highestPrice = 0m;
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_takePrice = null;
		_stopPrice = null;
		_highestPrice = 0m;
		_lowestPrice = 0m;
	}

	private bool IsMorningStar()
	{
		if (_candles.Count < 3)
		return false;
		var c1 = _candles[^1];
		var c2 = _candles[^2];
		var c3 = _candles[^3];
		return Body(c3) > Body(c2) &&
		Body(c1) > Body(c2) &&
		c3.ClosePrice < c3.OpenPrice &&
		c1.ClosePrice > c1.OpenPrice &&
		c1.ClosePrice > BodyLow(c3) + Body(c3) * 0.5m;
	}

	private bool IsEveningStar()
	{
		if (_candles.Count < 3)
		return false;
		var c1 = _candles[^1];
		var c2 = _candles[^2];
		var c3 = _candles[^3];
		return Body(c3) > Body(c2) &&
		Body(c1) > Body(c2) &&
		c3.ClosePrice > c3.OpenPrice &&
		c1.ClosePrice < c1.OpenPrice &&
		c1.ClosePrice < BodyHigh(c3) - Body(c3) * 0.5m;
	}

	private bool IsBullishEngulfing()
	{
		if (_candles.Count < 2)
		return false;
		var c1 = _candles[^1];
		var c2 = _candles[^2];
		return c2.ClosePrice < c2.OpenPrice &&
		c1.ClosePrice > c1.OpenPrice &&
		Body(c2) < Body(c1);
	}

	private bool IsBearishEngulfing()
	{
		if (_candles.Count < 2)
		return false;
		var c1 = _candles[^1];
		var c2 = _candles[^2];
		return c2.ClosePrice > c2.OpenPrice &&
		c1.ClosePrice < c1.OpenPrice &&
		Body(c2) < Body(c1);
	}

	private bool IsDojiCandle()
	{
		if (_candles.Count < 1)
		return false;
		var c1 = _candles[^1];
		return Body(c1) < (c1.HighPrice - c1.LowPrice) / 8.5m;
	}

	private static decimal Body(ICandleMessage c) => Math.Abs(c.OpenPrice - c.ClosePrice);
	private static decimal BodyLow(ICandleMessage c) => Math.Min(c.OpenPrice, c.ClosePrice);
	private static decimal BodyHigh(ICandleMessage c) => Math.Max(c.OpenPrice, c.ClosePrice);
}
