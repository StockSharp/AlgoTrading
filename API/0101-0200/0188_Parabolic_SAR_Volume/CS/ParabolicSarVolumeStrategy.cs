using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR Volume strategy.
/// A close above the Parabolic SAR on volume above the average of the previous VolumePeriod candles goes long and a close below it on such
/// volume goes short, reversing an opposite position. The SAR is the trailing stop: a long closes when the SAR flips above price and a short
/// when it flips below.
/// </summary>
public class ParabolicSarVolumeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _acceleration;
	private readonly StrategyParam<decimal> _maxAcceleration;
	private readonly StrategyParam<int> _volumePeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _volumes = [];

	/// <summary>
	/// Initial acceleration factor of the SAR.
	/// </summary>
	public decimal Acceleration
	{
		get => _acceleration.Value;
		set => _acceleration.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor of the SAR.
	/// </summary>
	public decimal MaxAcceleration
	{
		get => _maxAcceleration.Value;
		set => _maxAcceleration.Value = value;
	}

	/// <summary>
	/// Previous candles the volume is averaged over.
	/// </summary>
	public int VolumePeriod
	{
		get => _volumePeriod.Value;
		set => _volumePeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ParabolicSarVolumeStrategy()
	{
		_acceleration = Param(nameof(Acceleration), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Acceleration", "Initial acceleration factor of the SAR", "SAR");

		_maxAcceleration = Param(nameof(MaxAcceleration), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("SAR Max Acceleration", "Maximum acceleration factor of the SAR", "SAR");

		_volumePeriod = Param(nameof(VolumePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Period", "Previous candles the volume is averaged over", "Volume");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_volumes.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_volumes.Clear();

		var sar = new ParabolicSar
		{
			Acceleration = Acceleration,
			AccelerationMax = MaxAcceleration,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sar, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Volume is compared with the candles before this one.
		var average = _volumes.Count == VolumePeriod ? _volumes.Average() : (decimal?)null;

		_volumes.Add(candle.TotalVolume);

		if (_volumes.Count > VolumePeriod)
			_volumes.RemoveAt(0);

		// The first SAR value is formed but empty.
		if (!sarValue.IsFormed || sarValue.IsEmpty || average is not decimal avgVolume)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var sar = sarValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var surge = candle.TotalVolume > avgVolume;

		if (close > sar && surge && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < sar && surge && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close < sar)
			SellMarket(Position);
		else if (Position < 0 && close > sar)
			BuyMarket(-Position);
	}
}
