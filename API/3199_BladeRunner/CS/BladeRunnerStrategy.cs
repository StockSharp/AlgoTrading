using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Bladerunner" MetaTrader expert advisor.
/// </summary>
public class BladeRunnerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _filterMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _fractalLookback;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _takeProfitSteps;
	private readonly StrategyParam<int> _stopLossSteps;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private LinearWeightedMovingAverage _filterMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergence _macd = null!;

	private readonly List<CandleSnapshot> _candleHistory = new();
	private readonly List<decimal> _filterHistory = new();

	private readonly decimal[] _momentumHistory = new decimal[3];
	private int _momentumCount;

	private decimal? _fastMaValue;
	private decimal? _slowMaValue;
	private decimal? _filterMaValue;

	private decimal? _macdMain;
	private decimal? _macdSignal;

	private FractalDirection _latestFractalDirection = FractalDirection.None;
	private decimal? _latestFractalPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="BladeRunnerStrategy"/> class.
	/// </summary>
	public BladeRunnerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Primary Candle", "Base timeframe used by the core logic.", "Data");

		_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Momentum Candle", "Higher timeframe for the momentum filter.", "Data");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("MACD Candle", "Timeframe used to reproduce the monthly MACD filter.", "Data");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetRange(3, 60)
			.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average.", "Trend")
			.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
			.SetRange(20, 200)
			.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average.", "Trend")
			.SetCanOptimize(true);

		_filterMaPeriod = Param(nameof(FilterMaPeriod), 20)
			.SetRange(5, 100)
			.SetDisplay("Filter LWMA", "Moving average used by the fractal validation rules.", "Trend")
			.SetCanOptimize(true);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetRange(5, 50)
			.SetDisplay("Momentum Period", "Averaging length for the momentum oscillator.", "Momentum")
			.SetCanOptimize(true);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
			.SetNotNegative()
			.SetDisplay("Momentum Threshold", "Minimum absolute deviation from 100 required for entries.", "Momentum")
			.SetCanOptimize(true);

		_fractalLookback = Param(nameof(FractalLookback), 200)
			.SetRange(20, 400)
			.SetDisplay("Fractal Lookback", "Number of completed candles kept for fractal analysis.", "Fractals");

		_maxTrades = Param(nameof(MaxTrades), 3)
			.SetRange(1, 10)
			.SetDisplay("Max Trades", "Maximum number of scale-in orders per direction.", "Risk")
			.SetCanOptimize(true);

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetNotNegative()
			.SetDisplay("Order Volume", "Base volume used for each market order.", "Trading");

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50)
			.SetNotNegative()
			.SetDisplay("Take Profit (steps)", "Protective take-profit distance expressed in price steps.", "Risk")
			.SetCanOptimize(true);

		_stopLossSteps = Param(nameof(StopLossSteps), 20)
			.SetNotNegative()
			.SetDisplay("Stop Loss (steps)", "Protective stop-loss distance expressed in price steps.", "Risk")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Primary candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type used to calculate the momentum oscillator.
	/// </summary>
	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate the MACD filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Length of the fast LWMA.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Length of the slow LWMA.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Length of the LWMA used to validate fractals.
	/// </summary>
	public int FilterMaPeriod
	{
		get => _filterMaPeriod.Value;
		set => _filterMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum averaging period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute momentum deviation from 100.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Maximum number of scale-in trades per direction.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Base volume for new market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public int StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Number of completed candles stored for fractal calculations.
	/// </summary>
	public int FractalLookback
	{
		get => _fractalLookback.Value;
		set => _fractalLookback.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new LinearWeightedMovingAverage { Length = FastMaPeriod, CandlePrice = CandlePrice.Typical };
		_slowMa = new LinearWeightedMovingAverage { Length = SlowMaPeriod, CandlePrice = CandlePrice.Typical };
		_filterMa = new LinearWeightedMovingAverage { Length = FilterMaPeriod, CandlePrice = CandlePrice.Typical };
		_momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergence
		{
			ShortLength = 12,
			LongLength = 26,
			SignalLength = 9,
			CandlePrice = CandlePrice.Typical,
		};

		var primarySubscription = SubscribeCandles(CandleType);
		primarySubscription
			.Bind(_fastMa, _slowMa, _filterMa, ProcessPrimaryCandle)
			.Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription
			.Bind(_momentum, ProcessMomentumCandle)
			.Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription
			.BindEx(_macd, ProcessMacdCandle)
			.Start();

		var takeProfitUnit = TakeProfitSteps > 0 ? new Unit(TakeProfitSteps, UnitTypes.Step) : null;
		var stopLossUnit = StopLossSteps > 0 ? new Unit(StopLossSteps, UnitTypes.Step) : null;

		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit, useMarketOrders: true);

		var adjustedVolume = AdjustVolume(OrderVolume);
		Volume = adjustedVolume > 0m ? adjustedVolume : OrderVolume;

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, primarySubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _filterMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessPrimaryCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal filterValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_fastMaValue = fastValue;
		_slowMaValue = slowValue;
		_filterMaValue = filterValue;

		_candleHistory.Add(new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice));
		_filterHistory.Add(filterValue);

		DetectFractals();
		TrimHistory();

		TryGenerateSignals(candle);
	}

	private void ProcessMomentumCandle(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var deviation = Math.Abs(momentumValue - 100m);

		for (var i = _momentumHistory.Length - 1; i > 0; i--)
			_momentumHistory[i] = _momentumHistory[i - 1];

		_momentumHistory[0] = deviation;

		if (_momentumCount < _momentumHistory.Length)
			_momentumCount++;
	}

	private void ProcessMacdCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (!macdValue.IsFinal || macdValue is not MovingAverageConvergenceDivergenceValue macd)
			return;

		if (macd.Macd is not decimal macdMain || macd.Signal is not decimal macdSignal)
			return;

		_macdMain = macdMain;
		_macdSignal = macdSignal;
	}

	private void DetectFractals()
	{
		if (_candleHistory.Count < 5)
			return;

		var centerIndex = _candleHistory.Count - 3;
		if (centerIndex < 2 || centerIndex + 2 >= _candleHistory.Count)
			return;

		var center = _candleHistory[centerIndex];
		var prev1 = _candleHistory[centerIndex - 1];
		var prev2 = _candleHistory[centerIndex - 2];
		var next1 = _candleHistory[centerIndex + 1];
		var next2 = _candleHistory[centerIndex + 2];

		var isUpper = center.High > prev1.High && center.High > prev2.High && center.High > next1.High && center.High > next2.High;
		var isLower = center.Low < prev1.Low && center.Low < prev2.Low && center.Low < next1.Low && center.Low < next2.Low;

		if (isUpper && !isLower)
		{
			var validationOpen = next1.Open;
			var validationMa = _filterHistory[centerIndex + 1];

			if (validationOpen < center.High && validationOpen < validationMa)
			{
				_latestFractalDirection = FractalDirection.Upper;
				_latestFractalPrice = center.High;
			}
		}
		else if (isLower && !isUpper)
		{
			var validationOpen = next1.Open;
			var validationMa = _filterHistory[centerIndex + 1];

			if (validationOpen > center.Low && validationOpen > validationMa)
			{
				_latestFractalDirection = FractalDirection.Lower;
				_latestFractalPrice = center.Low;
			}
		}
	}

	private void TrimHistory()
	{
		var limit = Math.Max(FractalLookback, 16);

		if (_candleHistory.Count <= limit)
			return;

		var excess = _candleHistory.Count - limit;
		_candleHistory.RemoveRange(0, excess);
		_filterHistory.RemoveRange(0, excess);
	}

	private void TryGenerateSignals(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_filterMa.IsFormed)
			return;

		if (!_momentum.IsFormed || !_macd.IsFormed)
			return;

		if (_fastMaValue is not decimal fast || _slowMaValue is not decimal slow || _filterMaValue is not decimal)
			return;

		if (_macdMain is not decimal macd || _macdSignal is not decimal macdSignal)
			return;

		if (_momentumCount == 0)
			return;

		var momentumOk = false;
		for (var i = 0; i < _momentumCount; i++)
		{
			if (_momentumHistory[i] >= MomentumThreshold)
			{
				momentumOk = true;
				break;
			}
		}

		if (!momentumOk)
			return;

		if (_latestFractalDirection == FractalDirection.Upper && fast > slow && macd > macdSignal)
		{
			if (_latestFractalPrice is decimal level && candle.ClosePrice > level)
			{
				HandleLongEntry();
				_latestFractalDirection = FractalDirection.None;
			}
		}
		else if (_latestFractalDirection == FractalDirection.Lower && fast < slow && macd < macdSignal)
		{
			if (_latestFractalPrice is decimal level && candle.ClosePrice < level)
			{
				HandleShortEntry();
				_latestFractalDirection = FractalDirection.None;
			}
		}
	}

	private void HandleLongEntry()
	{
		if (Security is null)
			return;

		var volume = AdjustVolume(OrderVolume);
		if (volume <= 0m)
			return;

		if (Position < 0m)
			ClosePosition();

		var maxExposure = AdjustVolume(OrderVolume * MaxTrades);
		if (maxExposure <= 0m)
			maxExposure = volume;

		if (Position + volume <= maxExposure + 1e-10m)
			BuyMarket(volume);
	}

	private void HandleShortEntry()
	{
		if (Security is null)
			return;

		var volume = AdjustVolume(OrderVolume);
		if (volume <= 0m)
			return;

		if (Position > 0m)
			ClosePosition();

		var maxExposure = AdjustVolume(OrderVolume * MaxTrades);
		if (maxExposure <= 0m)
			maxExposure = volume;

		if (-Position + volume <= maxExposure + 1e-10m)
			SellMarket(volume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (Security is null)
			return volume;

		var step = Security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Round(volume / step);
			volume = steps * step;
		}

		var minVolume = Security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = Security.MaxVolume;
		if (maxVolume != null && maxVolume.Value > 0m && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(decimal open, decimal high, decimal low, decimal close)
		{
			Open = open;
			High = high;
			Low = low;
			Close = close;
		}

		public decimal Open { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}

	private enum FractalDirection
	{
		None,
		Upper,
		Lower,
	}
}
