using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Bollinger Bands with manual ADX trend strength.
/// Buys on upper band breakout with strong trend, sells on lower band breakout.
/// </summary>
public class BollingerAdxStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _cooldownBars;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private readonly List<decimal> _closes = new();
	private int _cooldown;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold for strong trend.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initialize strategy.
	/// </summary>
	public BollingerAdxStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetRange(10, 30)
			.SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetRange(7, 21)
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "ADX level for strong trend", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 100)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(5, 500);
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
		_highs.Clear();
		_lows.Clear();
		_closes.Clear();
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(bb, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		_highs.Add(high);
		_lows.Add(low);
		_closes.Add(close);

		var bbTyped = (BollingerBandsValue)bbValue;
		if (bbTyped.UpBand is not decimal upperBand || bbTyped.LowBand is not decimal lowerBand || bbTyped.MovingAverage is not decimal middleBand)
			return;

		var adxPeriod = AdxPeriod;

		// Calculate manual ADX trend strength
		decimal trendStrength = 0;
		if (_closes.Count >= adxPeriod + 2)
		{
			decimal sumTr = 0, sumDmPlus = 0, sumDmMinus = 0;
			var count = _highs.Count;
			for (int i = count - adxPeriod; i < count; i++)
			{
				var h = _highs[i];
				var l = _lows[i];
				var prevC = _closes[i - 1];
				var prevH = _highs[i - 1];
				var prevL = _lows[i - 1];

				var tr = Math.Max(h - l, Math.Max(Math.Abs(h - prevC), Math.Abs(l - prevC)));
				sumTr += tr;

				var upMove = h - prevH;
				var downMove = prevL - l;

				if (upMove > downMove && upMove > 0)
					sumDmPlus += upMove;
				if (downMove > upMove && downMove > 0)
					sumDmMinus += downMove;
			}

			if (sumTr > 0)
			{
				var diPlus = 100m * sumDmPlus / sumTr;
				var diMinus = 100m * sumDmMinus / sumTr;
				var diSum = diPlus + diMinus;
				trendStrength = diSum > 0 ? 100m * Math.Abs(diPlus - diMinus) / diSum : 0;
			}
		}

		// Trim lists
		var maxKeep = adxPeriod * 3;
		if (_highs.Count > maxKeep)
		{
			var trim = _highs.Count - adxPeriod * 2;
			_highs.RemoveRange(0, trim);
			_lows.RemoveRange(0, trim);
			_closes.RemoveRange(0, trim);
		}

		var strongTrend = trendStrength > AdxThreshold;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Buy: price above upper band + strong trend
		if (close > upperBand && strongTrend && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Sell: price below lower band + strong trend
		else if (close < lowerBand && strongTrend && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: price returns to middle band
		if (Position > 0 && close < middleBand)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: price returns to middle band
		else if (Position < 0 && close > middleBand)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
