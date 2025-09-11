using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on directional index difference between +DI and -DI.
/// Enters long when directional index is above the key level and short when below the negative key level.
/// </summary>
public class DirectionalIndexKxDMIStrategy : Strategy
{
	private readonly StrategyParam<int> _diLength;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<int> _step;
	private readonly StrategyParam<int> _keyLevel;
	private readonly StrategyParam<DataType> _candleType;

	private AverageDirectionalIndex _adx;

	/// <summary>
	/// DI calculation length.
	/// </summary>
	public int DiLength
	{
		get => _diLength.Value;
		set => _diLength.Value = value;
	}

	/// <summary>
	/// ADX smoothing length.
	/// </summary>
	public int SmoothLength
	{
		get => _smoothLength.Value;
		set => _smoothLength.Value = value;
	}

	/// <summary>
	/// Rounding step for directional index.
	/// </summary>
	public int Step
	{
		get => _step.Value;
		set => _step.Value = value;
	}

	/// <summary>
	/// Key level for directional index.
	/// </summary>
	public int KeyLevel
	{
		get => _keyLevel.Value;
		set => _keyLevel.Value = value;
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
	/// Initializes a new instance of <see cref="DirectionalIndexKxDMIStrategy"/>.
	/// </summary>
	public DirectionalIndexKxDMIStrategy()
	{
		_diLength = Param(nameof(DiLength), 7)
						.SetDisplay("DI Length", "Directional index calculation length", "Directional Index")
						.SetCanOptimize(true)
						.SetOptimize(5, 20, 1);

		_smoothLength = Param(nameof(SmoothLength), 3)
							.SetDisplay("ADX Smoothing", "ADX smoothing length", "Directional Index")
							.SetCanOptimize(true)
							.SetOptimize(1, 8, 1);

		_step = Param(nameof(Step), 2)
					.SetDisplay("Step", "Rounding step for directional index", "Directional Index")
					.SetCanOptimize(true)
					.SetOptimize(1, 10, 1);

		_keyLevel = Param(nameof(KeyLevel), 25)
						.SetDisplay("Key Level", "Directional index threshold", "Directional Index")
						.SetCanOptimize(true)
						.SetOptimize(10, 50, 5);

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
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_adx = new AverageDirectionalIndex { Length = DiLength, Smooth = SmoothLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_adx, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;

		if (adxTyped.Dx.Plus is not decimal plusDi || adxTyped.Dx.Minus is not decimal minusDi)
			return;

		var sum = plusDi + minusDi;
		if (sum == 0)
			return;

		var adxDirectional = 100m * (plusDi - minusDi) / sum;

		if (Step > 0)
			adxDirectional = Math.Round(adxDirectional / Step) * Step;

		var absAdx = Math.Abs(adxDirectional);
		var volume = Volume + Math.Abs(Position);

		if (absAdx < KeyLevel)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));

			return;
		}

		if (adxDirectional > KeyLevel && Position <= 0)
		{
			BuyMarket(volume);
		}
		else if (adxDirectional < -KeyLevel && Position >= 0)
		{
			SellMarket(volume);
		}
	}
}