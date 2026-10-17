using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades EMA crossovers with optional separate entry and exit permissions for long and short positions.
/// </summary>
public class EmaCrossoverSignalStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly StrategyParam<bool> _allowBuyOpen, _allowSellOpen, _allowBuyClose, _allowSellClose;
	private readonly StrategyParam<decimal> _stopLossTicks, _takeProfitTicks;
	private Order _pending;
	private bool _isInitialized;
	private bool _wasFastAboveSlow;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public bool AllowBuyOpen { get => _allowBuyOpen.Value; set => _allowBuyOpen.Value = value; }
	public bool AllowSellOpen { get => _allowSellOpen.Value; set => _allowSellOpen.Value = value; }
	public bool AllowBuyClose { get => _allowBuyClose.Value; set => _allowBuyClose.Value = value; }
	public bool AllowSellClose { get => _allowSellClose.Value; set => _allowSellClose.Value = value; }
	public decimal StopLossTicks { get => _stopLossTicks.Value; set => _stopLossTicks.Value = value; }
	public decimal TakeProfitTicks { get => _takeProfitTicks.Value; set => _takeProfitTicks.Value = value; }
	public EmaCrossoverSignalStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Length of the fast EMA", "EMA");

		_slowPeriod = Param(nameof(SlowPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Length of the slow EMA", "EMA");

		_allowBuyOpen = Param(nameof(AllowBuyOpen), true);
		_allowSellOpen = Param(nameof(AllowSellOpen), true);
		_allowBuyClose = Param(nameof(AllowBuyClose), true);
		_allowSellClose = Param(nameof(AllowSellClose), true);
		_stopLossTicks = Param(nameof(StopLossTicks), 1000m).SetNotNegative();
		_takeProfitTicks = Param(nameof(TakeProfitTicks), 2000m).SetNotNegative();
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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
		_isInitialized = default;
		_wasFastAboveSlow = default;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pending = null;
		var step = Security?.PriceStep ?? 1m;
		StartProtection(takeProfit: new Unit(TakeProfitTicks * step, UnitTypes.Absolute), stopLoss: new Unit(StopLossTicks * step, UnitTypes.Absolute), useMarketOrders: true);
		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, Process)
			.Start();
	}

	private void Process(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			_wasFastAboveSlow = fastValue > slowValue;
			_isInitialized = true;
			return;
		}

		var isFastAboveSlow = fastValue > slowValue;

		var crossing = _wasFastAboveSlow != isFastAboveSlow;
		_wasFastAboveSlow = isFastAboveSlow;
		if (!crossing || !IsFormedAndOnlineAndAllowTrading() || (_pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed))) return;
		if (isFastAboveSlow)
		{
			if (Position < 0m && AllowSellClose) _pending = BuyMarket(-Position + (AllowBuyOpen ? Volume : 0m));
			else if (Position == 0m && AllowBuyOpen) _pending = BuyMarket(Volume);
		}
		else
		{
			if (Position > 0m && AllowBuyClose) _pending = SellMarket(Position + (AllowSellOpen ? Volume : 0m));
			else if (Position == 0m && AllowSellOpen) _pending = SellMarket(Volume);
		}
	}
}
