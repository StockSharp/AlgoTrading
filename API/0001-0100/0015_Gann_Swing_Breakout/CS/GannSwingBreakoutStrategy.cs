using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Gann Swing Breakout technique.
/// Uses Donchian channel breakouts with SMA trend filter.
/// Enters long when price breaks above channel high and is above SMA.
/// Enters short when price breaks below channel low and is below SMA.
/// </summary>
public class GannSwingBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _swingLookback;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevChannelHigh;
	private decimal _prevChannelLow;
	private bool _hasPrevValues;
	private int _candlesSinceLastTrade;

	/// <summary>
	/// Number of bars to identify swing points.
	/// </summary>
	public int SwingLookback
	{
		get => _swingLookback.Value;
		set => _swingLookback.Value = value;
	}

	/// <summary>
	/// Period for moving average calculation.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the Gann Swing Breakout strategy.
	/// </summary>
	public GannSwingBreakoutStrategy()
	{
		_swingLookback = Param(nameof(SwingLookback), 40)
			.SetDisplay("Swing Lookback", "Lookback period for swing high/low", "Trading parameters")
			.SetOptimize(20, 60, 10);

		_maPeriod = Param(nameof(MaPeriod), 60)
			.SetDisplay("MA Period", "Period for trend filter MA", "Indicators")
			.SetOptimize(40, 80, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevChannelHigh = default;
		_prevChannelLow = default;
		_hasPrevValues = default;
		_candlesSinceLastTrade = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var donchian = new DonchianChannels { Length = SwingLookback };
		var ma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (maValue is not { IsEmpty: false })
			return;

		var ma = maValue.GetValue<decimal>();
		if (ma == 0)
			return;

		// Extract Donchian channel values
		var dcValue = (IDonchianChannelsValue)donchianValue;
		if (dcValue.UpperBand is not decimal channelHigh ||
			dcValue.LowerBand is not decimal channelLow)
			return;

		if (channelHigh == 0 || channelLow == 0)
			return;

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevChannelHigh = channelHigh;
			_prevChannelLow = channelLow;
			return;
		}

		_candlesSinceLastTrade++;

		// Breakout above previous channel high + above MA = buy
		if (_candlesSinceLastTrade >= 10 && candle.ClosePrice > _prevChannelHigh && candle.ClosePrice > ma && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_candlesSinceLastTrade = 0;
		}
		// Breakout below previous channel low + below MA = sell
		else if (_candlesSinceLastTrade >= 10 && candle.ClosePrice < _prevChannelLow && candle.ClosePrice < ma && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_candlesSinceLastTrade = 0;
		}

		_prevChannelHigh = channelHigh;
		_prevChannelLow = channelLow;
	}
}
