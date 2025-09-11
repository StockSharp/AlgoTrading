using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Template strategy that reacts to external signals.
/// Supports long and short entries and applies percent based stop-loss and take-profit.
/// </summary>
public class StrategyConnectableStrategy : Strategy
{
	private readonly StrategyParam<string> _signalMode;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _lastPrice;
	private bool _isLong;

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public string SignalMode
	{
		get => _signalMode.Value;
		set => _signalMode.Value = value;
	}

	/// <summary>
	/// Stop loss percent from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percent from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public StrategyConnectableStrategy()
	{
		_signalMode = Param(nameof(SignalMode), "Long")
			.SetDisplay("Signal Mode", "Allowed trade direction", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Percent of entry price for stop loss", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Percent of entry price for take profit", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(2m, 10m, 2m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_entryPrice = 0m;
		_lastPrice = 0m;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	/// <summary>
	/// Process external trading signal.
	/// </summary>
	/// <param name="isLongEntry">Long entry signal.</param>
	/// <param name="isShortEntry">Short entry signal.</param>
	/// <param name="exit">Exit signal.</param>
	public void ProcessSignal(bool isLongEntry, bool isShortEntry, bool exit)
	{
		if (exit && Position != 0)
		{
			ClosePosition();
			return;
		}

		if (_lastPrice == 0m)
			return;

		if (isLongEntry && Position <= 0 && (SignalMode == "Long" || SignalMode == "Both" || SignalMode == "Swing"))
		{
			_entryPrice = _lastPrice;
			_isLong = true;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (isShortEntry && Position >= 0 && (SignalMode == "Short" || SignalMode == "Both" || SignalMode == "Swing"))
		{
			_entryPrice = _lastPrice;
			_isLong = false;
			SellMarket(Volume + Math.Abs(Position));
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);

		_entryPrice = 0m;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastPrice = candle.ClosePrice;

		if (Position == 0 || _entryPrice == 0m)
			return;

		var sl = _stopLossPercent.Value / 100m;
		var tp = _takeProfitPercent.Value / 100m;

		if (_isLong)
		{
			var stop = _entryPrice * (1m - sl);
			var take = _entryPrice * (1m + tp);
			if (candle.LowPrice <= stop || candle.HighPrice >= take)
				ClosePosition();
		}
		else
		{
			var stop = _entryPrice * (1m + sl);
			var take = _entryPrice * (1m - tp);
			if (candle.HighPrice >= stop || candle.LowPrice <= take)
				ClosePosition();
		}
	}
}
