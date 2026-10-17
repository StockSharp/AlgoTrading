using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on crossing of two RSI lines (fast and slow).
/// </summary>
public class ColorZerolagJjrsxStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private readonly StrategyParam<bool> _buyOpen, _sellOpen, _buyClose, _sellClose;
	private Order _pending;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	public ColorZerolagJjrsxStrategy()
	{
		_buyOpen = Param(nameof(BuyOpen), true);
		_sellOpen = Param(nameof(SellOpen), true);
		_buyClose = Param(nameof(BuyClose), true);
		_sellClose = Param(nameof(SellClose), true);
		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast RSI period", "Indicator");

		_slowPeriod = Param(nameof(SlowPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow RSI period", "Indicator");

		_stopLoss = Param(nameof(StopLoss), 2m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 3m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for indicator", "General");
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
		_prevFast = null;
		_prevSlow = null;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_pending = null;

		var fastRsi = new RelativeStrengthIndex { Length = FastPeriod };
		var slowRsi = new RelativeStrengthIndex { Length = SlowPeriod };

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute),
			useMarketOrders: true);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastRsi, slowRsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastRsi);
			DrawIndicator(area, slowRsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast is null || _prevSlow is null)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var crossDown = _prevFast > _prevSlow && fast < slow;
		var crossUp = _prevFast < _prevSlow && fast > slow;

		if (IsFormedAndOnlineAndAllowTrading() && (_pending is null || _pending.State is OrderStates.Done or OrderStates.Failed))
		{
			if (crossDown)
			{
				if (Position < 0m && SellClose) _pending = BuyMarket(-Position + (BuyOpen ? Volume : 0m));
				else if (Position == 0m && BuyOpen) _pending = BuyMarket(Volume);
			}
			else if (crossUp)
			{
				if (Position > 0m && BuyClose) _pending = SellMarket(Position + (SellOpen ? Volume : 0m));
				else if (Position == 0m && SellOpen) _pending = SellMarket(Volume);
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
