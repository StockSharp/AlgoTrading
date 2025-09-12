using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class TurtleTraderStrategy : Strategy
{
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopMultiplier;
	private readonly StrategyParam<decimal> _pyramidProfit;
	private readonly StrategyParam<int> _s1Long;
	private readonly StrategyParam<int> _s2Long;
	private readonly StrategyParam<int> _s1LongExit;
	private readonly StrategyParam<int> _s2LongExit;
	private readonly StrategyParam<int> _s1Short;
	private readonly StrategyParam<int> _s2Short;
	private readonly StrategyParam<int> _s1ShortExit;
	private readonly StrategyParam<int> _s2ShortExit;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private Highest _dayHighS1;
	private Highest _dayHighS2;
	private Lowest _dayLowS1;
	private Lowest _dayLowS2;
	private Lowest _exitLongS1;
	private Lowest _exitLongS2;
	private Highest _exitShortS1;
	private Highest _exitShortS2;

	private decimal _buyPriceLong;
	private decimal _buyPriceShort;
	private decimal _stopLossLong;
	private decimal _stopLossShort;
	private bool _skip;
	private string _lastEntry;

	public decimal RiskPercent { get => _riskPercent.Value; set => _riskPercent.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal StopMultiplier { get => _stopMultiplier.Value; set => _stopMultiplier.Value = value; }
	public decimal PyramidProfit { get => _pyramidProfit.Value; set => _pyramidProfit.Value = value; }
	public int S1Long { get => _s1Long.Value; set => _s1Long.Value = value; }
	public int S2Long { get => _s2Long.Value; set => _s2Long.Value = value; }
	public int S1LongExit { get => _s1LongExit.Value; set => _s1LongExit.Value = value; }
	public int S2LongExit { get => _s2LongExit.Value; set => _s2LongExit.Value = value; }
	public int S1Short { get => _s1Short.Value; set => _s1Short.Value = value; }
	public int S2Short { get => _s2Short.Value; set => _s2Short.Value = value; }
	public int S1ShortExit { get => _s1ShortExit.Value; set => _s1ShortExit.Value = value; }
	public int S2ShortExit { get => _s2ShortExit.Value; set => _s2ShortExit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TurtleTraderStrategy()
	{
		_riskPercent = Param(nameof(RiskPercent), 1m)
		.SetDisplay("Risk Percent", "Capital risk percent", "General")
		.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR length", "General")
		.SetCanOptimize(true);

		_stopMultiplier = Param(nameof(StopMultiplier), 1.5m)
		.SetDisplay("Stop Multiplier", "ATR multiplier for stop", "General")
		.SetCanOptimize(true);

		_pyramidProfit = Param(nameof(PyramidProfit), 0.5m)
		.SetDisplay("Pyramid Profit", "ATR move to add", "General")
		.SetCanOptimize(true);

		_s1Long = Param(nameof(S1Long), 20)
		.SetGreaterThanZero()
		.SetDisplay("S1 Long", "System1 long breakout", "General")
		.SetCanOptimize(true);

		_s2Long = Param(nameof(S2Long), 55)
		.SetGreaterThanZero()
		.SetDisplay("S2 Long", "System2 long breakout", "General")
		.SetCanOptimize(true);

		_s1LongExit = Param(nameof(S1LongExit), 10)
		.SetGreaterThanZero()
		.SetDisplay("S1 Long Exit", "System1 long exit", "General")
		.SetCanOptimize(true);

		_s2LongExit = Param(nameof(S2LongExit), 20)
		.SetGreaterThanZero()
		.SetDisplay("S2 Long Exit", "System2 long exit", "General")
		.SetCanOptimize(true);

		_s1Short = Param(nameof(S1Short), 15)
		.SetGreaterThanZero()
		.SetDisplay("S1 Short", "System1 short breakout", "General")
		.SetCanOptimize(true);

		_s2Short = Param(nameof(S2Short), 55)
		.SetGreaterThanZero()
		.SetDisplay("S2 Short", "System2 short breakout", "General")
		.SetCanOptimize(true);

		_s1ShortExit = Param(nameof(S1ShortExit), 7)
		.SetGreaterThanZero()
		.SetDisplay("S1 Short Exit", "System1 short exit", "General")
		.SetCanOptimize(true);

		_s2ShortExit = Param(nameof(S2ShortExit), 20)
		.SetGreaterThanZero()
		.SetDisplay("S2 Short Exit", "System2 short exit", "General")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_atr = default;
		_dayHighS1 = default;
		_dayHighS2 = default;
		_dayLowS1 = default;
		_dayLowS2 = default;
		_exitLongS1 = default;
		_exitLongS2 = default;
		_exitShortS1 = default;
		_exitShortS2 = default;

		_buyPriceLong = 0m;
		_buyPriceShort = 0m;
		_stopLossLong = 0m;
		_stopLossShort = 0m;
		_skip = false;
		_lastEntry = string.Empty;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_dayHighS1 = new Highest { Length = S1Long };
		_dayHighS2 = new Highest { Length = S2Long };
		_dayLowS1 = new Lowest { Length = S1Short };
		_dayLowS2 = new Lowest { Length = S2Short };
		_exitLongS1 = new Lowest { Length = S1LongExit };
		_exitLongS2 = new Lowest { Length = S2LongExit };
		_exitShortS1 = new Highest { Length = S1ShortExit };
		_exitShortS2 = new Highest { Length = S2ShortExit };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_atr, _dayHighS1, _dayHighS2, _dayLowS1, _dayLowS2,
		_exitLongS1, _exitLongS2, _exitShortS1, _exitShortS2, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _dayHighS1);
			DrawIndicator(area, _dayHighS2);
			DrawIndicator(area, _dayLowS1);
			DrawIndicator(area, _dayLowS2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr, decimal highS1, decimal highS2,
	decimal lowS1, decimal lowS2, decimal exitLongS1, decimal exitLongS2,
	decimal exitShortS1, decimal exitShortS2)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (atr <= 0)
		return;

		var account = Portfolio.CurrentValue ?? 0m;
		if (account <= 0)
		return;

		var unit = RiskPercent / 100m * account / atr;
		if (unit <= 0)
		return;

		if (Position == 0)
		{
			if (!_skip)
			{
				if (candle.HighPrice >= highS1)
				{
					_stopLossLong = Math.Max(highS1 * 0.9m, highS1 - StopMultiplier * atr);
					BuyStop(highS1, unit);
					_buyPriceLong = highS1;
					_lastEntry = "Long1";
				}
				else if (candle.LowPrice <= lowS1)
				{
					_stopLossShort = Math.Min(lowS1 * 1.1m, lowS1 + StopMultiplier * atr);
					SellStop(lowS1, unit);
					_buyPriceShort = lowS1;
					_lastEntry = "Short1";
				}
			}
			else
			{
				if (candle.HighPrice >= highS2)
				{
					_stopLossLong = Math.Max(highS2 * 0.9m, highS2 - StopMultiplier * atr);
					BuyStop(highS2, unit);
					_buyPriceLong = highS2;
					_lastEntry = "Long2";
					_skip = false;
				}
				else if (candle.LowPrice <= lowS2)
				{
					_stopLossShort = Math.Min(lowS2 * 1.1m, lowS2 + StopMultiplier * atr);
					SellStop(lowS2, unit);
					_buyPriceShort = lowS2;
					_lastEntry = "Short2";
					_skip = false;
				}
			}
		}
		else if (Position > 0)
		{
			var exit = _lastEntry == "Long1" ? Math.Max(exitLongS1, _stopLossLong) : Math.Max(exitLongS2, _stopLossLong);

			if (candle.LowPrice <= exit)
			{
				SellStop(Position, exit);
				_skip = exit > _buyPriceLong;
			}

			if (candle.ClosePrice >= _buyPriceLong + PyramidProfit * atr)
			{
				var remaining = account - Position * candle.ClosePrice;
				var add = RiskPercent / 100m * remaining / atr;
				if (remaining > add && add > 0)
				{
					_stopLossLong += PyramidProfit * atr;
					BuyMarket(add);
					_buyPriceLong = candle.ClosePrice;
				}
			}
		}
		else if (Position < 0)
		{
			var exit = _lastEntry == "Short1" ? Math.Min(exitShortS1, _stopLossShort) : Math.Min(exitShortS2, _stopLossShort);

			if (candle.HighPrice >= exit)
			{
				BuyStop(-Position, exit);
				_skip = exit < _buyPriceShort;
			}

			if (candle.ClosePrice <= _buyPriceShort - PyramidProfit * atr)
			{
				var remaining = account + Position * candle.ClosePrice;
				var add = RiskPercent / 100m * remaining / atr;
				if (remaining > add && add > 0)
				{
					_stopLossShort -= PyramidProfit * atr;
					SellMarket(add);
					_buyPriceShort = candle.ClosePrice;
				}
			}
		}
	}
}
