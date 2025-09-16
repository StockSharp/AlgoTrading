using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume Weighted Moving Average slope strategy.
/// Goes long when VWMA is rising for two consecutive bars.
/// Goes short when VWMA is falling for two consecutive bars.
/// Closes existing positions on opposite slope.
/// </summary>
public class VolumeWeightedMaSlopeStrategy : Strategy
{
	private readonly StrategyParam<int> _vwmaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevVwma1;
	private decimal _prevVwma2;
	private bool _isFirst;

	/// <summary>
	/// VWMA calculation period.
	/// </summary>
	public int VwmaPeriod
	{
		get => _vwmaPeriod.Value;
		set => _vwmaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="VolumeWeightedMaSlopeStrategy"/>.
	/// </summary>
	public VolumeWeightedMaSlopeStrategy()
	{
		_vwmaPeriod = Param(nameof(VwmaPeriod), 12)
				.SetGreaterThanZero()
				.SetDisplay("VWMA Period", "Period of the Volume Weighted Moving Average", "General")
				.SetCanOptimize(true)
				.SetOptimize(5, 30, 5);

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

		_prevVwma1 = 0m;
		_prevVwma2 = 0m;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var vwma = new VolumeWeightedMovingAverage { Length = VwmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
				.BindEx(vwma, ProcessCandle)
				.Start();

		StartProtection(
				takeProfit: new Unit(2, UnitTypes.Percent),
				stopLoss: new Unit(1, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
				DrawCandles(area, subscription);
				DrawIndicator(area, vwma);
				DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue vwmaValue)
	{
		if (candle.State != CandleStates.Finished)
				return;

		if (!IsFormedAndOnlineAndAllowTrading())
				return;

		var currentVwma = vwmaValue.ToDecimal();

		if (_isFirst)
		{
				_prevVwma1 = currentVwma;
				_prevVwma2 = currentVwma;
				_isFirst = false;
				return;
		}

		var upSlope = _prevVwma2 < _prevVwma1 && currentVwma > _prevVwma1;
		var downSlope = _prevVwma2 > _prevVwma1 && currentVwma < _prevVwma1;

		if (upSlope)
		{
				if (Position < 0)
				BuyMarket(Math.Abs(Position));

				if (Position <= 0)
				BuyMarket(Volume);
		}
		else if (downSlope)
		{
				if (Position > 0)
				SellMarket(Math.Abs(Position));

				if (Position >= 0)
				SellMarket(Volume);
		}

		_prevVwma2 = _prevVwma1;
		_prevVwma1 = currentVwma;
	}
}
