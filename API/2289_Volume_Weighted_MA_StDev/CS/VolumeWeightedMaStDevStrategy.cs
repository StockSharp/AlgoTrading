using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume Weighted Moving Average with Standard Deviation filter.
/// Opens long when VWMA momentum exceeds threshold, short on opposite.
/// </summary>
public class VolumeWeightedMaStDevStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _vwmaLength;
	private readonly StrategyParam<int> _stdPeriod;
	private readonly StrategyParam<decimal> _k1;

	private VolumeWeightedMovingAverage _vwma;
	private StandardDeviation _stdDev;
	private decimal? _prevVwma;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int VwmaLength { get => _vwmaLength.Value; set => _vwmaLength.Value = value; }
	public int StdPeriod { get => _stdPeriod.Value; set => _stdPeriod.Value = value; }
	public decimal K1 { get => _k1.Value; set => _k1.Value = value; }

	public VolumeWeightedMaStDevStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for analysis", "General");

		_vwmaLength = Param(nameof(VwmaLength), 12)
			.SetDisplay("VWMA Length", "Period for Volume Weighted MA", "Indicators");

		_stdPeriod = Param(nameof(StdPeriod), 9)
			.SetDisplay("StdDev Period", "Period for standard deviation", "Indicators");

		_k1 = Param(nameof(K1), 0.5m)
			.SetDisplay("K1", "Deviation multiplier for signal threshold", "Signal");
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
		_vwma = null;
		_stdDev = null;
		_prevVwma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevVwma = null;

		_vwma = new VolumeWeightedMovingAverage { Length = VwmaLength };
		_stdDev = new StandardDeviation { Length = StdPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_vwma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _vwma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_vwma.IsFormed)
			return;
		var t = candle.ServerTime;

		if (_prevVwma is null)
		{
			_prevVwma = vwmaValue;
			return;
		}

		var diff = vwmaValue - _prevVwma.Value;

		var stdResult = _stdDev.Process(new DecimalIndicatorValue(_stdDev, diff, t) { IsFinal = true });

		if (!_stdDev.IsFormed)
		{
			_prevVwma = vwmaValue;
			return;
		}

		var stdValue = stdResult.ToDecimal();
		var filter = K1 * stdValue;

		if (diff > filter && Position <= 0)
			BuyMarket();
		else if (diff < -filter && Position >= 0)
			SellMarket();

		_prevVwma = vwmaValue;
	}
}
