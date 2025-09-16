using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the MetaTrader "MACD + Parabolic SAR" expert built with the MQL5 Wizard.
/// Combines the trend direction from Parabolic SAR with MACD momentum scores and uses weighted thresholds for decisions.
/// </summary>
public class MacdParabolicSarWizardStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _macdWeight;
	private readonly StrategyParam<decimal> _sarWeight;
	private readonly StrategyParam<decimal> _openThreshold;
	private readonly StrategyParam<decimal> _closeThreshold;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMax;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private ParabolicSar _parabolicSar = null!;
	private decimal _lastBullScore;
	private decimal _lastBearScore;

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Weight of the MACD signal in the combined score (0..1).
	/// </summary>
	public decimal MacdWeight
	{
		get => _macdWeight.Value;
		set => _macdWeight.Value = value;
	}

	/// <summary>
	/// Weight of the Parabolic SAR signal in the combined score (0..1).
	/// </summary>
	public decimal SarWeight
	{
		get => _sarWeight.Value;
		set => _sarWeight.Value = value;
	}

	/// <summary>
	/// Minimum combined bullish or bearish score required to open a position.
	/// </summary>
	public decimal OpenThreshold
	{
		get => _openThreshold.Value;
		set => _openThreshold.Value = value;
	}

	/// <summary>
	/// Minimum opposite score required to exit the current position.
	/// </summary>
	public decimal CloseThreshold
	{
		get => _closeThreshold.Value;
		set => _closeThreshold.Value = value;
	}

	/// <summary>
	/// Acceleration step for Parabolic SAR.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Maximum acceleration factor for Parabolic SAR.
	/// </summary>
	public decimal SarMax
	{
		get => _sarMax.Value;
		set => _sarMax.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="MacdParabolicSarWizardStrategy"/>.
	/// </summary>
	public MacdParabolicSarWizardStrategy()
	{
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
		.SetDisplay("MACD Fast", "Fast EMA period for MACD", "MACD")
		.SetCanOptimize(true);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 24)
		.SetDisplay("MACD Slow", "Slow EMA period for MACD", "MACD")
		.SetCanOptimize(true);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetDisplay("MACD Signal", "Signal SMA period for MACD", "MACD")
		.SetCanOptimize(true);

		_macdWeight = Param(nameof(MacdWeight), 0.9m)
		.SetDisplay("MACD Weight", "Relative weight of MACD in scoring", "Scoring")
		.SetCanOptimize(true);

		_sarWeight = Param(nameof(SarWeight), 0.1m)
		.SetDisplay("SAR Weight", "Relative weight of SAR in scoring", "Scoring")
		.SetCanOptimize(true);

		_openThreshold = Param(nameof(OpenThreshold), 20m)
		.SetDisplay("Open Threshold", "Score required to open trades", "Scoring")
		.SetCanOptimize(true);

		_closeThreshold = Param(nameof(CloseThreshold), 100m)
		.SetDisplay("Close Threshold", "Score required to exit trades", "Scoring")
		.SetCanOptimize(true);

		_sarStep = Param(nameof(SarStep), 0.02m)
		.SetDisplay("SAR Step", "Acceleration factor for Parabolic SAR", "Parabolic SAR")
		.SetCanOptimize(true);

		_sarMax = Param(nameof(SarMax), 0.2m)
		.SetDisplay("SAR Max", "Maximum acceleration for Parabolic SAR", "Parabolic SAR")
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
		.SetDisplay("Stop Loss (pts)", "Stop-loss distance in points", "Risk")
		.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 115m)
		.SetDisplay("Take Profit (pts)", "Take-profit distance in points", "Risk")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle source", "General");
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

		_lastBullScore = 0m;
		_lastBearScore = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Configure MACD indicator replicating the wizard defaults.
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod },
			},
			SignalMa = { Length = MacdSignalPeriod }
		};

		// Configure Parabolic SAR indicator.
		_parabolicSar = new ParabolicSar
		{
			AccelerationStep = SarStep,
			AccelerationMax = SarMax,
		};

		// Subscribe to candles and bind indicators.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _parabolicSar, ProcessCandle)
			.Start();

		// Configure risk management using point-based distances.
		var step = Security?.PriceStep ?? 1m;
		var takeProfit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * step, UnitTypes.Point) : new Unit();
		var stopLoss = StopLossPoints > 0m ? new Unit(StopLossPoints * step, UnitTypes.Point) : new Unit();

		StartProtection(takeProfit, stopLoss, useMarketOrders: true);

		// Prepare chart visualization if the environment supports it.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _parabolicSar);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue sarValue)
	{
		// Process only finished candles to avoid premature trades.
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal || !sarValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
			return;

		var sar = sarValue.ToDecimal();

		// Translate indicator states into normalized scores (0..100).
		var macdBull = macdLine > signalLine ? 100m : 0m;
		var macdBear = macdLine < signalLine ? 100m : 0m;
		var sarBull = candle.ClosePrice > sar ? 100m : 0m;
		var sarBear = candle.ClosePrice < sar ? 100m : 0m;

		var bullScore = macdBull * MacdWeight + sarBull * SarWeight;
		var bearScore = macdBear * MacdWeight + sarBear * SarWeight;

		_lastBullScore = bullScore;
		_lastBearScore = bearScore;

		// Exit conditions take priority to mirror the wizard behaviour.
		if (Position > 0 && bearScore >= CloseThreshold)
		{
			SellMarket(Position);
			return;
		}

		if (Position < 0 && bullScore >= CloseThreshold)
		{
			BuyMarket(-Position);
			return;
		}

		// Entry rules: open when the weighted score exceeds the open threshold.
		if (Position <= 0 && bullScore >= OpenThreshold)
		{
			BuyMarket(Volume + Math.Abs(Position));
			return;
		}

		if (Position >= 0 && bearScore >= OpenThreshold)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
