using System;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manual trainer strategy with entry, stop, take profit and optional partial close levels.
/// </summary>
public class MTrainerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _entryPrice;
	private readonly StrategyParam<decimal> _takeProfitPrice;
	private readonly StrategyParam<decimal> _stopLossPrice;
	private readonly StrategyParam<decimal> _partialClosePrice;
	private readonly StrategyParam<decimal> _partialClosePercent;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<TimeSpan> _timeFrame;

	private bool _partialClosed;

	/// <summary>
	/// Price line where the position is opened.
	/// </summary>
	public decimal EntryPrice
	{
		get => _entryPrice.Value;
		set => _entryPrice.Value = value;
	}

	/// <summary>
	/// Price line for full take profit.
	/// </summary>
	public decimal TakeProfitPrice
	{
		get => _takeProfitPrice.Value;
		set => _takeProfitPrice.Value = value;
	}

	/// <summary>
	/// Price line for stop loss.
	/// </summary>
	public decimal StopLossPrice
	{
		get => _stopLossPrice.Value;
		set => _stopLossPrice.Value = value;
	}

	/// <summary>
	/// Price line for partial close.
	/// </summary>
	public decimal PartialClosePrice
	{
		get => _partialClosePrice.Value;
		set => _partialClosePrice.Value = value;
	}

	/// <summary>
	/// Percentage of position to close on partial close.
	/// </summary>
	public decimal PartialClosePercent
	{
		get => _partialClosePercent.Value;
		set => _partialClosePercent.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle time frame used for price monitoring.
	/// </summary>
	public TimeSpan TimeFrame
	{
		get => _timeFrame.Value;
		set => _timeFrame.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="MTrainerStrategy"/>.
	/// </summary>
	public MTrainerStrategy()
	{
		_entryPrice = Param(nameof(EntryPrice), 0m)
			.SetDisplay("Entry Price", "Price line where position is opened", "Trading");

		_takeProfitPrice = Param(nameof(TakeProfitPrice), 0m)
			.SetDisplay("Take Profit", "Price line for full profit taking", "Trading");

		_stopLossPrice = Param(nameof(StopLossPrice), 0m)
			.SetDisplay("Stop Loss", "Price line for full stop loss", "Trading");

		_partialClosePrice = Param(nameof(PartialClosePrice), 0m)
			.SetDisplay("Partial Close", "Price line for partial position close", "Trading");

		_partialClosePercent = Param(nameof(PartialClosePercent), 0m)
			.SetDisplay("Partial Close %", "Percentage of position to close", "Trading");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_timeFrame = Param(nameof(TimeFrame), TimeSpan.FromMinutes(1))
			.SetDisplay("Time Frame", "Candle time frame", "Data");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(TimeFrame);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position == 0)
		{
			if (EntryPrice <= 0 || TakeProfitPrice <= 0 || StopLossPrice <= 0)
				return;

			if (TakeProfitPrice > StopLossPrice)
			{
				if (candle.HighPrice >= EntryPrice)
				{
					BuyMarket(Volume);
					_partialClosed = false;
				}
			}
			else
			{
				if (candle.LowPrice <= EntryPrice)
				{
					SellMarket(Volume);
					_partialClosed = false;
				}
			}
		}
		else if (Position > 0)
		{
			if (!_partialClosed && PartialClosePercent > 0 && PartialClosePrice > 0 && candle.HighPrice >= PartialClosePrice)
			{
				var part = Position * PartialClosePercent / 100m;
				SellMarket(part);
				_partialClosed = true;
			}

			if (candle.LowPrice <= StopLossPrice || candle.HighPrice >= TakeProfitPrice)
				SellMarket(Position);
		}
		else
		{
			if (!_partialClosed && PartialClosePercent > 0 && PartialClosePrice > 0 && candle.LowPrice <= PartialClosePrice)
			{
				var part = -Position * PartialClosePercent / 100m;
				BuyMarket(part);
				_partialClosed = true;
			}

			if (candle.HighPrice >= StopLossPrice || candle.LowPrice <= TakeProfitPrice)
				BuyMarket(-Position);
		}
	}
}
