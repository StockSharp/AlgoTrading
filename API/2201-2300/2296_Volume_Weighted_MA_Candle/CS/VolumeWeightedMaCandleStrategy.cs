using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume Weighted MA Candle strategy.
/// Uses VWMA slope direction combined with candle direction for signals.
/// Buys when VWMA turns up and candle is bullish, sells on opposite.
/// </summary>
public class VolumeWeightedMaCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _vwmaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevVwma;
	private decimal? _prevColor;

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

	public VolumeWeightedMaCandleStrategy()
	{
		_vwmaPeriod = Param(nameof(VwmaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("VWMA Period", "Period for volume weighted moving average", "Parameters")
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "Parameters");
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
		_prevColor = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevVwma = null;
		_prevColor = null;

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

		// Color: 2 = bullish (price above VWMA rising), 0 = bearish, 1 = neutral
		var priceAbove = candle.ClosePrice > vwmaValue;
		var priceBelow = candle.ClosePrice < vwmaValue;
		var rising = _prevVwma != null && vwmaValue > _prevVwma.Value;
		var falling = _prevVwma != null && vwmaValue < _prevVwma.Value;

		decimal currentColor;
		if (priceAbove && rising)
			currentColor = 2m;
		else if (priceBelow && falling)
			currentColor = 0m;
		else
			currentColor = 1m;

		if (_prevColor is decimal prevColor)
		{
			// Transition from bullish to not-bullish -> sell
			if (prevColor == 2m && currentColor < 2m && Position >= 0)
				SellMarket();
			// Transition from bearish to not-bearish -> buy
			else if (prevColor == 0m && currentColor > 0m && Position <= 0)
				BuyMarket();
		}

		_prevColor = currentColor;
		_prevVwma = vwmaValue;
	}
}
