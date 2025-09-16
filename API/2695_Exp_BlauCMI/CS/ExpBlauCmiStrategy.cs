using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the Exp_BlauCMI MetaTrader strategy using the Blau Candle Momentum Index.
/// </summary>
public class ExpBlauCmiStrategy : Strategy
{
	/// <summary>
	/// Price sources supported by the strategy.
	/// </summary>
	public enum AppliedPrice
	{
		Close = 1,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
		Simple,
		Quarter,
		TrendFollow0,
		TrendFollow1,
		Demark
	}

	/// <summary>
	/// Smoothing modes used in the multi-stage averages.
	/// </summary>
	public enum SmoothingMethod
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<SmoothingMethod> _smoothingMethod;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _firstSmoothingLength;
	private readonly StrategyParam<int> _secondSmoothingLength;
	private readonly StrategyParam<int> _thirdSmoothingLength;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<AppliedPrice> _priceForClose;
	private readonly StrategyParam<AppliedPrice> _priceForOpen;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _orderVolume;

	private LengthIndicator<decimal> _momentumStage1 = null!;
	private LengthIndicator<decimal> _momentumStage2 = null!;
	private LengthIndicator<decimal> _momentumStage3 = null!;
	private LengthIndicator<decimal> _absStage1 = null!;
	private LengthIndicator<decimal> _absStage2 = null!;
	private LengthIndicator<decimal> _absStage3 = null!;

	private readonly Queue<decimal> _priceBuffer = new();
	private readonly List<decimal> _indicatorHistory = new();

	private decimal _priceStep;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpBlauCmiStrategy"/> class.
	/// </summary>
	public ExpBlauCmiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for BlauCMI calculations", "General");

		_smoothingMethod = Param(nameof(MomentumSmoothing), SmoothingMethod.Exponential)
			.SetDisplay("Smoothing Method", "Averaging mode for the BlauCMI stages", "Indicator");

		_momentumLength = Param(nameof(MomentumLength), 1)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Depth", "Bars between compared prices", "Indicator");

