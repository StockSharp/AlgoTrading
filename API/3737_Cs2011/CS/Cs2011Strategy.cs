
namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// MACD based reversal strategy converted from the original cs2011 MetaTrader 5 expert advisor.
/// It reacts to zero line crosses and local extremes of the MACD signal line.
/// </summary>
public class Cs2011Strategy : Strategy
{
	private readonly StrategyParam<decimal> _targetVolume;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _macdPrev1;
	private decimal? _macdPrev2;
	private decimal? _signalPrev1;
	private decimal? _signalPrev2;
	private decimal? _signalPrev3;

	/// <summary>
	/// Target absolute position in lots when a bullish signal appears.
	/// </summary>
	public decimal TargetVolume
	{
		get => _targetVolume.Value;
		set => _targetVolume.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in instrument points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Fast EMA length used in MACD calculation.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length used in MACD calculation.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the MACD signal line.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters with defaults close to the original expert.
	/// </summary>
	public Cs2011Strategy()
	{
		_targetVolume = Param(nameof(TargetVolume), 1m)
			.SetDisplay("Target Volume", "Absolute position size targeted on entries", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2200)
			.SetDisplay("Take Profit (points)", "Take-profit distance in price points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(200, 4000, 200);

		_stopLossPoints = Param(nameof(StopLossPoints), 0)
			.SetDisplay("Stop Loss (points)", "Stop-loss distance in price points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 2000, 200);

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 30)
			.SetDisplay("Fast EMA", "Fast EMA period for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 500)
			.SetDisplay("Slow EMA", "Slow EMA period for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(200, 700, 20);

		_signalPeriod = Param(nameof(SignalPeriod), 36)
			.SetDisplay("Signal Period", "Signal line period for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Source timeframe for MACD", "General");
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

		_macdPrev1 = null;
		_macdPrev2 = null;
		_signalPrev1 = null;
		_signalPrev2 = null;
		_signalPrev3 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TargetVolume;

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastEmaPeriod },
				LongMa = { Length = SlowEmaPeriod }
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var chartArea = CreateChartArea();
		if (chartArea != null)
		{
			DrawCandles(chartArea, subscription);
			DrawIndicator(chartArea, macd);
			DrawOwnTrades(chartArea);
		}

		var step = Security?.PriceStep ?? 1m;
		Unit takeProfit = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints * step, UnitTypes.Point) : null;
		Unit stopLoss = StopLossPoints > 0 ? new Unit(StopLossPoints * step, UnitTypes.Point) : null;

		StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished || !indicatorValue.IsFinal)
			return;

		if (indicatorValue is not MovingAverageConvergenceDivergenceSignalValue macdValue)
			return;

		if (macdValue.Macd is not decimal macd || macdValue.Signal is not decimal signal)
			return;

		var prevMacd1 = _macdPrev1;
		var prevMacd2 = _macdPrev2;
		var prevSignal1 = _signalPrev1;
		var prevSignal2 = _signalPrev2;
		var prevSignal3 = _signalPrev3;

		var upSignal = false;
		var downSignal = false;

		if (prevMacd1.HasValue && prevMacd2.HasValue)
		{
			if (prevMacd1 > 0m && prevMacd2 < 0m)
				downSignal = true;

			if (prevMacd1 < 0m && prevMacd2 > 0m)
				upSignal = true;
		}

		if (prevMacd2.HasValue && prevSignal1.HasValue && prevSignal2.HasValue && prevSignal3.HasValue)
		{
			if (prevMacd2 < 0m && prevSignal1 < prevSignal2 && prevSignal2 > prevSignal3)
				downSignal = true;

			if (prevMacd2 > 0m && prevSignal1 > prevSignal2 && prevSignal2 < prevSignal3)
				upSignal = true;
		}

		if (upSignal || downSignal)
			ExecuteSignals(upSignal, downSignal);

		_macdPrev2 = _macdPrev1;
		_macdPrev1 = macd;
		_signalPrev3 = _signalPrev2;
		_signalPrev2 = _signalPrev1;
		_signalPrev1 = signal;
	}

	private void ExecuteSignals(bool upSignal, bool downSignal)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (upSignal)
		{
			var targetPosition = TargetVolume;
			var difference = targetPosition - Position;
			if (difference > 0m)
			{
				BuyMarket(difference);
				LogInfo($"Buy signal executed. Target={targetPosition:F2}, current position after order={Position + difference:F2}");
			}
		}

		if (downSignal)
		{
			var targetPosition = -TargetVolume;
			var difference = targetPosition - Position;
			if (difference < 0m)
			{
				SellMarket(-difference);
				LogInfo($"Sell signal executed. Target={targetPosition:F2}, current position after order={Position - difference:F2}");
			}
		}
	}
}
