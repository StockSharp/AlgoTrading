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
/// Breakout strategy that places stop orders at the extremes of the intraday channel.
/// </summary>
public class ChannelEa2Strategy : Strategy
{
	private readonly StrategyParam<int> _beginHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopBufferMultiplier;

	private decimal? _sessionHigh;
	private decimal? _sessionLow;
	private bool _channelReady;
	private decimal? _entryPrice;
	private decimal? _stopLossPrice;

	/// <summary>
	/// Trading session start hour.
	/// </summary>
	public int BeginHour
	{
		get => _beginHour.Value;
		set => _beginHour.Value = value;
	}

	/// <summary>
	/// Trading session end hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for channel detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of price steps added as a buffer to entry and protective orders.
	/// </summary>
	public decimal StopBufferMultiplier
	{
		get => _stopBufferMultiplier.Value;
		set => _stopBufferMultiplier.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ChannelEa2Strategy"/> class.
	/// </summary>
	public ChannelEa2Strategy()
	{
		_beginHour = Param(nameof(BeginHour), 1)
			.SetDisplay("Begin Hour", "Hour when the session resets", "Trading")
			
			.SetOptimize(0, 23, 1);

		_endHour = Param(nameof(EndHour), 10)
			.SetDisplay("End Hour", "Hour when breakout orders are scheduled", "Trading")
			
			.SetOptimize(0, 23, 1);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Volume", "Order volume", "Trading")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for the channel", "General");

		_stopBufferMultiplier = Param(nameof(StopBufferMultiplier), 2m)
			.SetDisplay("Stop Buffer", "Price step multiplier for safety offsets", "Risk")
			.SetNotNegative();
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

		_sessionHigh = null;
		_sessionLow = null;
		_channelReady = false;
		_entryPrice = null;
		_stopLossPrice = null;
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

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.OpenTime.Hour;

		// During channel-building hours, accumulate the range.
		if (hour >= BeginHour && hour < EndHour)
		{
			if (_sessionHigh is null || candle.HighPrice > _sessionHigh)
				_sessionHigh = candle.HighPrice;
			if (_sessionLow is null || candle.LowPrice < _sessionLow)
				_sessionLow = candle.LowPrice;
			_channelReady = true;
			return;
		}

		// Outside the channel window, attempt breakout entries.
		if (!_channelReady || _sessionHigh is not decimal high || _sessionLow is not decimal low || high <= low)
			return;

		var buffer = GetPriceBuffer();

		// Manage existing positions.
		if (Position > 0)
		{
			if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
			{
				SellMarket(Position);
				_entryPrice = null;
				_stopLossPrice = null;
			}
			return;
		}
		else if (Position < 0)
		{
			if (_stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = null;
				_stopLossPrice = null;
			}
			return;
		}

		// Long breakout: price exceeds channel high.
		if (candle.HighPrice > high + buffer)
		{
			BuyMarket(TradeVolume > 0 ? TradeVolume : Volume);
			_entryPrice = candle.ClosePrice;
			_stopLossPrice = low - buffer;
			// Reset channel for next session.
			_sessionHigh = null;
			_sessionLow = null;
			_channelReady = false;
			return;
		}

		// Short breakout: price drops below channel low.
		if (candle.LowPrice < low - buffer)
		{
			SellMarket(TradeVolume > 0 ? TradeVolume : Volume);
			_entryPrice = candle.ClosePrice;
			_stopLossPrice = high + buffer;
			_sessionHigh = null;
			_sessionLow = null;
			_channelReady = false;
		}
	}

	private decimal GetPriceBuffer()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m || StopBufferMultiplier <= 0m)
			return 0m;

		return step * StopBufferMultiplier;
	}
}