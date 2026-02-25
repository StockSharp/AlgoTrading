using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Millenium Code positional strategy.
/// Uses fast/slow MA crossover with high/low channel filter.
/// </summary>
public class MilleniumCodeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _highLowBars;
	private readonly StrategyParam<bool> _reverseSignal;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;

	private Highest _highest;
	private Lowest _lowest;
	private decimal _prevFast;
	private decimal _prevSlow;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int HighLowBars { get => _highLowBars.Value; set => _highLowBars.Value = value; }
	public bool ReverseSignal { get => _reverseSignal.Value; set => _reverseSignal.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }

	public MilleniumCodeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_fastLength = Param(nameof(FastLength), 10)
			.SetDisplay("Fast MA", "Fast moving average length", "Indicators");
		_slowLength = Param(nameof(SlowLength), 30)
			.SetDisplay("Slow MA", "Slow moving average length", "Indicators");
		_highLowBars = Param(nameof(HighLowBars), 10)
			.SetDisplay("HighLow Bars", "Bars count for high/low search", "Indicators");
		_reverseSignal = Param(nameof(ReverseSignal), true)
			.SetDisplay("Reverse", "Reverse buy/sell logic", "General");
		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");
		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0m;
		_prevSlow = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new SimpleMovingAverage { Length = FastLength };
		var slow = new SimpleMovingAverage { Length = SlowLength };
		_highest = new Highest { Length = HighLowBars };
		_lowest = new Lowest { Length = HighLowBars };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fast, slow, (candle, fastVal, slowVal) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			var highResult = _highest.Process(candle);
			var lowResult = _lowest.Process(candle);

			if (!highResult.IsFormed || !lowResult.IsFormed)
			{
				_prevFast = fastVal;
				_prevSlow = slowVal;
				return;
			}

			var high = highResult.ToDecimal();
			var low = lowResult.ToDecimal();

			if (_prevFast == 0 || _prevSlow == 0)
			{
				_prevFast = fastVal;
				_prevSlow = slowVal;
				return;
			}

			var crossUp = _prevFast < _prevSlow && fastVal > slowVal;
			var crossDown = _prevFast > _prevSlow && fastVal < slowVal;

			var dir = 0;
			if (crossUp) dir = ReverseSignal ? -1 : 1;
			else if (crossDown) dir = ReverseSignal ? 1 : -1;

			if (dir == 1 && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (dir == -1 && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}

			_prevFast = fastVal;
			_prevSlow = slowVal;
		}).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}
}
