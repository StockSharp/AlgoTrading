using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trade management strategy with break-even and trailing stop logic.
/// </summary>
public class LacustStopAndBeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStart;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _breakevenGain;
	private readonly StrategyParam<decimal> _breakeven;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;


	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initial stop loss distance.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initial take profit distance.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Profit required to activate trailing stop.
	/// </summary>
	public decimal TrailingStart
	{
		get => _trailingStart.Value;
		set => _trailingStart.Value = value;
	}

	/// <summary>
	/// Trailing stop distance from current price.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Profit required before moving stop to break-even.
	/// </summary>
	public decimal BreakevenGain
	{
		get => _breakevenGain.Value;
		set => _breakevenGain.Value = value;
	}

	/// <summary>
	/// Profit locked after moving stop to break-even.
	/// </summary>
	public decimal Breakeven
	{
		get => _breakeven.Value;
		set => _breakeven.Value = value;
	}

	public LacustStopAndBeStrategy()
	{
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle type");
		_stopLoss = Param(nameof(StopLoss), 40m)
			.SetDisplay("Stop loss").SetCanOptimize(true);
		_takeProfit = Param(nameof(TakeProfit), 200m)
			.SetDisplay("Take profit").SetCanOptimize(true);
		_trailingStart = Param(nameof(TrailingStart), 30m)
			.SetDisplay("Trailing start").SetCanOptimize(true);
		_trailingStop = Param(nameof(TrailingStop), 20m)
			.SetDisplay("Trailing stop").SetCanOptimize(true);
		_breakevenGain = Param(nameof(BreakevenGain), 25m)
			.SetDisplay("Breakeven gain").SetCanOptimize(true);
		_breakeven = Param(nameof(Breakeven), 10m)
			.SetDisplay("Breakeven").SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = default;
		_stopPrice = default;
		_takePrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
		{
			if (candle.ClosePrice > candle.OpenPrice)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLoss;
				_takePrice = _entryPrice + TakeProfit;
			}
			else
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLoss;
				_takePrice = _entryPrice - TakeProfit;
			}
			return;
		}

		if (Position > 0)
		{
			if (candle.ClosePrice - _entryPrice >= BreakevenGain && _stopPrice < _entryPrice + Breakeven)
				_stopPrice = _entryPrice + Breakeven;

			if (candle.ClosePrice - _entryPrice >= TrailingStart && _stopPrice < candle.ClosePrice - TrailingStop)
				_stopPrice = candle.ClosePrice - TrailingStop;

			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				ClosePosition();
		}
		else
		{
			if (_entryPrice - candle.ClosePrice >= BreakevenGain && _stopPrice > _entryPrice - Breakeven)
				_stopPrice = _entryPrice - Breakeven;

			if (_entryPrice - candle.ClosePrice >= TrailingStart && _stopPrice > candle.ClosePrice + TrailingStop)
				_stopPrice = candle.ClosePrice + TrailingStop;

			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				ClosePosition();
		}
	}
}
