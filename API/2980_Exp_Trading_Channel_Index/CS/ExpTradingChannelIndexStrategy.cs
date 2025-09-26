
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that replicates the Exp Trading Channel Index Expert Advisor from MQL5.
/// It monitors the Trading Channel Index indicator and reacts to color changes on a configurable historical bar.
/// </summary>
public class ExpTradingChannelIndexStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<bool> _buyPositionOpen;
	private readonly StrategyParam<bool> _sellPositionOpen;
	private readonly StrategyParam<bool> _buyPositionClose;
	private readonly StrategyParam<bool> _sellPositionClose;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _highLevel;
	private readonly StrategyParam<int> _lowLevel;
	private readonly StrategyParam<SmoothMethod> _method1;
	private readonly StrategyParam<SmoothMethod> _method2;
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<int> _phase1;
	private readonly StrategyParam<int> _phase2;
	private readonly StrategyParam<decimal> _coefficient;
	private readonly StrategyParam<AppliedPrice> _appliedPrice;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<int> _colorHistory = new();
	private decimal? _longStopLoss;
	private decimal? _longTakeProfit;
	private decimal? _shortStopLoss;
	private decimal? _shortTakeProfit;
	private decimal _priceStep;
	private TradingChannelIndexIndicator _indicator = null!;

	/// <summary>
	/// Quantity used for each new position.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables long entries.
	/// </summary>
	public bool BuyPositionOpen
	{
		get => _buyPositionOpen.Value;
		set => _buyPositionOpen.Value = value;
	}

	/// <summary>
	/// Enables short entries.
	/// </summary>
	public bool SellPositionOpen
	{
		get => _sellPositionOpen.Value;
		set => _sellPositionOpen.Value = value;
	}

	/// <summary>
	/// Enables long exits triggered by indicator signals.
	/// </summary>
	public bool BuyPositionClose
	{
		get => _buyPositionClose.Value;
		set => _buyPositionClose.Value = value;
	}

	/// <summary>
	/// Enables short exits triggered by indicator signals.
	/// </summary>
	public bool SellPositionClose
	{
		get => _sellPositionClose.Value;
		set => _sellPositionClose.Value = value;
	}

	/// <summary>
	/// Number of bars back used to evaluate color transitions.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Upper threshold for the Trading Channel Index color map.
	/// </summary>
	public int HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold for the Trading Channel Index color map.
	/// </summary>
	public int LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Primary smoothing method.
	/// </summary>
	public SmoothMethod Method1
	{
		get => _method1.Value;
		set => _method1.Value = value;
	}

	/// <summary>
	/// Secondary smoothing method.
	/// </summary>
	public SmoothMethod Method2
	{
		get => _method2.Value;
		set => _method2.Value = value;
	}

	/// <summary>
	/// Length of the primary moving average.
	/// </summary>
	public int Length1
	{
		get => _length1.Value;
		set => _length1.Value = value;
	}

	/// <summary>
	/// Length of the secondary moving average.
	/// </summary>
	public int Length2
	{
		get => _length2.Value;
		set => _length2.Value = value;
	}

	/// <summary>
	/// Phase setting used by specific smoothing methods.
	/// </summary>
	public int Phase1
	{
		get => _phase1.Value;
		set => _phase1.Value = value;
	}

	/// <summary>
	/// Phase applied to the secondary smoothing method.
	/// </summary>
	public int Phase2
	{
		get => _phase2.Value;
		set => _phase2.Value = value;
	}

	/// <summary>
	/// Normalization coefficient of the Trading Channel Index.
	/// </summary>
	public decimal Coefficient
	{
		get => _coefficient.Value;
		set => _coefficient.Value = value;
	}

	/// <summary>
	/// Price source used for indicator calculations.
	/// </summary>
	public AppliedPrice AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Candle series used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpTradingChannelIndexStrategy"/> class.
	/// </summary>
	public ExpTradingChannelIndexStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Quantity used for each order", "Orders")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 5m, 0.5m);
		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetDisplay("Stop Loss (pts)", "Stop loss in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(100, 2000, 100);
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetDisplay("Take Profit (pts)", "Take profit in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(200, 4000, 100);
		_buyPositionOpen = Param(nameof(BuyPositionOpen), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Signals");
		_sellPositionOpen = Param(nameof(SellPositionOpen), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Signals");
		_buyPositionClose = Param(nameof(BuyPositionClose), true)
			.SetDisplay("Enable Long Exits", "Allow closing long positions via indicator", "Signals");
		_sellPositionClose = Param(nameof(SellPositionClose), true)
			.SetDisplay("Enable Short Exits", "Allow closing short positions via indicator", "Signals");
		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Number of bars back for color lookup", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);
		_highLevel = Param(nameof(HighLevel), 50)
			.SetDisplay("High Level", "Upper level for color coding", "Indicator");
		_lowLevel = Param(nameof(LowLevel), -50)
			.SetDisplay("Low Level", "Lower level for color coding", "Indicator");
		_method1 = Param(nameof(Method1), SmoothMethod.Simple)
			.SetDisplay("Primary Method", "Smoothing method for the first average", "Indicator");
		_method2 = Param(nameof(Method2), SmoothMethod.Simple)
			.SetDisplay("Secondary Method", "Smoothing method for the second average", "Indicator");
		_length1 = Param(nameof(Length1), 60)
			.SetDisplay("Length #1", "Period of the primary moving average", "Indicator")
			.SetGreaterThanZero();
		_length2 = Param(nameof(Length2), 30)
			.SetDisplay("Length #2", "Period of the secondary moving average", "Indicator")
			.SetGreaterThanZero();
		_phase1 = Param(nameof(Phase1), 15)
			.SetDisplay("Phase #1", "Phase parameter for the primary smoothing", "Indicator");
		_phase2 = Param(nameof(Phase2), 100)
			.SetDisplay("Phase #2", "Phase parameter for the secondary smoothing", "Indicator");
		_coefficient = Param(nameof(Coefficient), 0.015m)
			.SetDisplay("Coefficient", "Normalization factor", "Indicator")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(0.005m, 0.05m, 0.005m);
		_appliedPrice = Param(nameof(AppliedPrice), AppliedPrice.Close)
			.SetDisplay("Applied Price", "Price source for calculations", "Indicator");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used by the strategy", "General");
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
		_colorHistory.Clear();
		ClearLongProtection();
		ClearShortProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;

		_indicator = new TradingChannelIndexIndicator
		{
			Method1 = Method1,
			Length1 = Length1,
			Phase1 = Phase1,
			Method2 = Method2,
			Length2 = Length2,
			Phase2 = Phase2,
			Coefficient = Coefficient,
			HighLevel = HighLevel,
			LowLevel = LowLevel,
			AppliedPrice = AppliedPrice,
			PriceStep = _priceStep
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_indicator, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ProcessProtectiveOrders(candle);

		if (indicatorValue is not TradingChannelIndexValue tciValue)
			return;

		if (tciValue.ColorIndex is not int color || tciValue.Value is not decimal)
			return;

		_colorHistory.Add(color);
		var maxHistory = Math.Max(5, SignalBar + 3);
		if (_colorHistory.Count > maxHistory)
			_colorHistory.RemoveAt(0);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var targetIndex = _colorHistory.Count - 1 - SignalBar;
		if (targetIndex <= 0)
			return;

		var col0 = _colorHistory[targetIndex];
		var col1 = _colorHistory[targetIndex - 1];

		var shouldCloseShort = SellPositionClose && col1 == 0;
		var shouldOpenBuy = BuyPositionOpen && col1 == 0 && col0 != 0;
		var shouldCloseLong = BuyPositionClose && col1 == 4;
		var shouldOpenSell = SellPositionOpen && col1 == 4 && col0 != 4;

		if (shouldCloseShort && Position < 0)
		{
			var volume = Math.Abs(Position);
			if (volume > 0)
			{
				BuyMarket(volume);
				ClearShortProtection();
			}
		}

		if (shouldCloseLong && Position > 0)
		{
			var volume = Position;
			if (volume > 0)
			{
				SellMarket(volume);
				ClearLongProtection();
			}
		}

		if (shouldOpenBuy && Position <= 0 && TradeVolume > 0)
		{
			if (Position < 0)
			{
				var covering = Math.Abs(Position);
				if (covering > 0)
				{
					BuyMarket(covering);
					ClearShortProtection();
				}
			}

			BuyMarket(TradeVolume);
			SetLongProtection(candle.ClosePrice);
		}

		if (shouldOpenSell && Position >= 0 && TradeVolume > 0)
		{
			if (Position > 0)
			{
				var covering = Position;
				if (covering > 0)
				{
					SellMarket(covering);
					ClearLongProtection();
				}
			}

			SellMarket(TradeVolume);
			SetShortProtection(candle.ClosePrice);
		}
	}

	private void ProcessProtectiveOrders(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var exit = false;
			if (_longStopLoss.HasValue && candle.LowPrice <= _longStopLoss.Value)
				exit = true;
			else if (_longTakeProfit.HasValue && candle.HighPrice >= _longTakeProfit.Value)
				exit = true;

			if (exit)
			{
				var volume = Position;
				if (volume > 0)
				{
					SellMarket(volume);
					ClearLongProtection();
				}
			}
		}
		else if (_longStopLoss.HasValue || _longTakeProfit.HasValue)
		{
			ClearLongProtection();
		}

		if (Position < 0)
		{
			var exit = false;
			if (_shortStopLoss.HasValue && candle.HighPrice >= _shortStopLoss.Value)
				exit = true;
			else if (_shortTakeProfit.HasValue && candle.LowPrice <= _shortTakeProfit.Value)
				exit = true;

			if (exit)
			{
				var volume = Math.Abs(Position);
				if (volume > 0)
				{
					BuyMarket(volume);
					ClearShortProtection();
				}
			}
		}
		else if (_shortStopLoss.HasValue || _shortTakeProfit.HasValue)
		{
			ClearShortProtection();
		}
	}

	private void SetLongProtection(decimal fallbackPrice)
	{
		var entryPrice = Position > 0 ? PositionAvgPrice : fallbackPrice;
		_longStopLoss = StopLossPoints > 0 ? entryPrice - StopLossPoints * _priceStep : null;
		_longTakeProfit = TakeProfitPoints > 0 ? entryPrice + TakeProfitPoints * _priceStep : null;
	}

	private void SetShortProtection(decimal fallbackPrice)
	{
		var entryPrice = Position < 0 ? PositionAvgPrice : fallbackPrice;
		_shortStopLoss = StopLossPoints > 0 ? entryPrice + StopLossPoints * _priceStep : null;
		_shortTakeProfit = TakeProfitPoints > 0 ? entryPrice - TakeProfitPoints * _priceStep : null;
	}

	private void ClearLongProtection()
	{
		_longStopLoss = null;
		_longTakeProfit = null;
	}

	private void ClearShortProtection()
	{
		_shortStopLoss = null;
		_shortTakeProfit = null;
	}
}

/// <summary>
/// Indicator computing the Trading Channel Index values and color codes.
/// </summary>
public class TradingChannelIndexIndicator : BaseIndicator<decimal>
{
	private IIndicator _primaryMa;
	private IIndicator _volatilityMa;
	private IIndicator _signalMa;

	/// <summary>
	/// Primary smoothing method.
	/// </summary>
	public SmoothMethod Method1 { get; set; } = SmoothMethod.Simple;

	/// <summary>
	/// Primary moving average length.
	/// </summary>
	public int Length1 { get; set; } = 60;

	/// <summary>
	/// Phase value for the primary method.
	/// </summary>
	public int Phase1 { get; set; } = 15;

	/// <summary>
	/// Secondary smoothing method.
	/// </summary>
	public SmoothMethod Method2 { get; set; } = SmoothMethod.Simple;

	/// <summary>
	/// Secondary moving average length.
	/// </summary>
	public int Length2 { get; set; } = 30;

	/// <summary>
	/// Phase value for the secondary method.
	/// </summary>
	public int Phase2 { get; set; } = 100;

	/// <summary>
	/// Normalization coefficient.
	/// </summary>
	public decimal Coefficient { get; set; } = 0.015m;

	/// <summary>
	/// Upper threshold used to assign bullish colors.
	/// </summary>
	public int HighLevel { get; set; } = 50;

	/// <summary>
	/// Lower threshold used to assign bearish colors.
	/// </summary>
	public int LowLevel { get; set; } = -50;

	/// <summary>
	/// Applied price selection.
	/// </summary>
	public AppliedPrice AppliedPrice { get; set; } = AppliedPrice.Close;

	/// <summary>
	/// Price step used to avoid division by zero.
	/// </summary>
	public decimal PriceStep { get; set; } = 1m;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new TradingChannelIndexValue(this, input, null, null);

		_primaryMa ??= CreateMovingAverage(Method1, Length1, Phase1);
		_volatilityMa ??= CreateMovingAverage(Method1, Length1, Phase1);
		_signalMa ??= CreateMovingAverage(Method2, Length2, Phase2);

		var price = GetAppliedPrice(candle);
		var maValue = _primaryMa.Process(new DecimalIndicatorValue(_primaryMa, price, input.Time));
		if (!_primaryMa.IsFormed)
			return new TradingChannelIndexValue(this, input, null, null);

		var smoothedPrice = maValue.ToDecimal();
		var diff = Math.Abs(price - smoothedPrice);
		if (diff < PriceStep)
			diff = PriceStep;

		_volatilityMa.Process(new DecimalIndicatorValue(_volatilityMa, diff, input.Time));

		if (Coefficient <= 0)
			return new TradingChannelIndexValue(this, input, null, null);

		var normalized = (price - smoothedPrice) / (diff * Coefficient);
		var tciValue = _signalMa.Process(new DecimalIndicatorValue(_signalMa, normalized, input.Time));
		if (!_signalMa.IsFormed)
			return new TradingChannelIndexValue(this, input, null, null);

		var tci = tciValue.ToDecimal();
		var color = 2;
		if (tci > HighLevel)
			color = 0;
		else if (tci > 0)
			color = 1;
		else if (tci < LowLevel)
			color = 4;
		else if (tci < 0)
			color = 3;

		return new TradingChannelIndexValue(this, input, tci, color);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_primaryMa = null;
		_volatilityMa = null;
		_signalMa = null;
	}

	private static IIndicator CreateMovingAverage(SmoothMethod method, int length, int phase)
	{
		return method switch
		{
			SmoothMethod.Simple => new SimpleMovingAverage { Length = length },
			SmoothMethod.Exponential => new ExponentialMovingAverage { Length = length },
			SmoothMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			SmoothMethod.Weighted => new WeightedMovingAverage { Length = length },
			SmoothMethod.Jurik => new JurikMovingAverage { Length = length, Phase = phase },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			AppliedPrice.Weighted => (candle.ClosePrice * 2m + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrice.TrendFollow => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
			AppliedPrice.TrendFollowAverage => candle.ClosePrice > candle.OpenPrice
				? (candle.HighPrice + candle.ClosePrice) / 2m
				: candle.ClosePrice < candle.OpenPrice
					? (candle.LowPrice + candle.ClosePrice) / 2m
					: candle.ClosePrice,
			AppliedPrice.Demark => CalculateDemarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}

	private static decimal CalculateDemarkPrice(ICandleMessage candle)
	{
		var sum = candle.HighPrice + candle.LowPrice + candle.ClosePrice;
		if (candle.ClosePrice < candle.OpenPrice)
			sum = (sum + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
			sum = (sum + candle.HighPrice) / 2m;
		else
			sum = (sum + candle.ClosePrice) / 2m;

		return ((sum - candle.LowPrice) + (sum - candle.HighPrice)) / 2m;
	}
}

/// <summary>
/// Indicator value that exposes both Trading Channel Index and color index.
/// </summary>
public class TradingChannelIndexValue : ComplexIndicatorValue
{
	public TradingChannelIndexValue(IIndicator indicator, IIndicatorValue input, decimal? value, int? colorIndex)
		: base(indicator, input, (nameof(Value), value), (nameof(ColorIndex), colorIndex))
	{
	}

	/// <summary>
	/// Trading Channel Index value.
	/// </summary>
	public decimal? Value => (decimal?)GetValue(nameof(Value));

	/// <summary>
	/// Color index used by the original indicator (0..4).
	/// </summary>
	public int? ColorIndex => (int?)GetValue(nameof(ColorIndex));
}
