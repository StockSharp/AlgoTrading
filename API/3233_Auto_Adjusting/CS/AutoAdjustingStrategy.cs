using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Auto Adjusting strategy converted from the MQL4 expert "Aouto Adjusting1".
/// Combines a three-EMA stack, higher timeframe momentum, and a macro MACD filter
/// to trade pullbacks in the trend with risk-based position sizing.
/// </summary>
public class AutoAdjustingStrategy : Strategy
{
	private static readonly DataType DefaultMacroType = TimeSpan.FromDays(30).TimeFrame();

	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _middleEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _momentumSamples;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _momentumCandleType;
	private readonly StrategyParam<DataType> _macroCandleType;
	private readonly StrategyParam<decimal> _padAmount;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _rewardRatio;
	private readonly StrategyParam<int> _candlesBack;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<decimal> _tradeVolume;

	private ExponentialMovingAverage _emaFast = null!;
	private ExponentialMovingAverage _emaMiddle = null!;
	private ExponentialMovingAverage _emaSlow = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly Queue<CandleSnapshot> _recentCandles = new();
	private readonly Queue<decimal> _momentumHistory = new();

	private decimal? _macdMain;
	private decimal? _macdSignal;
	private decimal _pipSize;

	/// <summary>
	/// Constructor.
	/// </summary>
	public AutoAdjustingStrategy()
	{
		var defaultFrame = TimeSpan.FromHours(1);
		var defaultMomentum = GetDefaultMomentumFrame(defaultFrame);

		_fastEmaLength = Param(nameof(FastEmaLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Length of the fast EMA in the stack", "Signals");

		_middleEmaLength = Param(nameof(MiddleEmaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Middle EMA", "Length of the middle EMA in the stack", "Signals");

		_slowEmaLength = Param(nameof(SlowEmaLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Length of the slow EMA in the stack", "Signals");

		_momentumLength = Param(nameof(MomentumLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Lookback for the momentum indicator", "Signals");

		_momentumSamples = Param(nameof(MomentumSamples), 3)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Samples", "Number of higher timeframe momentum values required", "Signals");

		_candleType = Param(nameof(CandleType), defaultFrame.TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for the EMA stack", "General");

		_momentumCandleType = Param(nameof(MomentumCandleType), defaultMomentum.TimeFrame())
			.SetDisplay("Momentum Timeframe", "Higher timeframe feeding the momentum filter", "General");

		_macroCandleType = Param(nameof(MacroMacdCandleType), DefaultMacroType)
			.SetDisplay("Macro MACD Timeframe", "Timeframe used for the monthly MACD filter", "General");

		_padAmount = Param(nameof(PadAmount), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Pad (pips)", "Extra pips added beyond the swing extreme for stop placement", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 0.1m)
			.SetDisplay("Risk %", "Portfolio percentage risked per trade", "Risk");

		_rewardRatio = Param(nameof(RewardRatio), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Reward Ratio", "Take-profit distance relative to risk distance", "Risk");

		_candlesBack = Param(nameof(CandlesBack), 3)
			.SetGreaterThanZero()
			.SetDisplay("Swing Lookback", "Number of candles used to detect swing highs and lows", "Signals");

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Momentum Buy", "Minimum deviation above 100 required for long setups", "Signals");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Momentum Sell", "Minimum deviation above 100 required for short setups", "Signals");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Fallback order volume when risk sizing is unavailable", "Risk");
	}

	/// <summary>
	/// Primary candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the fast EMA in the stack.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Length of the middle EMA in the stack.
	/// </summary>
	public int MiddleEmaLength
	{
		get => _middleEmaLength.Value;
		set => _middleEmaLength.Value = value;
	}

	/// <summary>
	/// Length of the slow EMA in the stack.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Lookback for the momentum indicator.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Number of higher timeframe momentum values required.
	/// </summary>
	public int MomentumSamples
	{
		get => _momentumSamples.Value;
		set => _momentumSamples.Value = value;
	}

	/// <summary>
	/// Higher timeframe for the momentum filter.
	/// </summary>
	public DataType MomentumCandleType
	{
		get => _momentumCandleType.Value;
		set => _momentumCandleType.Value = value;
	}

	/// <summary>
	/// Timeframe delivering the macro MACD confirmation.
	/// </summary>
	public DataType MacroMacdCandleType
	{
		get => _macroCandleType.Value;
		set => _macroCandleType.Value = value;
	}

	/// <summary>
	/// Extra pips beyond the swing low/high for stop placement.
	/// </summary>
	public decimal PadAmount
	{
		get => _padAmount.Value;
		set => _padAmount.Value = value;
	}

	/// <summary>
	/// Portfolio share risked per trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Take-profit distance relative to stop distance.
	/// </summary>
	public decimal RewardRatio
	{
		get => _rewardRatio.Value;
		set => _rewardRatio.Value = value;
	}

	/// <summary>
	/// Number of candles used for swing detection.
	/// </summary>
	public int CandlesBack
	{
		get => _candlesBack.Value;
		set => _candlesBack.Value = value;
	}

	/// <summary>
	/// Momentum deviation required for long setups.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Momentum deviation required for short setups.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Base volume used when risk-based sizing cannot be evaluated.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var pair in base.GetWorkingSecurities())
		yield return pair;

		if (Security != null)
		{
			yield return (Security, CandleType);
			yield return (Security, MomentumCandleType);
			yield return (Security, MacroMacdCandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_recentCandles.Clear();
		_momentumHistory.Clear();
		_macdMain = null;
		_macdSignal = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaFast = new ExponentialMovingAverage { Length = FastEmaLength };
		_emaMiddle = new ExponentialMovingAverage { Length = MiddleEmaLength };
		_emaSlow = new ExponentialMovingAverage { Length = SlowEmaLength };
		_momentum = new Momentum { Length = MomentumLength };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		UpdatePipSize();

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(ProcessMainCandle)
			.Start();

		var momentumSubscription = SubscribeCandles(MomentumCandleType);
		momentumSubscription
			.Bind(ProcessMomentum)
			.Start();

		var macroSubscription = SubscribeCandles(MacroMacdCandleType);
		macroSubscription
			.BindEx(_macd, ProcessMacroMacd)
			.Start();

		Volume = TradeVolume;
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var fastValue = _emaFast.Process(median, candle.OpenTime, true);
		var middleValue = _emaMiddle.Process(median, candle.OpenTime, true);
		var slowValue = _emaSlow.Process(median, candle.OpenTime, true);

		if (!fastValue.IsFinal || !middleValue.IsFinal || !slowValue.IsFinal)
		return;

		var fast = fastValue.ToDecimal();
		var middle = middleValue.ToDecimal();
		var slow = slowValue.ToDecimal();

		_recentCandles.Enqueue(new CandleSnapshot(candle.HighPrice, candle.LowPrice, candle.ClosePrice));

		var required = Math.Max(CandlesBack + 2, MomentumSamples);
		while (_recentCandles.Count > required)
		_recentCandles.Dequeue();

		if (_recentCandles.Count < required)
		return;

		var candles = _recentCandles.ToArray();
		var last = candles[^1];
		var prev1 = candles[^2];
		var prev2 = candles[^3];

		var pad = ConvertPipsToPrice(PadAmount);
		var swingSlice = candles.Skip(Math.Max(0, candles.Length - CandlesBack));
		var lowestLow = swingSlice.Min(c => c.Low);
		var highestHigh = swingSlice.Max(c => c.High);

		UpdateExistingPosition(last.Close, lowestLow, highestHigh, pad);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_momentumHistory.Count < MomentumSamples || _macdMain is null || _macdSignal is null)
		return;

		var hasBuyMomentum = _momentumHistory.Any(v => v >= MomentumBuyThreshold);
		var hasSellMomentum = _momentumHistory.Any(v => v >= MomentumSellThreshold);

		var macdBullish = _macdMain > _macdSignal;
		var macdBearish = _macdMain < _macdSignal;

		var buyStack = fast < middle && middle < slow;
		var sellStack = fast > middle && middle > slow;

		var buyStructure = prev1.Low <= middle && prev2.Low < prev1.High;
		var sellStructure = prev1.High >= middle && prev1.Low < prev2.High;

		if (buyStack && buyStructure && hasBuyMomentum && macdBullish && Position <= 0m)
		{
			var stopPrice = lowestLow - pad;
			var stopDistance = last.Close - stopPrice;
			if (stopDistance > 0m)
			{
				var takeDistance = stopDistance * RewardRatio;
				EnterLong(last.Close, stopDistance, takeDistance);
			}
		}
		else if (sellStack && sellStructure && hasSellMomentum && macdBearish && Position >= 0m)
		{
			var stopPrice = highestHigh + pad;
			var stopDistance = stopPrice - last.Close;
			if (stopDistance > 0m)
			{
				var takeDistance = stopDistance * RewardRatio;
				EnterShort(last.Close, stopDistance, takeDistance);
			}
		}
	}

	private void ProcessMomentum(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var value = _momentum.Process(candle.ClosePrice, candle.OpenTime, true);
		if (!value.IsFinal)
		return;

		var deviation = Math.Abs(value.ToDecimal() - 100m);

		_momentumHistory.Enqueue(deviation);
		while (_momentumHistory.Count > MomentumSamples)
		_momentumHistory.Dequeue();
	}

	private void ProcessMacroMacd(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (value is not MovingAverageConvergenceDivergenceSignalValue macdValue)
		return;

		if (macdValue.Macd is decimal macd && macdValue.Signal is decimal signal)
		{
			_macdMain = macd;
			_macdSignal = signal;
		}
	}

	private void EnterLong(decimal referencePrice, decimal stopDistance, decimal takeDistance)
	{
		var volumeToBuy = GetVolumeForRisk(stopDistance);
		if (Position < 0m)
		volumeToBuy += Math.Abs(Position);

		if (volumeToBuy <= 0m)
		return;

		BuyMarket(volumeToBuy);

		var resultingPosition = Position + volumeToBuy;
		ApplyRiskControls(referencePrice, stopDistance, takeDistance, resultingPosition);
	}

	private void EnterShort(decimal referencePrice, decimal stopDistance, decimal takeDistance)
	{
		var volumeToSell = GetVolumeForRisk(stopDistance);
		if (Position > 0m)
		volumeToSell += Math.Abs(Position);

		if (volumeToSell <= 0m)
		return;

		SellMarket(volumeToSell);

		var resultingPosition = Position - volumeToSell;
		ApplyRiskControls(referencePrice, stopDistance, takeDistance, resultingPosition);
	}

	private void ApplyRiskControls(decimal referencePrice, decimal stopDistance, decimal takeDistance, decimal resultingPosition)
	{
		if (stopDistance > 0m)
		SetStopLoss(stopDistance, referencePrice, resultingPosition);

		if (takeDistance > 0m)
		SetTakeProfit(takeDistance, referencePrice, resultingPosition);
	}

	private void UpdateExistingPosition(decimal referencePrice, decimal lowestLow, decimal highestHigh, decimal pad)
	{
		if (Position > 0m)
		{
			var stopPrice = lowestLow - pad;
			var stopDistance = referencePrice - stopPrice;
			if (stopDistance > 0m)
			{
				var takeDistance = stopDistance * RewardRatio;
				ApplyRiskControls(referencePrice, stopDistance, takeDistance, Position);
			}
		}
		else if (Position < 0m)
		{
			var stopPrice = highestHigh + pad;
			var stopDistance = stopPrice - referencePrice;
			if (stopDistance > 0m)
			{
				var takeDistance = stopDistance * RewardRatio;
				ApplyRiskControls(referencePrice, stopDistance, takeDistance, Position);
			}
		}
	}

	private decimal GetVolumeForRisk(decimal stopDistance)
	{
		var baseVolume = TradeVolume;

		if (RiskPercent <= 0m || stopDistance <= 0m || Portfolio == null || Portfolio.CurrentValue <= 0m)
		return baseVolume;

		var priceStep = Security?.PriceStep ?? _pipSize;
		var stepPrice = Security?.StepPrice;
		if (priceStep <= 0m || stepPrice is null || stepPrice <= 0m)
		return baseVolume;

		var stepsToStop = stopDistance / priceStep;
		var lossPerUnit = stepsToStop * stepPrice.Value;
		if (lossPerUnit <= 0m)
		return baseVolume;

		var riskAmount = Portfolio.CurrentValue * RiskPercent / 100m;
		var volumeByRisk = riskAmount / lossPerUnit;

		return volumeByRisk > 0m ? volumeByRisk : baseVolume;
	}

	private decimal ConvertPipsToPrice(decimal pips)
	{
		if (pips <= 0m)
		return 0m;

		if (_pipSize <= 0m)
		UpdatePipSize();

		return pips * _pipSize;
	}

	private void UpdatePipSize()
	{
		var step = Security?.PriceStep;
		if (step is null || step <= 0m)
		{
			_pipSize = 0.0001m;
			return;
		}

		var value = step.Value;
		if (value == 0.00001m || value == 0.01m)
		value *= 10m;

		_pipSize = value;
	}

	private static TimeSpan GetDefaultMomentumFrame(TimeSpan baseFrame)
	{
		return baseFrame.TotalMinutes switch
		{
			1 => TimeSpan.FromMinutes(15),
			5 => TimeSpan.FromMinutes(30),
			15 => TimeSpan.FromHours(1),
			30 => TimeSpan.FromHours(4),
			60 => TimeSpan.FromDays(1),
			240 => TimeSpan.FromDays(7),
			1440 => TimeSpan.FromDays(30),
			_ => baseFrame
		};
	}

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(decimal high, decimal low, decimal close)
		{
			High = high;
			Low = low;
			Close = close;
		}

		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}
}
