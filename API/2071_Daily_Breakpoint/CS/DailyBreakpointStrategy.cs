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

	private decimal _prevOpen;
	private decimal _prevClose;
	private DateTimeOffset _prevTime;
	private bool _hasPrev;
	private decimal _dayOpen;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal BreakPointPct { get => _breakPointPct.Value; set => _breakPointPct.Value = value; }
	public decimal LastBarMinPct { get => _lastBarMinPct.Value; set => _lastBarMinPct.Value = value; }
	public decimal LastBarMaxPct { get => _lastBarMaxPct.Value; set => _lastBarMaxPct.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }

	public DailyBreakpointStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		_prevOpen = default;
		_prevClose = default;
		_prevTime = default;
		_hasPrev = default;
		_dayOpen = default;
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

		var sub = SubscribeCandles(CandleType);
		sub.Bind(Process).Start();
	}

	private void Process(ICandleMessage c)
	{
		if (c.State != CandleStates.Finished)
			return;

		if (!_hasPrev || c.OpenTime.Date != _prevTime.Date)
			_dayOpen = c.OpenPrice;

		if (Position == 0 && _hasPrev)
		{
			var price = c.ClosePrice;
			var lastSize = Math.Abs(_prevClose - _prevOpen);
			var minSize = LastBarMinPct / 100m * price;
			var maxSize = LastBarMaxPct / 100m * price;
			var offset = BreakPointPct / 100m * price;
			var breakBuy = _dayOpen + offset;
			var breakSell = _dayOpen - offset;

			if (_prevClose > _prevOpen && price - _dayOpen >= offset &&
				lastSize >= minSize && lastSize <= maxSize &&
				breakBuy >= _prevOpen && breakBuy <= _prevClose)
			{
				BuyMarket();
			}
			else if (_prevClose < _prevOpen && _dayOpen - price >= offset &&
				lastSize >= minSize && lastSize <= maxSize &&
				breakSell <= _prevOpen && breakSell >= _prevClose)
			{
				SellMarket();
			}
		}

		_prevOpen = c.OpenPrice;
		_prevClose = c.ClosePrice;
		_prevTime = c.OpenTime;
		_hasPrev = true;
	}
}
