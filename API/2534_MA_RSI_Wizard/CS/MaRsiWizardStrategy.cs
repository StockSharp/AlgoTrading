using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average plus RSI strategy converted from the MQL5 Wizard template.
/// The strategy computes weighted scores from a shifted moving average and RSI momentum.
/// </summary>
public class MaRsiWizardStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _thresholdOpen;
	private readonly StrategyParam<int> _thresholdClose;
	private readonly StrategyParam<decimal> _priceLevelPoints;
	private readonly StrategyParam<int> _stopLevelPoints;
	private readonly StrategyParam<int> _takeLevelPoints;
	private readonly StrategyParam<int> _expirationBars;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MaMethod> _maMethod;
	private readonly StrategyParam<AppliedPrice> _maAppliedPrice;
	private readonly StrategyParam<decimal> _maWeight;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<AppliedPrice> _rsiAppliedPrice;
	private readonly StrategyParam<decimal> _rsiWeight;

	private LengthIndicator<decimal> _ma = null!;
	private RelativeStrengthIndex _rsi = null!;
	private readonly Queue<decimal> _maShiftBuffer = new();

	private int _barIndex;
	private int? _lastLongEntryBar;
	private int? _lastShortEntryBar;

	/// <summary>
	/// Initializes a new instance of the <see cref="MaRsiWizardStrategy"/>.
	/// </summary>
	public MaRsiWizardStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for incoming candles", "General");

		_thresholdOpen = Param(nameof(ThresholdOpen), 55)
			.SetRange(0, 100)
			.SetDisplay("Open Threshold", "Weighted score required to open a position", "Signals")
			.SetCanOptimize(true);

		_thresholdClose = Param(nameof(ThresholdClose), 100)
			.SetRange(0, 100)
			.SetDisplay("Close Threshold", "Weighted score required to exit an existing position", "Signals")
			.SetCanOptimize(true);

		_priceLevelPoints = Param(nameof(PriceLevelPoints), 0m)
			.SetDisplay("Price Level (points)", "Minimum distance between price and moving average", "Signals")
			.SetCanOptimize(true);

		_stopLevelPoints = Param(nameof(StopLevelPoints), 50)
			.SetDisplay("Stop Loss (points)", "Protective stop distance expressed in price points", "Risk")
			.SetCanOptimize(true);

		_takeLevelPoints = Param(nameof(TakeLevelPoints), 50)
			.SetDisplay("Take Profit (points)", "Profit target distance expressed in price points", "Risk")
			.SetCanOptimize(true);

		_expirationBars = Param(nameof(ExpirationBars), 4)
			.SetDisplay("Signal Cooldown (bars)", "Bars to wait before allowing a new trade in the same direction", "Signals")
			.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Moving Average")
			.SetCanOptimize(true);

		_maShift = Param(nameof(MaShift), 3)
			.SetRange(0, 100)
			.SetDisplay("MA Shift", "Lag applied to the moving average output", "Moving Average")
			.SetCanOptimize(true);

		_maMethod = Param(nameof(MaMethod), MaMethod.Simple)
			.SetDisplay("MA Method", "Moving average calculation method", "Moving Average");

		_maAppliedPrice = Param(nameof(MaAppliedPrice), AppliedPrice.Close)
			.SetDisplay("MA Source", "Price type used for the moving average", "Moving Average");

		_maWeight = Param(nameof(MaWeight), 0.8m)
			.SetDisplay("MA Weight", "Contribution of the moving average score", "Signals")
			.SetRange(0m, 1m)
			.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation length", "RSI")
			.SetCanOptimize(true);

		_rsiAppliedPrice = Param(nameof(RsiAppliedPrice), AppliedPrice.Close)
			.SetDisplay("RSI Source", "Price type used for RSI", "RSI");

		_rsiWeight = Param(nameof(RsiWeight), 0.5m)
			.SetDisplay("RSI Weight", "Contribution of the RSI score", "Signals")
			.SetRange(0m, 1m)
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Type of candles used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Weighted score required to open a new position.
	/// </summary>
	public int ThresholdOpen
	{
		get => _thresholdOpen.Value;
		set => _thresholdOpen.Value = value;
	}

	/// <summary>
	/// Weighted score required to close the current position.
	/// </summary>
	public int ThresholdClose
	{
		get => _thresholdClose.Value;
		set => _thresholdClose.Value = value;
	}

	/// <summary>
	/// Minimum price distance from the moving average expressed in points.
	/// </summary>
	public decimal PriceLevelPoints
	{
		get => _priceLevelPoints.Value;
		set => _priceLevelPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in points.
	/// </summary>
	public int StopLevelPoints
	{
		get => _stopLevelPoints.Value;
		set => _stopLevelPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in points.
	/// </summary>
	public int TakeLevelPoints
	{
		get => _takeLevelPoints.Value;
		set => _takeLevelPoints.Value = value;
	}

	/// <summary>
	/// Cooldown measured in bars before a new trade in the same direction is allowed.
	/// </summary>
	public int ExpirationBars
	{
		get => _expirationBars.Value;
		set => _expirationBars.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Number of bars used to lag the moving average output.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average calculation method.
	/// </summary>
	public MaMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price source used for the moving average.
	/// </summary>
	public AppliedPrice MaAppliedPrice
	{
		get => _maAppliedPrice.Value;
		set => _maAppliedPrice.Value = value;
	}

	/// <summary>
	/// Contribution of the moving average score in the weighted decision.
	/// </summary>
	public decimal MaWeight
	{
		get => _maWeight.Value;
		set => _maWeight.Value = value;
	}

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Price source used for the RSI indicator.
	/// </summary>
	public AppliedPrice RsiAppliedPrice
	{
		get => _rsiAppliedPrice.Value;
		set => _rsiAppliedPrice.Value = value;
	}

	/// <summary>
	/// Contribution of the RSI score in the weighted decision.
	/// </summary>
	public decimal RsiWeight
	{
		get => _rsiWeight.Value;
		set => _rsiWeight.Value = value;
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

		_maShiftBuffer.Clear();
		_barIndex = 0;
		_lastLongEntryBar = null;
		_lastShortEntryBar = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_maShiftBuffer.Clear();
		_barIndex = 0;
		_lastLongEntryBar = null;
		_lastShortEntryBar = null;

		_ma = CreateMovingAverage(MaMethod, MaPeriod);
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var step = Security.PriceStep ?? 1m;

		Unit? takeProfit = TakeLevelPoints > 0
			? new Unit(TakeLevelPoints * step, UnitTypes.Point)
			: null;

		Unit? stopLoss = StopLevelPoints > 0
			? new Unit(StopLevelPoints * step, UnitTypes.Point)
			: null;

		StartProtection(
			takeProfit: takeProfit,
			stopLoss: stopLoss,
			useMarketOrders: true);

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, _ma);
			DrawOwnTrades(priceArea);
		}

		var rsiArea = CreateChartArea("RSI");
		if (rsiArea != null)
		{
			DrawIndicator(rsiArea, _rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_barIndex++;

		var maInput = SelectAppliedPrice(candle, MaAppliedPrice);
		var maValue = _ma.Process(new DecimalIndicatorValue(_ma, maInput, candle.OpenTime));
		if (!maValue.IsFinal || maValue is not DecimalIndicatorValue maResult)
			return;

		var rsiInput = SelectAppliedPrice(candle, RsiAppliedPrice);
		var rsiValue = _rsi.Process(new DecimalIndicatorValue(_rsi, rsiInput, candle.OpenTime));
		if (!rsiValue.IsFinal || rsiValue is not DecimalIndicatorValue rsiResult)
			return;

		var referenceMa = UpdateAndGetShiftedMa(maResult.Value);
		if (referenceMa == null)
			return;

		var currentPrice = candle.ClosePrice;
		var step = Security.PriceStep ?? 1m;
		var priceOffset = PriceLevelPoints * step;

		if (PriceLevelPoints > 0 && Math.Abs(currentPrice - referenceMa.Value) < priceOffset)
			return;

		var maLongSignal = currentPrice > referenceMa.Value ? 100m : 0m;
		var maShortSignal = currentPrice < referenceMa.Value ? 100m : 0m;

		var rsi = rsiResult.Value;
		var rsiLongSignal = rsi > 50m ? Math.Min(100m, (rsi - 50m) * 2m) : 0m;
		var rsiShortSignal = rsi < 50m ? Math.Min(100m, (50m - rsi) * 2m) : 0m;

		var weightSum = MaWeight + RsiWeight;
		if (weightSum <= 0m)
			return;

		var longScore = (MaWeight * maLongSignal + RsiWeight * rsiLongSignal) / weightSum;
		var shortScore = (MaWeight * maShortSignal + RsiWeight * rsiShortSignal) / weightSum;

		if (Position > 0 && shortScore >= ThresholdClose)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && longScore >= ThresholdClose)
		{
			BuyMarket(-Position);
		}

		var allowLong = ExpirationBars <= 0 || _lastLongEntryBar == null || _barIndex - _lastLongEntryBar >= ExpirationBars;
		var allowShort = ExpirationBars <= 0 || _lastShortEntryBar == null || _barIndex - _lastShortEntryBar >= ExpirationBars;

		if (Position <= 0 && longScore >= ThresholdOpen && allowLong)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0)
			{
				BuyMarket(volume);
				_lastLongEntryBar = _barIndex;
			}
			return;
		}

		if (Position >= 0 && shortScore >= ThresholdOpen && allowShort)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0)
			{
				SellMarket(volume);
				_lastShortEntryBar = _barIndex;
			}
		}
	}

	private decimal? UpdateAndGetShiftedMa(decimal maValue)
	{
		var shift = Math.Max(0, MaShift);
		if (shift == 0)
		{
			return maValue;
		}

		_maShiftBuffer.Enqueue(maValue);

		if (_maShiftBuffer.Count <= shift)
			return null;

		if (_maShiftBuffer.Count > shift + 1)
			_maShiftBuffer.Dequeue();

		return _maShiftBuffer.Count == shift + 1 ? _maShiftBuffer.Peek() : (decimal?)null;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MaMethod method, int period)
	{
		return method switch
		{
			MaMethod.Simple => new SimpleMovingAverage { Length = period },
			MaMethod.Exponential => new ExponentialMovingAverage { Length = period },
			MaMethod.Smoothed => new SmoothedMovingAverage { Length = period },
			MaMethod.LinearWeighted => new LinearWeightedMovingAverage { Length = period },
			_ => new SimpleMovingAverage { Length = period }
		};
	}

	private static decimal SelectAppliedPrice(ICandleMessage candle, AppliedPrice price)
	{
		return price switch
		{
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}
}

/// <summary>
/// Moving average calculation methods supported by the strategy.
/// </summary>
public enum MaMethod
{
	Simple,
	Exponential,
	Smoothed,
	LinearWeighted
}

/// <summary>
/// Price sources compatible with the indicators used in the strategy.
/// </summary>
public enum AppliedPrice
{
	Close,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted
}
