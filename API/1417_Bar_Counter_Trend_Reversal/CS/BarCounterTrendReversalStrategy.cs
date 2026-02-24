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
/// Bar Counter Trend Reversal strategy.
/// Detects consecutive rises or falls with SMA/StdDev channel confirmation.
/// </summary>
public class BarCounterTrendReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _noOfRises;
	private readonly StrategyParam<int> _noOfFalls;
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<decimal> _channelWidth;
	private readonly StrategyParam<DataType> _candleType;

	private int _riseCount;
	private int _fallCount;
	private decimal _prevClose;

	/// <summary>Number of rising closes to trigger short setup.</summary>
	public int NoOfRises { get => _noOfRises.Value; set => _noOfRises.Value = value; }
	/// <summary>Number of falling closes to trigger long setup.</summary>
	public int NoOfFalls { get => _noOfFalls.Value; set => _noOfFalls.Value = value; }
	/// <summary>Channel length.</summary>
	public int ChannelLength { get => _channelLength.Value; set => _channelLength.Value = value; }
	/// <summary>Channel width multiplier.</summary>
	public decimal ChannelWidth { get => _channelWidth.Value; set => _channelWidth.Value = value; }
	/// <summary>Candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BarCounterTrendReversalStrategy()
	{
		_noOfRises = Param(nameof(NoOfRises), 3)
			.SetDisplay("No. of Rises", "Consecutive rising bars", "Parameters")
			.SetGreaterThanZero();
		_noOfFalls = Param(nameof(NoOfFalls), 3)
			.SetDisplay("No. of Falls", "Consecutive falling bars", "Parameters")
			.SetGreaterThanZero();
		_channelLength = Param(nameof(ChannelLength), 20)
			.SetDisplay("Channel Length", "SMA / StdDev length", "Parameters")
			.SetGreaterThanZero();
		_channelWidth = Param(nameof(ChannelWidth), 2m)
			.SetDisplay("Channel Width", "StdDev multiplier", "Parameters")
			.SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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
		_riseCount = _fallCount = 0;
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = ChannelLength };
		var stdDev = new StandardDeviation { Length = ChannelLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, stdDev, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stdVal <= 0)
			return;

		var upper = smaVal + stdVal * ChannelWidth;
		var lower = smaVal - stdVal * ChannelWidth;

		var prevClose = _prevClose;

		if (prevClose != 0m)
		{
			if (candle.ClosePrice > prevClose)
			{
				_riseCount++;
				_fallCount = 0;
			}
			else if (candle.ClosePrice < prevClose)
			{
				_fallCount++;
				_riseCount = 0;
			}
			else
			{
				_riseCount = _fallCount = 0;
			}
		}

		_prevClose = candle.ClosePrice;

		// Counter-trend: after consecutive falls + price below lower band => buy
		var longSetup = _fallCount >= NoOfFalls && candle.LowPrice < lower;
		// Counter-trend: after consecutive rises + price above upper band => sell
		var shortSetup = _riseCount >= NoOfRises && candle.HighPrice > upper;

		if (longSetup && Position <= 0)
		{
			BuyMarket();
		}
		else if (shortSetup && Position >= 0)
		{
			SellMarket();
		}
	}
}
