using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on DeMarker crossing a threshold.
/// </summary>
public class ArdOrderManagementStrategy : Strategy
{
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _previousValue;
	private bool _hasPrev;

	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ArdOrderManagementStrategy()
	{
		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("DeMarker Period", "DeMarker indicator period", "Parameters");

		_threshold = Param(nameof(Threshold), 0.5m)
			.SetDisplay("Threshold", "DeMarker crossing level", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousValue = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var deMarker = new DeMarker { Length = DeMarkerPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(deMarker, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, deMarker);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal deMarkerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_previousValue = deMarkerValue;
			_hasPrev = true;
			return;
		}

		var buySignal = _previousValue > Threshold && deMarkerValue < Threshold;
		var sellSignal = _previousValue < Threshold && deMarkerValue > Threshold;

		if (buySignal && Position <= 0)
			BuyMarket();
		else if (sellSignal && Position >= 0)
			SellMarket();

		_previousValue = deMarkerValue;
	}
}
