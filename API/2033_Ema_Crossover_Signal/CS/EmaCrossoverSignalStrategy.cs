using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma = null!;
	private ExponentialMovingAverage _slowEma = null!;

	private bool _isInitialized;
	private bool _wasFastAboveSlow;
	private decimal _tickSize;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Allow opening long positions on upward crossover.
	/// </summary>
	public bool AllowBuyOpen
	{
		get => _allowBuyOpen.Value;
		set => _allowBuyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening short positions on downward crossover.
	/// </summary>
	public bool AllowSellOpen
	{
		get => _allowSellOpen.Value;
		set => _allowSellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing long positions on downward crossover.
	/// </summary>
	public bool AllowBuyClose
	{
		get => _allowBuyClose.Value;
		set => _allowBuyClose.Value = value;
	}

	/// <summary>
	/// Allow closing short positions on upward crossover.
	/// </summary>
	public bool AllowSellClose
	{
		get => _allowSellClose.Value;
		set => _allowSellClose.Value = value;
	}

	/// <summary>
	/// Take profit distance in ticks.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Stop loss distance in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EmaCrossoverSignalStrategy"/>.
	/// </summary>
	public EmaCrossoverSignalStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Fast Period", "Length of the fast EMA", "EMA")
		.SetCanOptimize(true)
		.SetOptimize(2, 20, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("Slow Period", "Length of the slow EMA", "EMA")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_allowBuyOpen = Param(nameof(AllowBuyOpen), true)
		.SetDisplay("Allow Buy Open", "Open long on upward crossover", "Trading");

		_allowSellOpen = Param(nameof(AllowSellOpen), true)
		.SetDisplay("Allow Sell Open", "Open short on downward crossover", "Trading");

		_allowBuyClose = Param(nameof(AllowBuyClose), true)
		.SetDisplay("Allow Buy Close", "Close long on downward crossover", "Trading");

		_allowSellClose = Param(nameof(AllowSellClose), true)
		.SetDisplay("Allow Sell Close", "Close short on upward crossover", "Trading");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 2000)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit Ticks", "Take profit distance in ticks", "Risk");

		_stopLossTicks = Param(nameof(StopLossTicks), 1000)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss Ticks", "Stop loss distance in ticks", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security.PriceStep ?? 1m;
		_fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastEma, _slowEma, Process)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_isInitialized)
		{
			if (_fastEma.IsFormed && _slowEma.IsFormed)
			{
				_wasFastAboveSlow = fastValue > slowValue;
				_isInitialized = true;
			}
			return;
		}

		var isFastAboveSlow = fastValue > slowValue;

		if (_wasFastAboveSlow != isFastAboveSlow)
		{
			if (isFastAboveSlow)
			{
				// Upward crossover
				if (AllowSellClose && Position < 0)
				{
					CancelActiveOrders();
					BuyMarket(Math.Abs(Position));
				}

				if (AllowBuyOpen && Position <= 0)
				{
					CancelActiveOrders();
					var volume = Volume + Math.Max(0m, -Position);
					BuyMarket(volume);
					PlaceRiskOrders(candle.ClosePrice, true, volume);
				}
			}
		else
		{
			// Downward crossover
			if (AllowBuyClose && Position > 0)
			{
				CancelActiveOrders();
				SellMarket(Math.Abs(Position));
			}

			if (AllowSellOpen && Position >= 0)
			{
				CancelActiveOrders();
				var volume = Volume + Math.Max(0m, Position);
				SellMarket(volume);
				PlaceRiskOrders(candle.ClosePrice, false, volume);
			}
		}

		_wasFastAboveSlow = isFastAboveSlow;
	}
}

private void PlaceRiskOrders(decimal entryPrice, bool isLong, decimal volume)
{
	if (TakeProfitTicks > 0)
	{
		var tp = isLong ? entryPrice + TakeProfitTicks * _tickSize : entryPrice - TakeProfitTicks * _tickSize;
		if (isLong)
		SellLimit(tp, volume);
	else
	BuyLimit(tp, volume);
}

if (StopLossTicks > 0)
{
	var sl = isLong ? entryPrice - StopLossTicks * _tickSize : entryPrice + StopLossTicks * _tickSize;
	if (isLong)
	SellStop(sl, volume);
else
BuyStop(sl, volume);
}
}
}