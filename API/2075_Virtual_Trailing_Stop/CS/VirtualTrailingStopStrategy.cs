
using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that applies a virtual trailing stop to existing positions.
/// It can manage both long and short positions, updating stop levels as price moves.
/// The strategy does not generate entry signals; positions must be opened externally.
/// </summary>
public class VirtualTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<int> _trailingStart;
	private readonly StrategyParam<int> _trailingStep;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _trailingBuy;
	private decimal _trailingSell;

	/// <summary>
	/// Hard stop-loss distance in price steps.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Hard take-profit distance in price steps.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Distance from current price to trailing stop in price steps.
	/// </summary>
	public int TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Profit in price steps required before trailing starts.
	/// </summary>
	public int TrailingStart
	{
		get => _trailingStart.Value;
		set => _trailingStart.Value = value;
	}

	/// <summary>
	/// Minimum additional profit in price steps required to move the trailing stop.
	/// </summary>
	public int TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Type of candles used for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the virtual trailing stop strategy.
	/// </summary>
	public VirtualTrailingStopStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 0)
			.SetDisplay("Stoploss", "Stop-loss distance in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0, 20, 1);

		_takeProfit = Param(nameof(TakeProfit), 0)
			.SetDisplay("Takeprofit", "Take-profit distance in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0, 20, 1);

		_trailingStop = Param(nameof(TrailingStop), 5)
			.SetDisplay("Trailing Stop", "Trailing stop distance in price steps", "Trailing")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_trailingStart = Param(nameof(TrailingStart), 5)
			.SetDisplay("Trailing Start", "Start trailing after price moves this many steps", "Trailing")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_trailingStep = Param(nameof(TrailingStep), 1)
			.SetDisplay("Trailing Step", "Minimal step to update trailing level", "Trailing")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "Data");
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
		_trailingBuy = 0m;
		_trailingSell = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = Security.PriceStep ?? 1m;

		if (Position > 0)
		{
			var entry = PositionPrice ?? candle.ClosePrice;

			if (StopLoss > 0 && candle.ClosePrice <= entry - StopLoss * step)
			{
				SellMarket(Position);
				_trailingBuy = 0m;
				return;
			}

			if (TakeProfit > 0 && candle.ClosePrice >= entry + TakeProfit * step)
			{
				SellMarket(Position);
				_trailingBuy = 0m;
				return;
			}

			if (TrailingStop > 0)
			{
				var sl = candle.ClosePrice - TrailingStop * step;

				if (sl >= entry + TrailingStart * step && (_trailingBuy == 0m || _trailingBuy + TrailingStep * step < sl))
					_trailingBuy = sl;

				if (_trailingBuy != 0m && candle.ClosePrice <= _trailingBuy)
				{
					SellMarket(Position);
					_trailingBuy = 0m;
				}
			}
		}
		else if (Position < 0)
		{
			var entry = PositionPrice ?? candle.ClosePrice;
			var volume = Math.Abs(Position);

			if (StopLoss > 0 && candle.ClosePrice >= entry + StopLoss * step)
			{
				BuyMarket(volume);
				_trailingSell = 0m;
				return;
			}

			if (TakeProfit > 0 && candle.ClosePrice <= entry - TakeProfit * step)
			{
				BuyMarket(volume);
				_trailingSell = 0m;
				return;
			}

			if (TrailingStop > 0)
			{
				var sl = candle.ClosePrice + TrailingStop * step;

				if (sl <= entry - TrailingStart * step && (_trailingSell == 0m || _trailingSell - TrailingStep * step > sl))
					_trailingSell = sl;

				if (_trailingSell != 0m && candle.ClosePrice >= _trailingSell)
				{
					BuyMarket(volume);
					_trailingSell = 0m;
				}
			}
		}
		else
		{
			_trailingBuy = 0m;
			_trailingSell = 0m;
		}
	}
}
