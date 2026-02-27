using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR strategy using a single timeframe with fast and slow SAR parameters.
/// Buys when price is above both SAR levels, sells when below both.
/// </summary>
public class ThreeParabolicSarStrategy : Strategy
{
	private readonly StrategyParam<decimal> _fastAcceleration;
	private readonly StrategyParam<decimal> _fastMaxAcceleration;
	private readonly StrategyParam<decimal> _slowAcceleration;
	private readonly StrategyParam<decimal> _slowMaxAcceleration;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _prevFastAbove;
	private bool? _prevSlowAbove;

	/// <summary>
	/// Fast SAR acceleration.
	/// </summary>
	public decimal FastAcceleration
	{
		get => _fastAcceleration.Value;
		set => _fastAcceleration.Value = value;
	}

	/// <summary>
	/// Fast SAR maximum acceleration.
	/// </summary>
	public decimal FastMaxAcceleration
	{
		get => _fastMaxAcceleration.Value;
		set => _fastMaxAcceleration.Value = value;
	}

	/// <summary>
	/// Slow SAR acceleration.
	/// </summary>
	public decimal SlowAcceleration
	{
		get => _slowAcceleration.Value;
		set => _slowAcceleration.Value = value;
	}

	/// <summary>
	/// Slow SAR maximum acceleration.
	/// </summary>
	public decimal SlowMaxAcceleration
	{
		get => _slowMaxAcceleration.Value;
		set => _slowMaxAcceleration.Value = value;
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
	/// Initializes a new instance.
	/// </summary>
	public ThreeParabolicSarStrategy()
	{
		_fastAcceleration = Param(nameof(FastAcceleration), 0.04m)
			.SetDisplay("Fast Acceleration", "Fast SAR acceleration", "SAR");

		_fastMaxAcceleration = Param(nameof(FastMaxAcceleration), 0.2m)
			.SetDisplay("Fast Max Accel", "Fast SAR max acceleration", "SAR");

		_slowAcceleration = Param(nameof(SlowAcceleration), 0.01m)
			.SetDisplay("Slow Acceleration", "Slow SAR acceleration", "SAR");

		_slowMaxAcceleration = Param(nameof(SlowMaxAcceleration), 0.1m)
			.SetDisplay("Slow Max Accel", "Slow SAR max acceleration", "SAR");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFastAbove = null;
		_prevSlowAbove = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastSar = new ParabolicSar { Acceleration = FastAcceleration, AccelerationMax = FastMaxAcceleration };
		var slowSar = new ParabolicSar { Acceleration = SlowAcceleration, AccelerationMax = SlowMaxAcceleration };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastSar, slowSar, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSar);
			DrawIndicator(area, slowSar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastSar, decimal slowSar)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastAbove = candle.ClosePrice > fastSar;
		var slowAbove = candle.ClosePrice > slowSar;

		if (_prevFastAbove != null && _prevSlowAbove != null)
		{
			// Buy when both SAR levels flip bullish
			if (fastAbove && slowAbove && (!_prevFastAbove.Value || !_prevSlowAbove.Value) && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			// Sell when both SAR levels flip bearish
			else if (!fastAbove && !slowAbove && (_prevFastAbove.Value || _prevSlowAbove.Value) && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}

			// Exit long if slow SAR turns bearish
			if (Position > 0 && !slowAbove)
				SellMarket();
			// Exit short if slow SAR turns bullish
			else if (Position < 0 && slowAbove)
				BuyMarket();
		}

		_prevFastAbove = fastAbove;
		_prevSlowAbove = slowAbove;
	}
}
