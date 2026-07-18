namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Triple Supertrend strategy using three ATR-based bands.
/// Buys when all three bands show uptrend.
/// Sells when all three bands show downtrend.
/// </summary>
public class TripleSupertrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candle;
	private readonly StrategyParam<int> _p1, _p2, _p3;
	private readonly StrategyParam<decimal> _f1, _f2, _f3;
	private readonly StrategyParam<int> _cooldownBars;

	private int _cooldownRemaining;

	public DataType CandleType { get => _candle.Value; set => _candle.Value = value; }
	public int AtrPeriod1 { get => _p1.Value; set => _p1.Value = value; }
	public decimal Factor1 { get => _f1.Value; set => _f1.Value = value; }
	public int AtrPeriod2 { get => _p2.Value; set => _p2.Value = value; }
	public decimal Factor2 { get => _f2.Value; set => _f2.Value = value; }
	public int AtrPeriod3 { get => _p3.Value; set => _p3.Value = value; }
	public decimal Factor3 { get => _f3.Value; set => _f3.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public TripleSupertrendStrategy()
	{
		_candle = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_p1 = Param(nameof(AtrPeriod1), 11)
			.SetDisplay("ATR1", "Fast ATR", "Supertrend");
		_f1 = Param(nameof(Factor1), 1m)
			.SetDisplay("Factor1", "Fast factor", "Supertrend");
		_p2 = Param(nameof(AtrPeriod2), 12)
			.SetDisplay("ATR2", "Medium ATR", "Supertrend");
		_f2 = Param(nameof(Factor2), 2m)
			.SetDisplay("Factor2", "Medium factor", "Supertrend");
		_p3 = Param(nameof(AtrPeriod3), 13)
			.SetDisplay("ATR3", "Slow ATR", "Supertrend");
		_f3 = Param(nameof(Factor3), 3m)
			.SetDisplay("Factor3", "Slow factor", "Supertrend");
		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_cooldownRemaining = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema1 = new ExponentialMovingAverage { Length = AtrPeriod1 };
		var ema2 = new ExponentialMovingAverage { Length = AtrPeriod2 };
		var ema3 = new ExponentialMovingAverage { Length = AtrPeriod3 };
		var atr1 = new AverageTrueRange { Length = AtrPeriod1 };
		var atr2 = new AverageTrueRange { Length = AtrPeriod2 };
		var atr3 = new AverageTrueRange { Length = AtrPeriod3 };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(ema1, ema2, ema3, atr1, atr2, atr3, Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, ema1);
			DrawIndicator(area, ema2);
			DrawIndicator(area, ema3);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage c, decimal m1, decimal m2, decimal m3, decimal a1, decimal a2, decimal a3)
	{
		if (c.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var close = c.ClosePrice;
		var lower1 = m1 - a1 * Factor1;
		var lower2 = m2 - a2 * Factor2;
		var lower3 = m3 - a3 * Factor3;
		var upper1 = m1 + a1 * Factor1;
		var upper2 = m2 + a2 * Factor2;
		var upper3 = m3 + a3 * Factor3;

		var up1 = close > lower1;
		var up2 = close > lower2;
		var up3 = close > lower3;
		var dn1 = close < upper1;
		var dn2 = close < upper2;
		var dn3 = close < upper3;

		// Buy: all three bands show uptrend
		if (up1 && up2 && up3 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell: all three bands show downtrend
		else if (!up1 && !up2 && !up3 && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: majority bands flip bearish
		else if (Position > 0 && !up1 && !up2)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: majority bands flip bullish
		else if (Position < 0 && up1 && up2)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
