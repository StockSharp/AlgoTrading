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
/// Strategy reacting to volume delta similar to "Tape" indicator.
/// </summary>
public class TapeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastPrice;
	private decimal _lastVolume;

	public decimal VolumeDeltaThreshold { get => _threshold.Value; set => _threshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TapeStrategy()
	{
		_threshold = Param(nameof(VolumeDeltaThreshold), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Delta", "Volume delta threshold", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_lastPrice = 0m;
		_lastVolume = 0m;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_lastPrice = 0m;
		_lastVolume = 0m;

		var sub = SubscribeCandles(CandleType);
		sub.Bind(Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_lastPrice == 0)
		{
			_lastPrice = candle.ClosePrice;
			_lastVolume = candle.TotalVolume;
			return;
		}

		var deltaVolume = (candle.TotalVolume - _lastVolume) * Math.Sign(candle.ClosePrice - _lastPrice);

		if (deltaVolume > VolumeDeltaThreshold && Position <= 0)
			BuyMarket();
		else if (deltaVolume < -VolumeDeltaThreshold && Position >= 0)
			SellMarket();

		_lastPrice = candle.ClosePrice;
		_lastVolume = candle.TotalVolume;
	}
}
