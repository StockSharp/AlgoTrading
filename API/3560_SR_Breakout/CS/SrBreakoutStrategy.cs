using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Support and resistance breakout strategy using Donchian channels.
/// Buys when price breaks above resistance (upper band), sells when breaks below support (lower band).
/// </summary>
public class SrBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackLength;
	private readonly StrategyParam<DataType> _candleType;

	private DonchianChannels _donchian;
	private decimal? _prevUpper;
	private decimal? _prevLower;

	public int LookbackLength
	{
		get => _lookbackLength.Value;
		set => _lookbackLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SrBreakoutStrategy()
	{
		_lookbackLength = Param(nameof(LookbackLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Number of candles for Donchian channel", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevUpper = null;
		_prevLower = null;

		_donchian = new DonchianChannels { Length = LookbackLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_donchian.IsFormed)
			return;

		if (donchianValue is not IDonchianChannelsValue dcv)
			return;

		var upper = dcv.UpperBand;
		var lower = dcv.LowerBand;
		var close = candle.ClosePrice;

		if (_prevUpper is null || _prevLower is null)
		{
			_prevUpper = upper;
			_prevLower = lower;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Break above resistance
		if (close > _prevUpper.Value)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		// Break below support
		else if (close < _prevLower.Value)
		{
			if (Position > 0)
				SellMarket(Position);

			if (Position >= 0)
				SellMarket(volume);
		}

		_prevUpper = upper;
		_prevLower = lower;
	}
}
