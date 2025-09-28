using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// OHLC check strategy that opens positions based on the previous candle body.
/// </summary>
public class OhlcCheckStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<decimal> _spreadLimitPips;
	private readonly StrategyParam<int> _signalShift;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;

	private readonly List<(decimal Open, decimal Close)> _history = new();
	private decimal _bestBid;
	private decimal _bestAsk;
	private decimal _pipStep = 1m;

	/// <summary>
	/// Initializes a new instance of the <see cref="OhlcCheckStrategy"/> class.
	/// </summary>
	public OhlcCheckStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signals", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance expressed in pips", "Risk")
			.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
			.SetDisplay("Take Profit (pips)", "Take profit distance expressed in pips", "Risk")
			.SetNotNegative();

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Trade opposite direction of the candle body", "General");

		_spreadLimitPips = Param(nameof(SpreadLimitPips), 1m)
			.SetDisplay("Spread Limit (pips)", "Maximum allowed spread before opening", "Risk")
			.SetNotNegative();

		_signalShift = Param(nameof(SignalShift), 1)
			.SetDisplay("Signal Shift", "How many closed candles back to evaluate", "General")
			.SetGreaterOrEqualTo(0);

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetDisplay("Order Volume", "Volume used for market orders", "Trading")
			.SetGreaterThanZero();
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trade the opposite direction of the detected signal.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread (in pips) when opening new trades.
	/// </summary>
	public decimal SpreadLimitPips
	{
		get => _spreadLimitPips.Value;
		set => _spreadLimitPips.Value = value;
	}

	/// <summary>
	/// How many closed candles back are used for signal detection.
	/// </summary>
	public int SignalShift
	{
		get => _signalShift.Value;
		set => _signalShift.Value = value;
	}

	/// <summary>
	/// Candle type used for signal generation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Volume used when sending market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
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

		_history.Clear();
		_bestBid = 0m;
		_bestAsk = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdatePipStep();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		SubscribeOrderBook()
			.Bind(depth =>
			{
				var bid = depth.GetBestBid();
				if (bid != null)
					_bestBid = bid.Price;

				var ask = depth.GetBestAsk();
				if (ask != null)
					_bestAsk = ask.Price;
			})
			.Start();

		var takeProfitUnit = CreateTakeProfitUnit();
		var stopLossUnit = CreateStopLossUnit();

		if (takeProfitUnit != null || stopLossUnit != null)
			StartProtection(takeProfitUnit, stopLossUnit);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Store the closed candle for shift-based access.
		_history.Add((candle.OpenPrice, candle.ClosePrice));

		var shift = SignalShift;
		if (shift < 0)
			shift = 0;

		var minCount = shift + 1;
		if (_history.Count < minCount)
			return;

		TrimHistory(minCount + 1);

		var targetIndex = _history.Count - 1 - shift;
		if (targetIndex < 0)
			return;

		var target = _history[targetIndex];

		var signal = 0;
		if (target.Close > target.Open)
			signal = 1;
		else if (target.Close < target.Open)
			signal = -1;

		if (signal == 0)
			return;

		if (ReverseSignals)
			signal = -signal;

		if (Position == 0)
		{
			if (signal > 0)
				TryOpenLong();
			else
				TryOpenShort();
		}
		else
		{
			if (signal > 0 && Position < 0)
				BuyMarket(-Position); // Close short positions on bullish signal.
			else if (signal < 0 && Position > 0)
				SellMarket(Position); // Close long positions on bearish signal.
		}
	}

	private void TryOpenLong()
	{
		if (!CanOpenPosition())
			return;

		// Enter long position at market price.
		BuyMarket(OrderVolume);
	}

	private void TryOpenShort()
	{
		if (!CanOpenPosition())
			return;

		// Enter short position at market price.
		SellMarket(OrderVolume);
	}

	private bool CanOpenPosition()
	{
		if (SpreadLimitPips <= 0m)
			return true;

		if (_bestBid <= 0m || _bestAsk <= 0m)
			return false;

		var spread = _bestAsk - _bestBid;
		var limit = SpreadLimitPips * _pipStep;

		return spread <= limit;
	}

	private void UpdatePipStep()
	{
		var step = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals;

		if (decimals == 3 || decimals == 5)
			step *= 10m;

		_pipStep = step;
	}

	private void TrimHistory(int maxCount)
	{
		if (maxCount <= 0)
			return;

		while (_history.Count > maxCount)
			_history.RemoveAt(0);
	}

	private Unit CreateTakeProfitUnit()
	{
		if (TakeProfitPips <= 0m)
			return null;

		return new Unit(TakeProfitPips * _pipStep, UnitTypes.Point);
	}

	private Unit CreateStopLossUnit()
	{
		if (StopLossPips <= 0m)
			return null;

		return new Unit(StopLossPips * _pipStep, UnitTypes.Point);
	}
}