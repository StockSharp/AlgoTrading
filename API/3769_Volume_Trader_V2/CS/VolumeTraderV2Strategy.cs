using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume Trader V2 strategy converted from the MetaTrader expert Volume_trader_v2_www_forex-instruments_info.mq4.
/// Follows the original logic by comparing the volume of the last two finished candles and trading only during configured hours.
/// </summary>
public class VolumeTraderV2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _tradeVolume;

	private decimal? _previousVolume;
	private decimal? _twoBarsAgoVolume;

	/// <summary>
	/// Initializes a new instance of the <see cref="VolumeTraderV2Strategy"/> class.
	/// </summary>
	public VolumeTraderV2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used to request candles", "Data");

		_startHour = Param(nameof(StartHour), 8)
		.SetDisplay("Start Hour", "First hour (inclusive) when trading is allowed", "Trading")
		.SetRange(0, 23);

		_endHour = Param(nameof(EndHour), 20)
		.SetDisplay("End Hour", "Last hour (inclusive) when trading is allowed", "Trading")
		.SetRange(0, 23);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetDisplay("Trade Volume", "Order volume replicated from the original EA", "Trading")
		.SetGreaterThanZero();

		Volume = TradeVolume;
	}

	/// <summary>
	/// Candle type used for the strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// First trading hour (inclusive).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Last trading hour (inclusive).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Default order volume for market operations.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
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

		// Drop cached volume values so the warm-up sequence matches the EA behavior after a reset.
		_previousVolume = null;
		_twoBarsAgoVolume = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to candles and process them with the same granularity as the original indicator buffers.
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Only act on finished candles to replicate the bar-by-bar logic.
		if (candle.State != CandleStates.Finished)
		return;

		var currentVolume = candle.TotalVolume;

		// Collect the first two candles before generating signals.
		if (_previousVolume is null)
		{
			_previousVolume = currentVolume;
			return;
		}

		if (_twoBarsAgoVolume is null)
		{
			_twoBarsAgoVolume = _previousVolume;
			_previousVolume = currentVolume;
			return;
		}

		var volume1 = _previousVolume.Value;
		var volume2 = _twoBarsAgoVolume.Value;

		var hour = candle.OpenTime.Hour;
		var hourValid = hour >= StartHour && hour <= EndHour;

		var shouldGoLong = hourValid && volume1 < volume2;
		var shouldGoShort = hourValid && volume1 > volume2;

		Comment = !hourValid
			? "Trading paused"
			: shouldGoLong
			? "Up trend"
			: shouldGoShort
			? "Down trend"
			: "No trend...";

		if (!shouldGoLong && !shouldGoShort)
		{
			// Exit the market when no direction is active (equal volume or outside trading hours).
			ClosePosition();
		}
		else if (shouldGoLong)
		{
			// Flatten any short position before opening a new long trade.
			if (Position < 0)
			BuyMarket(-Position);

			if (Position <= 0)
			BuyMarket(TradeVolume);
		}
		else if (shouldGoShort)
		{
			// Flatten any long position before opening a new short trade.
			if (Position > 0)
			SellMarket(Position);

			if (Position >= 0)
			SellMarket(TradeVolume);
		}

		// Shift the cached volumes to emulate Volume[1] and Volume[2] from MetaTrader.
		_twoBarsAgoVolume = _previousVolume;
		_previousVolume = currentVolume;
	}

	private void ClosePosition()
	{
		// Mirror the EA behavior by leaving the market whenever the signal is neutral.
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
		}
	}
}
