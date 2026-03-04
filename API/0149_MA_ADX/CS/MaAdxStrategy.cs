using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining MA trend filter with manual ADX-like trend strength.
/// Enters when price crosses MA with strong directional movement.
/// </summary>
public class MaAdxStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
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
	/// Moving Average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	/// ADX threshold for trend strength.
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
	/// Strategy constructor.
	/// </summary>
	public MaAdxStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("MA Period", "Period of the Moving Average", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetRange(7, 21)
			.SetDisplay("ADX Period", "Period of the ADX indicator", "Indicators");

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

		// Use EMA as binding indicator to drive candle processing
		var ma = new ExponentialMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
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

		var adxPeriod = AdxPeriod;

		// Need at least adxPeriod+1 bars
		if (_closes.Count < adxPeriod + 2)
		{
			if (_cooldown > 0)
				_cooldown--;
			return;
		}

		// Manual ADX-like trend strength calculation
		decimal sumTr = 0;
		decimal sumDmPlus = 0;
		decimal sumDmMinus = 0;

		var count = _highs.Count;
		var start = count - adxPeriod;

		for (int i = start; i < count; i++)
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

		decimal trendStrength = 0;
		if (sumTr > 0)
		{
			var diPlus = 100m * sumDmPlus / sumTr;
			var diMinus = 100m * sumDmMinus / sumTr;
			var diSum = diPlus + diMinus;
			trendStrength = diSum > 0 ? 100m * Math.Abs(diPlus - diMinus) / diSum : 0;
		}

		// Keep lists manageable
		if (_highs.Count > adxPeriod * 3)
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

		// Long: price above MA + strong trend
		if (close > maValue && strongTrend && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Short: price below MA + strong trend
		else if (close < maValue && strongTrend && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: price crosses below MA
		if (Position > 0 && close < maValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: price crosses above MA
		else if (Position < 0 && close > maValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