		_firstSmoothingLength = Param(nameof(FirstSmoothingLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("First Smoothing", "Length of the first BlauCMI smoothing", "Indicator");

		_secondSmoothingLength = Param(nameof(SecondSmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Second Smoothing", "Length of the second BlauCMI smoothing", "Indicator");

		_thirdSmoothingLength = Param(nameof(ThirdSmoothingLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Third Smoothing", "Length of the third BlauCMI smoothing", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Signal Shift", "Number of closed bars used for signals", "Trading");

		_priceForClose = Param(nameof(PriceForClose), AppliedPrice.Close)
			.SetDisplay("Momentum Price", "Price type for the leading leg", "Indicator");

		_priceForOpen = Param(nameof(PriceForOpen), AppliedPrice.Open)
			.SetDisplay("Reference Price", "Price type compared against the delayed bar", "Indicator");

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
			.SetDisplay("Allow Long Entry", "Enable opening long trades", "Trading");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
			.SetDisplay("Allow Short Entry", "Enable opening short trades", "Trading");

		_allowLongExit = Param(nameof(AllowLongExit), true)
			.SetDisplay("Allow Long Exit", "Enable closing long trades on opposite signals", "Trading");

		_allowShortExit = Param(nameof(AllowShortExit), true)
			.SetDisplay("Allow Short Exit", "Enable closing short trades on opposite signals", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetRange(0, 100000)
			.SetDisplay("Stop-Loss Points", "Distance to stop-loss in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetRange(0, 100000)
			.SetDisplay("Take-Profit Points", "Distance to take-profit in price steps", "Risk");

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Contract volume used for entries", "Trading");
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
	/// Averaging method for momentum smoothing stages.
	/// </summary>
	public SmoothingMethod MomentumSmoothing
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Bars between the compared prices when computing raw momentum.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Length of the first momentum smoothing stage.
	/// </summary>
	public int FirstSmoothingLength
	{
		get => _firstSmoothingLength.Value;
		set => _firstSmoothingLength.Value = value;
	}

	/// <summary>
	/// Length of the second momentum smoothing stage.
	/// </summary>
	public int SecondSmoothingLength
	{
		get => _secondSmoothingLength.Value;
		set => _secondSmoothingLength.Value = value;
	}

	/// <summary>
	/// Length of the third momentum smoothing stage.
	/// </summary>
	public int ThirdSmoothingLength
	{
		get => _thirdSmoothingLength.Value;
		set => _thirdSmoothingLength.Value = value;
	}

	/// <summary>
	/// Index of the closed bar that produces trading signals.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Applied price for the front leg of momentum.
	/// </summary>
	public AppliedPrice PriceForClose
	{
		get => _priceForClose.Value;
		set => _priceForClose.Value = value;
	}

	/// <summary>
	/// Applied price for the delayed leg of momentum.
	/// </summary>
	public AppliedPrice PriceForOpen
	{
		get => _priceForOpen.Value;
		set => _priceForOpen.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	/// <summary>
	/// Allow closing long positions when an opposite signal appears.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	/// <summary>
	/// Allow closing short positions when an opposite signal appears.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Order volume used for market entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
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
		_priceBuffer.Clear();
		_indicatorHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;
		_stopLossDistance = StopLossPoints > 0 ? StopLossPoints * _priceStep : 0m;
		_takeProfitDistance = TakeProfitPoints > 0 ? TakeProfitPoints * _priceStep : 0m;

		StartProtection(
			takeProfit: TakeProfitPoints > 0 ? new Unit(_takeProfitDistance, UnitTypes.Absolute) : null,
			stopLoss: StopLossPoints > 0 ? new Unit(_stopLossDistance, UnitTypes.Absolute) : null);

		Volume = Math.Abs(OrderVolume);

		_momentumStage1 = CreateMovingAverage(MomentumSmoothing, FirstSmoothingLength);
		_absStage1 = CreateMovingAverage(MomentumSmoothing, FirstSmoothingLength);
		_momentumStage2 = CreateMovingAverage(MomentumSmoothing, SecondSmoothingLength);
		_absStage2 = CreateMovingAverage(MomentumSmoothing, SecondSmoothingLength);
		_momentumStage3 = CreateMovingAverage(MomentumSmoothing, ThirdSmoothingLength);
		_absStage3 = CreateMovingAverage(MomentumSmoothing, ThirdSmoothingLength);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private LengthIndicator<decimal> CreateMovingAverage(SmoothingMethod method, int length)
	{
		var normalized = Math.Max(1, length);

		return method switch
		{
			SmoothingMethod.Simple => new SimpleMovingAverage { Length = normalized },
			SmoothingMethod.Smoothed => new SmoothedMovingAverage { Length = normalized },
			SmoothingMethod.LinearWeighted => new WeightedMovingAverage { Length = normalized },
			_ => new ExponentialMovingAverage { Length = normalized }
		};
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var frontPrice = GetAppliedPrice(candle, PriceForClose);
		var referencePrice = GetAppliedPrice(candle, PriceForOpen);

		var momentumDepth = Math.Max(1, MomentumLength);
		_priceBuffer.Enqueue(referencePrice);
		while (_priceBuffer.Count > momentumDepth)
			_priceBuffer.Dequeue();

		if (_priceBuffer.Count < momentumDepth)
			return;

		var delayedPrice = _priceBuffer.Peek();
		var momentum = frontPrice - delayedPrice;
		var absMomentum = Math.Abs(momentum);
		var time = candle.Time;

		var stage1 = _momentumStage1.Process(new DecimalIndicatorValue(_momentumStage1, momentum, time)).ToDecimal();
		var absStage1 = _absStage1.Process(new DecimalIndicatorValue(_absStage1, absMomentum, time)).ToDecimal();

		var stage2 = _momentumStage2.Process(new DecimalIndicatorValue(_momentumStage2, stage1, time)).ToDecimal();
		var absStage2 = _absStage2.Process(new DecimalIndicatorValue(_absStage2, absStage1, time)).ToDecimal();

		var stage3Value = _momentumStage3.Process(new DecimalIndicatorValue(_momentumStage3, stage2, time));
		var absStage3Value = _absStage3.Process(new DecimalIndicatorValue(_absStage3, absStage2, time));

		if (!stage3Value.IsFinal || !absStage3Value.IsFinal)
			return;

		var denominator = absStage3Value.ToDecimal();
		if (denominator == 0m)
			return;

		var cmi = 100m * stage3Value.ToDecimal() / denominator;

		_indicatorHistory.Add(cmi);
		var required = SignalBar + 3;
		if (_indicatorHistory.Count > required)
			_indicatorHistory.RemoveRange(0, _indicatorHistory.Count - required);

		var index = _indicatorHistory.Count - 1 - SignalBar;
		if (index < 2)
			return;

		var value0 = _indicatorHistory[index];
		var value1 = _indicatorHistory[index - 1];
		var value2 = _indicatorHistory[index - 2];

		var buySignal = value1 < value2 && value0 > value1;
		var sellSignal = value1 > value2 && value0 < value1;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0 && AllowLongExit && sellSignal)
		{
			SellMarket(Position);
		}

		if (Position < 0 && AllowShortExit && buySignal)
		{
			BuyMarket(Math.Abs(Position));
		}

		if (Position != 0)
			return;

		if (buySignal && AllowLongEntry)
		{
			BuyMarket(Volume);
		}
		else if (sellSignal && AllowShortEntry)
		{
			SellMarket(Volume);
		}
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrice price)
	{
		return price switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			AppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice
				? candle.HighPrice
				: candle.ClosePrice < candle.OpenPrice
					? candle.LowPrice
					: candle.ClosePrice,
			AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
				? (candle.HighPrice + candle.ClosePrice) / 2m
				: candle.ClosePrice < candle.OpenPrice
					? (candle.LowPrice + candle.ClosePrice) / 2m
					: candle.ClosePrice,
			AppliedPrice.Demark =>
				GetDemarkPrice(candle),
			_ => candle.ClosePrice
		};
	}

	private static decimal GetDemarkPrice(ICandleMessage candle)
	{
		var baseValue = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		if (candle.ClosePrice < candle.OpenPrice)
			baseValue = (baseValue + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
			baseValue = (baseValue + candle.HighPrice) / 2m;
		else
			baseValue = (baseValue + candle.ClosePrice) / 2m;

		return ((baseValue - candle.LowPrice) + (baseValue - candle.HighPrice)) / 2m;
	}
}
