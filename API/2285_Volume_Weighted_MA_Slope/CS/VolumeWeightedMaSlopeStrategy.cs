using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume Weighted Moving Average slope strategy.
/// Goes long when VWMA turns up (valley), short when VWMA turns down (peak).
/// </summary>
public class VolumeWeightedMaSlopeStrategy : Strategy
{
	private readonly StrategyParam<int> _vwmaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevVwma1;
	private decimal? _prevVwma2;

	public int VwmaPeriod { get => _vwmaPeriod.Value; set => _vwmaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VolumeWeightedMaSlopeStrategy()
	{
		_vwmaPeriod = Param(nameof(VwmaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("VWMA Period", "Period of the Volume Weighted Moving Average", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_prevVwma1 = null;
		_prevVwma2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevVwma1 = null;
		_prevVwma2 = null;

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

	private void ProcessCandle(ICandleMessage candle, decimal currentVwma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevVwma1 is null)
		{
			_prevVwma1 = currentVwma;
			return;
		}

		if (_prevVwma2 is null)
		{
			_prevVwma2 = _prevVwma1;
			_prevVwma1 = currentVwma;
			return;
		}

		// Valley: was falling, now rising -> buy
		if (_prevVwma2 > _prevVwma1 && currentVwma > _prevVwma1 && Position <= 0)
			BuyMarket();
		// Peak: was rising, now falling -> sell
		else if (_prevVwma2 < _prevVwma1 && currentVwma < _prevVwma1 && Position >= 0)
			SellMarket();

		_prevVwma2 = _prevVwma1;
		_prevVwma1 = currentVwma;
	}
}
