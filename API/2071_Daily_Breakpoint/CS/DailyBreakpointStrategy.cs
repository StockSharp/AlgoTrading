using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily breakout strategy based on the previous bar size and daily open.
/// </summary>
public class DailyBreakpointStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _breakPointPct;
	private readonly StrategyParam<decimal> _lastBarMinPct;
	private readonly StrategyParam<decimal> _lastBarMaxPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<decimal> _stopLossPct;

	private ICandleMessage _prev;
	private decimal _dayOpen;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal BreakPointPct { get => _breakPointPct.Value; set => _breakPointPct.Value = value; }
	public decimal LastBarMinPct { get => _lastBarMinPct.Value; set => _lastBarMinPct.Value = value; }
	public decimal LastBarMaxPct { get => _lastBarMaxPct.Value; set => _lastBarMaxPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }

	public DailyBreakpointStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
		_breakPointPct = Param(nameof(BreakPointPct), 0.3m)
			.SetDisplay("Break Point %", "Breakout offset as % of price", "General");
		_lastBarMinPct = Param(nameof(LastBarMinPct), 0.05m)
			.SetDisplay("Min Bar %", "Minimal bar size as % of price", "Filter");
		_lastBarMaxPct = Param(nameof(LastBarMaxPct), 1.0m)
			.SetDisplay("Max Bar %", "Maximum bar size as % of price", "Filter");
		_takeProfitPct = Param(nameof(TakeProfitPct), 2m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");
		_stopLossPct = Param(nameof(StopLossPct), 1m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prev = null;
		_dayOpen = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPct, UnitTypes.Percent),
			isStopTrailing: true,
			useMarketOrders: true);

		var passthrough = new SimpleMovingAverage { Length = 1 };
		var sub = SubscribeCandles(CandleType);
		sub.Bind(passthrough, (candle, _) => Process(candle)).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage c)
	{
		if (c.State != CandleStates.Finished)
			return;

		if (_prev == null || c.OpenTime.Date != _prev.OpenTime.Date)
			_dayOpen = c.OpenPrice;

		if (Position == 0 && _prev != null)
		{
			var price = c.ClosePrice;
			var lastSize = Math.Abs(_prev.ClosePrice - _prev.OpenPrice);
			var minSize = LastBarMinPct / 100m * price;
			var maxSize = LastBarMaxPct / 100m * price;
			var offset = BreakPointPct / 100m * price;
			var breakBuy = _dayOpen + offset;
			var breakSell = _dayOpen - offset;

			if (_prev.ClosePrice > _prev.OpenPrice && price - _dayOpen >= offset &&
				lastSize >= minSize && lastSize <= maxSize &&
				breakBuy >= _prev.OpenPrice && breakBuy <= _prev.ClosePrice)
			{
				BuyMarket();
			}
			else if (_prev.ClosePrice < _prev.OpenPrice && _dayOpen - price >= offset &&
				lastSize >= minSize && lastSize <= maxSize &&
				breakSell <= _prev.OpenPrice && breakSell >= _prev.ClosePrice)
			{
				SellMarket();
			}
		}

		_prev = c;
	}
}
