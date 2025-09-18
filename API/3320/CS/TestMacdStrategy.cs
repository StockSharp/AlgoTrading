using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the MetaTrader TestMACD expert behavior in StockSharp.
/// It looks for MACD line crossovers, applies fixed stop-loss and take-profit distances and trades with a fixed volume.
/// </summary>
public class TestMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal? _macd;
	private decimal? _previousMacdDiff;

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line length for MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Order volume used for each entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used to calculate indicators.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Create strategy parameters that mirror the original MetaTrader expert inputs.
	/// </summary>
	public TestMacdStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetDisplay("MACD Fast", "Fast EMA period for MACD", "MACD")
			.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 24)
			.SetDisplay("MACD Slow", "Slow EMA period for MACD", "MACD")
			.SetCanOptimize(true);

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetDisplay("MACD Signal", "Signal EMA period for MACD", "MACD")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 90)
			.SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk")
			.SetGreaterThanZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 110)
			.SetDisplay("Take Profit", "Take-profit distance in price steps", "Risk")
			.SetGreaterThanZero();

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Volume", "Fixed order volume", "Trading")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to subscribe", "Data");
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
		_previousMacdDiff = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		var takeProfitUnit = CreateProtectionUnit(TakeProfitPoints);
		var stopLossUnit = CreateProtectionUnit(StopLossPoints);

		StartProtection(takeProfitUnit, stopLossUnit);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private Unit? CreateProtectionUnit(int distanceInPoints)
	{
		if (distanceInPoints <= 0)
			return null;

		var priceStep = Security?.PriceStep;
		if (priceStep.HasValue && priceStep.Value > 0m)
			return new Unit(distanceInPoints, UnitTypes.PriceStep);

		var absoluteDistance = distanceInPoints * (Security?.MinPriceStep ?? 1m);
		return new Unit(absoluteDistance, UnitTypes.Absolute);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macdSignal)
			return;

		if (macdSignal.Macd is not decimal macd || macdSignal.Signal is not decimal signal)
			return;

		var macdDiff = macd - signal;

		if (_previousMacdDiff is null)
		{
			_previousMacdDiff = macdDiff;
			return;
		}

		var prevDiff = _previousMacdDiff.Value;
		var crossedUp = prevDiff <= 0m && macdDiff > 0m;
		var crossedDown = prevDiff >= 0m && macdDiff < 0m;

		if (crossedUp)
		{
			var volume = TradeVolume;
			if (Position < 0m)
				volume += -Position;

			if (volume > 0m)
			{
				BuyMarket(volume);
				LogInfo($"MACD bullish crossover detected at {candle.ClosePrice:F5}. Buying {volume}.");
			}
		}
		else if (crossedDown)
		{
			var volume = TradeVolume;
			if (Position > 0m)
				volume += Position;

			if (volume > 0m)
			{
				SellMarket(volume);
				LogInfo($"MACD bearish crossover detected at {candle.ClosePrice:F5}. Selling {volume}.");
			}
		}

		_previousMacdDiff = macdDiff;
	}
}
