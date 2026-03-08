using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Volume Weighted MA slope turning points.
/// Opens long when VWMA slope turns upward, short when it turns downward.
/// </summary>
public class VolumeWeightedMaDigitSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _vwmaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevVwma;
	private decimal? _prevSlope;

	public int VwmaPeriod
	{
		get => _vwmaPeriod.Value;
		set => _vwmaPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public VolumeWeightedMaDigitSystemStrategy()
	{
		_vwmaPeriod = Param(nameof(VwmaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("VWMA Period", "Length of the VWMA indicator", "Parameters")
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe for analysis", "Parameters");
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
		_prevVwma = null;
		_prevSlope = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevVwma = null;
		_prevSlope = null;

		var vwma = new VolumeWeightedMovingAverage { Length = VwmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(vwma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevVwma is decimal prev)
		{
			var slope = vwmaValue - prev;

			if (_prevSlope is decimal prevSlope)
			{
				var turnedUp = prevSlope <= 0 && slope > 0;
				var turnedDown = prevSlope >= 0 && slope < 0;

				if (turnedUp && Position <= 0)
					BuyMarket();
				else if (turnedDown && Position >= 0)
					SellMarket();
			}

			_prevSlope = slope;
		}

		_prevVwma = vwmaValue;
	}
}
