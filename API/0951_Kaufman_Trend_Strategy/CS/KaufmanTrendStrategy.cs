using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Kaufman Trend strategy.
/// </summary>
public class KaufmanTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _takeProfit1Percent;
	private readonly StrategyParam<int> _takeProfit2Percent;
	private readonly StrategyParam<int> _takeProfit3Percent;
	private readonly StrategyParam<int> _swingLookback;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _trendStrengthEntry;
	private readonly StrategyParam<int> _trendStrengthExit;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private Highest _highest;
	private Lowest _lowest;
	private WeightedMovingAverage _trendWma;

	private decimal _filteredSrc;
	private decimal _oscillator;
	private decimal _p00 = 1m;
	private decimal _p01;
	private decimal _p10;
	private decimal _p11 = 1m;

	private const decimal ProcessNoise1 = 0.01m;
	private const decimal ProcessNoise2 = 0.01m;
	private const decimal MeasurementNoise = 500m;
	private const int OscBufferLength = 10;
	private const int R2 = 10;

	private readonly Queue<decimal> _oscBuffer = new();
	private decimal _prevTrendStrength;
	private bool _inLong;
	private bool _inShort;
	private bool _longTp1Hit;
	private bool _longTp2Hit;
	private bool _shortTp1Hit;
	private bool _shortTp2Hit;

	/// <summary>
	/// Initializes <see cref="KaufmanTrendStrategy"/>.
	/// </summary>
	public KaufmanTrendStrategy()
	{
		_takeProfit1Percent = Param(nameof(TakeProfit1Percent), 50)
		.SetDisplay("1st TP %", "First take profit percent", "Take Profit")
		.SetCanOptimize(true)
		.SetOptimize(10, 80, 5);

		_takeProfit2Percent = Param(nameof(TakeProfit2Percent), 25)
		.SetDisplay("2nd TP %", "Second take profit percent", "Take Profit")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 5);

		_takeProfit3Percent = Param(nameof(TakeProfit3Percent), 25)
		.SetDisplay("Final TP %", "Final take profit percent", "Take Profit")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 5);

		_swingLookback = Param(nameof(SwingLookback), 10)
		.SetDisplay("Swing Lookback", "Bars for swing high/low", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetDisplay("ATR Period", "ATR calculation period", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(7, 28, 1);

		_trendStrengthEntry = Param(nameof(TrendStrengthEntry), 60)
		.SetDisplay("Trend Strength Entry", "Entry threshold", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(30, 80, 5);

		_trendStrengthExit = Param(nameof(TrendStrengthExit), 40)
		.SetDisplay("Trend Strength Exit", "Exit threshold", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <summary>
	/// First take profit percent.
	/// </summary>
	public int TakeProfit1Percent
	{
		get => _takeProfit1Percent.Value;
		set => _takeProfit1Percent.Value = value;
	}

	/// <summary>
	/// Second take profit percent.
	/// </summary>
	public int TakeProfit2Percent
	{
		get => _takeProfit2Percent.Value;
		set => _takeProfit2Percent.Value = value;
	}

	/// <summary>
	/// Final take profit percent.
	/// </summary>
	public int TakeProfit3Percent
	{
		get => _takeProfit3Percent.Value;
		set => _takeProfit3Percent.Value = value;
	}

	/// <summary>
	/// Swing high/low lookback.
	/// </summary>
	public int SwingLookback
	{
		get => _swingLookback.Value;
		set => _swingLookback.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Trend strength entry threshold.
	/// </summary>
	public int TrendStrengthEntry
	{
		get => _trendStrengthEntry.Value;
		set => _trendStrengthEntry.Value = value;
	}

	/// <summary>
	/// Trend strength exit threshold.
	/// </summary>
	public int TrendStrengthExit
	{
		get => _trendStrengthExit.Value;
		set => _trendStrengthExit.Value = value;
	}

	/// <summary>
	/// Candle type for the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_atr = null;
		_highest = null;
		_lowest = null;
		_trendWma = null;
		_filteredSrc = 0m;
		_oscillator = 0m;
		_p00 = 1m;
		_p01 = 0m;
		_p10 = 0m;
		_p11 = 1m;
		_oscBuffer.Clear();
		_prevTrendStrength = 0m;
		_inLong = false;
		_inShort = false;
		_longTp1Hit = false;
		_longTp2Hit = false;
		_shortTp1Hit = false;
		_shortTp2Hit = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_highest = new Highest { Length = SwingLookback };
		_lowest = new Lowest { Length = SwingLookback };
		_trendWma = new WeightedMovingAverage { Length = R2 };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_atr, _highest, _lowest, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal swingHigh, decimal swingLow)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateKalman(candle.ClosePrice);

		_oscBuffer.Enqueue(_oscillator);
		if (_oscBuffer.Count > OscBufferLength)
		_oscBuffer.Dequeue();

		decimal maxAbs = 0m;
		foreach (var v in _oscBuffer)
		{
			var abs = Math.Abs(v);
			if (abs > maxAbs)
			maxAbs = abs;
		}

		var trendRaw = maxAbs > 0m ? _oscillator / maxAbs * 100m : 0m;
		var trendValue = _trendWma.Process(trendRaw);
		if (!trendValue.IsFinal)
		return;
		var trendStrength = trendValue.GetValue<decimal>();

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var maGreen = trendStrength > 0m;
		var maRed = trendStrength < 0m;
		var maBlue = Math.Abs(trendStrength) < 10m;

		var priceAboveMa = candle.ClosePrice > _filteredSrc;
		var priceBelowMa = candle.ClosePrice < _filteredSrc;

		var trendStrongLong = trendStrength >= TrendStrengthEntry;
		var trendStrongShort = trendStrength <= -TrendStrengthEntry;
		var trendWeakLong = trendStrength < TrendStrengthExit;
		var trendWeakShort = trendStrength > -TrendStrengthExit;

		var oscCrossDown = _prevTrendStrength >= 0m && trendStrength < 0m;
		var oscCrossUp = _prevTrendStrength <= 0m && trendStrength > 0m;
		_prevTrendStrength = trendStrength;

		var longStop = swingLow - atrValue;
		var shortStop = swingHigh + atrValue;

		if (!_inLong && trendStrongLong && maGreen && priceAboveMa)
		{
			BuyMarket(Volume);
			_inLong = true;
			_longTp1Hit = false;
			_longTp2Hit = false;
		}
		else if (!_inShort && trendStrongShort && maRed && priceBelowMa)
		{
			SellMarket(Volume);
			_inShort = true;
			_shortTp1Hit = false;
			_shortTp2Hit = false;
		}

		if (_inLong)
		{
			if (candle.LowPrice <= longStop)
			{
				SellMarket(Position);
				_inLong = false;
			}
			else
			{
				if (!_longTp1Hit && maBlue)
				{
					var qty = Math.Abs(Position) * TakeProfit1Percent / 100m;
					SellMarket(qty);
					_longTp1Hit = true;
				}
				if (!_longTp2Hit && oscCrossDown)
				{
					var qty = Math.Abs(Position) * TakeProfit2Percent / 100m;
					SellMarket(qty);
					_longTp2Hit = true;
				}
				if (trendWeakLong)
				{
					SellMarket(Position);
					_inLong = false;
				}
			}
		}
		else if (_inShort)
		{
			if (candle.HighPrice >= shortStop)
			{
				BuyMarket(Math.Abs(Position));
				_inShort = false;
			}
			else
			{
				if (!_shortTp1Hit && maBlue)
				{
					var qty = Math.Abs(Position) * TakeProfit1Percent / 100m;
					BuyMarket(qty);
					_shortTp1Hit = true;
				}
				if (!_shortTp2Hit && oscCrossUp)
				{
					var qty = Math.Abs(Position) * TakeProfit2Percent / 100m;
					BuyMarket(qty);
					_shortTp2Hit = true;
				}
				if (trendWeakShort)
				{
					BuyMarket(Math.Abs(Position));
					_inShort = false;
				}
			}
		}
	}

	private void UpdateKalman(decimal price)
	{
		_filteredSrc += _oscillator;

		var p00 = _p00;
		var p01 = _p01;
		var p10 = _p10;
		var p11 = _p11;

		var p00p = p00 + p01 + p10 + p11 + ProcessNoise1;
		var p01p = p01 + p11;
		var p10p = p10 + p11;
		var p11p = p11 + ProcessNoise2;

		var s = p00p + MeasurementNoise;
		var k0 = p00p / s;
		var k1 = p10p / s;
		var innovation = price - _filteredSrc;

		_filteredSrc += k0 * innovation;
		_oscillator += k1 * innovation;

		_p00 = (1 - k0) * p00p;
		_p01 = (1 - k0) * p01p;
		_p10 = p10p - k1 * p00p;
		_p11 = p11p - k1 * p01p;
	}
}
