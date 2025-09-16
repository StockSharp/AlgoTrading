using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using channel based trailing stops with optional "noose" adjustment.
/// </summary>
public class ChannelTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _trailPeriod;
	private readonly StrategyParam<decimal> _trailStop;
	private readonly StrategyParam<bool> _useNooseTrailing;
	private readonly StrategyParam<bool> _useChannelTrailing;
	private readonly StrategyParam<bool> _deletePendingOrders;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _longStop;
	private decimal _shortStop;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Period to calculate channel boundaries.
	/// </summary>
	public int TrailPeriod
	{
		get => _trailPeriod.Value;
		set => _trailPeriod.Value = value;
	}

	/// <summary>
	/// Offset added to channel boundaries.
	/// </summary>
	public decimal TrailStop
	{
		get => _trailStop.Value;
		set => _trailStop.Value = value;
	}

	/// <summary>
	/// Enable symmetrical "noose" trailing.
	/// </summary>
	public bool UseNooseTrailing
	{
		get => _useNooseTrailing.Value;
		set => _useNooseTrailing.Value = value;
	}

	/// <summary>
	/// Enable trailing stop based on channel levels.
	/// </summary>
	public bool UseChannelTrailing
	{
		get => _useChannelTrailing.Value;
		set => _useChannelTrailing.Value = value;
	}

	/// <summary>
	/// Delete pending orders after trade execution.
	/// </summary>
	public bool DeletePendingOrders
	{
		get => _deletePendingOrders.Value;
		set => _deletePendingOrders.Value = value;
	}

	/// <summary>
	/// Type of candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ChannelTrailingStopStrategy"/> class.
	/// </summary>
	public ChannelTrailingStopStrategy()
	{
		_trailPeriod = Param(nameof(TrailPeriod), 5)
			.SetDisplay("Channel Period", "Lookback for channel calculation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_trailStop = Param(nameof(TrailStop), 50m)
			.SetDisplay("Trail Stop", "Offset from channel boundaries", "Parameters");

		_useNooseTrailing = Param(nameof(UseNooseTrailing), true)
			.SetDisplay("Use Noose Trailing", "Mirror stop relative to take profit", "Parameters");

		_useChannelTrailing = Param(nameof(UseChannelTrailing), true)
			.SetDisplay("Use Channel Trailing", "Adjust stop to channel levels", "Parameters");

		_deletePendingOrders = Param(nameof(DeletePendingOrders), true)
			.SetDisplay("Delete Pending Orders", "Cancel pending orders after fill", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
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
		_longStop = 0;
		_shortStop = 0;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (DeletePendingOrders)
			CancelActiveOrders();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var donchian = new DonchianChannels { Length = TrailPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var dc = (DonchianChannelsValue)value;

		if (dc.UpperBand is not decimal upper || dc.LowerBand is not decimal lower)
			return;

		// Entry logic: breakouts of channel boundaries
		if (candle.ClosePrice > upper && Position <= 0)
		{
			var vol = Volume + Math.Abs(Position);
			BuyMarket(vol);
			_longStop = candle.ClosePrice - TrailStop;
			_takeProfitPrice = candle.ClosePrice + TrailStop;
		}
		else if (candle.ClosePrice < lower && Position >= 0)
		{
			var vol = Volume + Math.Abs(Position);
			SellMarket(vol);
			_shortStop = candle.ClosePrice + TrailStop;
			_takeProfitPrice = candle.ClosePrice - TrailStop;
		}

		// Update trailing stops based on channel
		if (UseChannelTrailing)
		{
			if (Position > 0)
			{
				var level = lower - TrailStop;
				if (level > _longStop)
					_longStop = level;
			}
			else if (Position < 0)
			{
				var level = upper + TrailStop;
				if (_shortStop == 0 || level < _shortStop)
					_shortStop = level;
			}
		}

		// Noose trailing adjusts stop symmetrically to take profit
		if (UseNooseTrailing && _takeProfitPrice != null)
		{
			if (Position > 0)
			{
				var noose = candle.ClosePrice - (_takeProfitPrice.Value - candle.ClosePrice);
				if (noose > _longStop)
					_longStop = noose;
			}
			else if (Position < 0)
			{
				var noose = candle.ClosePrice + (candle.ClosePrice - _takeProfitPrice.Value);
				if (_shortStop == 0 || noose < _shortStop)
					_shortStop = noose;
			}
		}

		// Exit conditions when price crosses trailing levels
		if (Position > 0 && candle.LowPrice <= _longStop)
		{
			SellMarket(Position);
			_longStop = 0;
			_takeProfitPrice = null;
		}
		else if (Position < 0 && candle.HighPrice >= _shortStop)
		{
			BuyMarket(Math.Abs(Position));
			_shortStop = 0;
			_takeProfitPrice = null;
		}
	}
}
