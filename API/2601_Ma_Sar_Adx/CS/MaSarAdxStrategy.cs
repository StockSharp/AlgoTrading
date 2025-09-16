namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Conversion of the MaSarADX MetaTrader strategy to StockSharp high level API.
/// Combines a moving average, ADX directional movement and Parabolic SAR for entries and exits.
/// </summary>
public class MaSarAdxStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Moving average period used for the trend filter.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Average Directional Index calculation period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Acceleration step for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for the Parabolic SAR indicator.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// Base order volume used for market entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Type of candles processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="MaSarAdxStrategy"/>.
	/// </summary>
	public MaSarAdxStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 100)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Length of the trend moving average", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 200, 10);

		_adxPeriod = Param(nameof(AdxPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ADX Period", "Length of the Average Directional Index", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(7, 28, 1);

		_sarStep = Param(nameof(SarStep), 0.02m)
		.SetRange(0.005m, 0.2m)
		.SetDisplay("SAR Step", "Acceleration step for Parabolic SAR", "Indicators")
		.SetCanOptimize(true);

		_sarMax = Param(nameof(SarMax), 0.1m)
		.SetRange(0.05m, 1m)
		.SetDisplay("SAR Maximum", "Maximum acceleration for Parabolic SAR", "Indicators")
		.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Base order size for new positions", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to request", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Instantiate indicators used in the original MetaTrader script.
		var movingAverage = new SimpleMovingAverage
		{
			Length = MaPeriod
		};

		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationStep = SarStep,
			AccelerationMax = SarMax
		};

		var adx = new AverageDirectionalIndex
		{
			Length = AdxPeriod
		};

		// Subscribe to candle data and bind indicator updates to a single handler.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, movingAverage, parabolicSar, ProcessCandle)
			.Start();

		// Draw the trading context for visual debugging when charts are available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, movingAverage);
			DrawIndicator(area, parabolicSar);
			DrawOwnTrades(area);

			var adxArea = CreateChartArea();
			if (adxArea != null)
			{
				DrawIndicator(adxArea, adx);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue maValue, IIndicatorValue sarValue)
	{
		// Work only with completed candles to mirror the original first-tick logic.
		if (candle.State != CandleStates.Finished)
		return;

		if (!adxValue.IsFinal || !maValue.IsFinal || !sarValue.IsFinal)
		return;

		if (adxValue is not AverageDirectionalIndexValue adxData)
		return;

		// Convert indicator values to simple decimals for decision making.
		var plusDi = adxData.Dx.Plus;
		var minusDi = adxData.Dx.Minus;
		var movingAverage = maValue.ToDecimal();
		var sar = sarValue.ToDecimal();

		// Always allow risk exits even if trading is temporarily disabled.
		if (Position > 0 && candle.ClosePrice < sar)
		{
			SellMarket(Math.Abs(Position));
			LogInfo($"Exit long at {candle.ClosePrice} because price crossed below SAR {sar}.");
			return;
		}

		if (Position < 0 && candle.ClosePrice > sar)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit short at {candle.ClosePrice} because price crossed above SAR {sar}.");
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		// Entry conditions replicated from the MetaTrader version.
		var bullishSignal = candle.ClosePrice > movingAverage && plusDi >= minusDi && candle.ClosePrice > sar;
		var bearishSignal = candle.ClosePrice < movingAverage && plusDi <= minusDi && candle.ClosePrice < sar;

		var volume = Volume + Math.Abs(Position);

		if (bullishSignal && Position <= 0)
		{
			BuyMarket(volume);
			LogInfo($"Enter long at {candle.ClosePrice}. Close {candle.ClosePrice}, MA {movingAverage}, +DI {plusDi}, -DI {minusDi}, SAR {sar}.");
			return;
		}

		if (bearishSignal && Position >= 0)
		{
			SellMarket(volume);
			LogInfo($"Enter short at {candle.ClosePrice}. Close {candle.ClosePrice}, MA {movingAverage}, +DI {plusDi}, -DI {minusDi}, SAR {sar}.");
		}
	}
}
