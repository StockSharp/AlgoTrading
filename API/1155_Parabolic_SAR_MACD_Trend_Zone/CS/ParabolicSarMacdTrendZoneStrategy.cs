using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR with MACD confirmation.
/// Enter when price crosses SAR and MACD confirms.
/// </summary>
public class ParabolicSarMacdTrendZoneStrategy : Strategy
{
	private readonly StrategyParam<decimal> _sarStart;
	private readonly StrategyParam<decimal> _sarIncrement;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<DataType> _candleType;

	private bool _isPriceAboveSarPrev;
	private bool _isInitialized;

	/// <summary>
	/// Initial acceleration factor for SAR.
	/// </summary>
	public decimal SarStart
	{
		get => _sarStart.Value;
		set => _sarStart.Value = value;
	}

	/// <summary>
	/// Increment step for SAR.
	/// </summary>
	public decimal SarIncrement
	{
		get => _sarIncrement.Value;
		set => _sarIncrement.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for SAR.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal smoothing length for MACD.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
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
	/// Initialize strategy parameters.
	/// </summary>
	public ParabolicSarMacdTrendZoneStrategy()
	{
		_sarStart = Param(nameof(SarStart), 0.02m)
			.SetDisplay("SAR Start", "Initial acceleration factor for SAR", "Parabolic SAR")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.05m, 0.01m);

		_sarIncrement = Param(nameof(SarIncrement), 0.02m)
			.SetDisplay("SAR Step", "Increment step for SAR", "Parabolic SAR")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.05m, 0.01m);

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetDisplay("SAR Max", "Maximum acceleration factor for SAR", "Parabolic SAR")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 0.4m, 0.1m);

		_macdFast = Param(nameof(MacdFast), 12)
			.SetDisplay("MACD Fast", "Fast EMA length for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(8, 16, 2);

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetDisplay("MACD Slow", "Slow EMA length for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetDisplay("MACD Signal", "Signal smoothing length for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_isInitialized = false;
		_isPriceAboveSarPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var parabolicSar = new ParabolicSar
		{
			Acceleration = SarStart,
			AccelerationStep = SarIncrement,
			AccelerationMax = SarMax
		};

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(parabolicSar, macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, parabolicSar);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!sarValue.IsFinal || !macdValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var sar = sarValue.GetValue<decimal>();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdLine = macdTyped.Macd;
		var signalLine = macdTyped.Signal;

		var isPriceAboveSar = candle.ClosePrice > sar;
		var macdBullish = macdLine > signalLine;
		var macdBearish = macdLine < signalLine;

		if (!_isInitialized)
		{
			_isPriceAboveSarPrev = isPriceAboveSar;
			_isInitialized = true;
			return;
		}

		var volume = Volume + Math.Abs(Position);

		if (isPriceAboveSar && !_isPriceAboveSarPrev && macdBullish && Position <= 0)
		{
			BuyMarket(volume);
		}
		else if (!isPriceAboveSar && _isPriceAboveSarPrev && macdBearish && Position >= 0)
		{
			SellMarket(volume);
		}
		else if (Position > 0 && (!isPriceAboveSar || macdBearish))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (isPriceAboveSar || macdBullish))
		{
			BuyMarket(-Position);
		}

		_isPriceAboveSarPrev = isPriceAboveSar;
	}
}
