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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy based on the Price Extreme indicator.
/// </summary>
public class PriceExtremeStrategy : Strategy
{
	private readonly StrategyParam<int> _levelLength;
	private readonly StrategyParam<int> _signalShift;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _history = new();
	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _prevPosition;
	private decimal _entryPrice;
	private decimal _prevUpper;
	private decimal _prevLower;

	/// <summary>
	/// Number of candles used to build extreme levels.
	/// </summary>
	public int LevelLength
	{
		get => _levelLength.Value;
		set => _levelLength.Value = value;
	}

	/// <summary>
	/// Shift in candles used for the breakout signal.
	/// </summary>
	public int SignalShift
	{
		get => _signalShift.Value;
		set => _signalShift.Value = value;
	}

	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Reverse long and short signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="PriceExtremeStrategy"/>.
	/// </summary>
	public PriceExtremeStrategy()
	{
		_levelLength = Param(nameof(LevelLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Level Length", "Number of candles for price extremes", "Indicator")
			
			.SetOptimize(3, 30, 1);

		_signalShift = Param(nameof(SignalShift), 1)
			.SetGreaterThanZero()
			.SetDisplay("Signal Shift", "Closed candles used for breakout", "Indicator");

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow buying trades", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow selling trades", "Trading");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert breakout direction", "Trading");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume sent with market orders", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 0)
			.SetDisplay("Stop Loss", "Protective stop in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0)
			.SetDisplay("Take Profit", "Profit target in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_history.Clear();
		_highs.Clear();
		_lows.Clear();
		_prevUpper = 0m;
		_prevLower = 0m;
		_entryPrice = 0m;
		_prevPosition = 0m;
		ResetTargets();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private bool CanOpenLong => EnableLong && OrderVolume > 0m;
	private bool CanOpenShort => EnableShort && OrderVolume > 0m;

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);
		_history.Add(candle);

		var maxHistory = Math.Max(LevelLength + SignalShift + 2, 10);
		if (_history.Count > maxHistory)
		{
			var removeCount = _history.Count - maxHistory;
			_history.RemoveRange(0, removeCount);
			_highs.RemoveRange(0, removeCount);
			_lows.RemoveRange(0, removeCount);
		}

		if (_highs.Count < LevelLength)
			return;

		var upper = decimal.MinValue;
		var lower = decimal.MaxValue;
		for (var i = _highs.Count - LevelLength; i < _highs.Count; i++)
		{
			if (_highs[i] > upper) upper = _highs[i];
			if (_lows[i] < lower) lower = _lows[i];
		}

		if (_history.Count < SignalShift)
			return;

		var signalCandle = _history[_history.Count - SignalShift];

		var breakoutUp = candle.ClosePrice > _prevUpper && _prevUpper > 0;
		var breakoutDown = candle.ClosePrice < _prevLower && _prevLower > 0;

		_prevUpper = upper;
		_prevLower = lower;

		var wantLong = ReverseSignals ? breakoutDown : breakoutUp;
		var wantShort = ReverseSignals ? breakoutUp : breakoutDown;

		if (wantLong && CanOpenLong && Position <= 0)
		{
			if (Position < 0)
			{
				if (Position > 0) SellMarket(Position); else if (Position < 0) BuyMarket(-Position);
				ResetTargets();
			}

			BuyMarket(OrderVolume);
		}
		else if (wantShort && CanOpenShort && Position >= 0)
		{
			if (Position > 0)
			{
				if (Position > 0) SellMarket(Position); else if (Position < 0) BuyMarket(-Position);
				ResetTargets();
			}

			SellMarket(OrderVolume);
		}

		if (Position != _prevPosition)
		{
			UpdateTargets();
			_prevPosition = Position;
		}

		ApplyRiskManagement(candle);
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		if (trade?.Trade == null) return;
		if (Position != 0 && _entryPrice == 0m)
			_entryPrice = trade.Trade.Price;
		if (Position == 0)
			_entryPrice = 0m;
	}

	private void UpdateTargets()
	{
		_stopPrice = null;
		_takePrice = null;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m || Position == 0m)
			return;

		if (Position > 0m)
		{
			if (StopLossPoints > 0)
				_stopPrice = _entryPrice - StopLossPoints * step;

			if (TakeProfitPoints > 0)
				_takePrice = _entryPrice + TakeProfitPoints * step;
		}
		else if (Position < 0m)
		{
			if (StopLossPoints > 0)
				_stopPrice = _entryPrice + StopLossPoints * step;

			if (TakeProfitPoints > 0)
				_takePrice = _entryPrice - TakeProfitPoints * step;
		}
	}

	private void ApplyRiskManagement(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				if (Position > 0) SellMarket(Position); else if (Position < 0) BuyMarket(-Position);
				ResetTargets();
				return;
			}

			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				if (Position > 0) SellMarket(Position); else if (Position < 0) BuyMarket(-Position);
				ResetTargets();
			}
		}
		else if (Position < 0m)
		{
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				if (Position > 0) SellMarket(Position); else if (Position < 0) BuyMarket(-Position);
				ResetTargets();
				return;
			}

			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				if (Position > 0) SellMarket(Position); else if (Position < 0) BuyMarket(-Position);
				ResetTargets();
			}
		}
	}

	private void ResetTargets()
	{
		_stopPrice = null;
		_takePrice = null;
		_prevPosition = Position;
	}
}