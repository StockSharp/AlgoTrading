namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy managing virtual stop loss, take profit, trailing stop and breakeven levels.
/// Converted from MetaTrader script "VR---STEALS-3-EN".
/// </summary>
public class VirtualStopManagerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<int> _breakeven;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Virtual take profit in points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Virtual stop loss in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public int TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Profit in points to move stop to breakeven.
	/// </summary>
	public int Breakeven
	{
		get => _breakeven.Value;
		set => _breakeven.Value = value;
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;
	private decimal _bestPrice;
	private bool _breakevenMoved;

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public VirtualStopManagerStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");

		_takeProfit = Param(nameof(TakeProfit), 500)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (points)", "Virtual take profit in points", "Risk");

		_stopLoss = Param(nameof(StopLoss), 500)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (points)", "Virtual stop loss in points", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 300)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop (points)", "Trailing stop distance in points", "Risk");

		_breakeven = Param(nameof(Breakeven), 300)
			.SetGreaterThanZero()
			.SetDisplay("Breakeven (points)", "Profit in points to move stop to breakeven", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to track", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = Security.PriceStep ?? 1m;
		var close = candle.ClosePrice;

		// Open long position if none exists.
		if (Position == 0)
		{
			BuyMarket(Volume);
			return;
		}

		if (_entryPrice == 0m)
			return; // Wait for trade price information.

		// Move stop to breakeven when profit exceeds threshold.
		if (!_breakevenMoved && close - _entryPrice >= Breakeven * step)
		{
			_stopPrice = _entryPrice;
			_breakevenMoved = true;
		}

		// Update trailing stop when new high reached.
		if (close > _bestPrice)
		{
			_bestPrice = close;

			if (close - TrailingStop * step > _stopPrice)
				_stopPrice = close - TrailingStop * step;
		}

		// Exit on stop loss or take profit levels.
		if (close <= _stopPrice || close >= _targetPrice)
		{
			SellMarket(Position);
			ResetState();
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position == 0 || _entryPrice != 0m)
			return;

		_entryPrice = trade.Trade.Price;
		var step = Security.PriceStep ?? 1m;
		_targetPrice = _entryPrice + TakeProfit * step;
		_stopPrice = _entryPrice - StopLoss * step;
		_bestPrice = _entryPrice;
		_breakevenMoved = false;
	}

	private void ResetState()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_targetPrice = 0m;
		_bestPrice = 0m;
		_breakevenMoved = false;
	}
}

