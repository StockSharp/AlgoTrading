using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Linear regression channel strategy.
/// Uses LinearReg as the center line with Highest/Lowest to form a channel.
/// Sells at upper channel, buys at lower channel, with trend filter from regression slope.
/// </summary>
public class MultiTimeFrameRegressionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _regressionLength;
	private readonly StrategyParam<int> _channelLength;

	private decimal _prevLrValue;
	private bool _hasPrev;

	public MultiTimeFrameRegressionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis.", "General");

		_regressionLength = Param(nameof(RegressionLength), 20)
			.SetDisplay("Regression Length", "Period for linear regression.", "Indicators");

		_channelLength = Param(nameof(ChannelLength), 20)
			.SetDisplay("Channel Length", "Period for highest/lowest channel.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RegressionLength
	{
		get => _regressionLength.Value;
		set => _regressionLength.Value = value;
	}

	public int ChannelLength
	{
		get => _channelLength.Value;
		set => _channelLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevLrValue = 0;
		_hasPrev = false;

		var lr = new LinearReg { Length = RegressionLength };
		var highest = new Highest { Length = ChannelLength };
		var lowest = new Lowest { Length = ChannelLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(lr, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, lr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal lrValue, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Determine slope direction from regression
		var slope = _hasPrev ? lrValue - _prevLrValue : 0m;

		// Channel boundaries
		var channelMid = (highestValue + lowestValue) / 2m;
		var channelWidth = highestValue - lowestValue;

		if (channelWidth <= 0)
		{
			_prevLrValue = lrValue;
			_hasPrev = true;
			return;
		}

		// Upper/lower thresholds
		var upperThreshold = channelMid + channelWidth * 0.4m;
		var lowerThreshold = channelMid - channelWidth * 0.4m;

		// Exit conditions
		if (Position > 0 && (close >= upperThreshold || slope < 0))
		{
			SellMarket();
		}
		else if (Position < 0 && (close <= lowerThreshold || slope > 0))
		{
			BuyMarket();
		}

		// Entry conditions
		if (Position == 0)
		{
			if (close <= lowerThreshold && slope >= 0)
			{
				// Price near lower channel with flat/rising regression
				BuyMarket();
			}
			else if (close >= upperThreshold && slope <= 0)
			{
				// Price near upper channel with flat/falling regression
				SellMarket();
			}
		}

		_prevLrValue = lrValue;
		_hasPrev = true;
	}
}
