using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 55-period moving average comparison between two bars.
/// </summary>
public class FiftyFiveMaBarComparisonStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _differenceThreshold;
	private readonly StrategyParam<int> _barA;
	private readonly StrategyParam<int> _barB;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<AppliedPriceType> _appliedPrice;

	private LengthIndicator<decimal>? _movingAverage;
	private decimal[] _maBuffer = Array.Empty<decimal>();
	private int _bufferCount;
	private decimal _pipSize;

	public FiftyFiveMaBarComparisonStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for MA calculations.", "General");

		_stopLossPips = Param(nameof(StopLossPips), 30)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance expressed in pips.", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetDisplay("Take Profit (pips)", "Take profit distance expressed in pips.", "Risk")
			.SetCanOptimize(true);

		_startHour = Param(nameof(StartHour), 8)
			.SetDisplay("Start Hour", "Hour (inclusive) when trading window opens.", "Session");

		_endHour = Param(nameof(EndHour), 21)
			.SetDisplay("End Hour", "Hour (exclusive) when trading window closes.", "Session");

		_differenceThreshold = Param(nameof(DifferenceThreshold), 0.0001m)
			.SetDisplay("MA Difference", "Required difference between MA values.", "Logic")
			.SetCanOptimize(true);

		_barA = Param(nameof(BarA), 0)
			.SetDisplay("Bar A", "Index of the first bar for MA comparison.", "Logic");

		_barB = Param(nameof(BarB), 1)
			.SetDisplay("Bar B", "Index of the second bar for MA comparison.", "Logic");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert generated trade direction.", "Logic");

		_closeOpposite = Param(nameof(CloseOppositePositions), false)
			.SetDisplay("Close Opposite", "Close existing positions in the opposite direction.", "Risk");

		_maShift = Param(nameof(MaShift), 0)
			.SetDisplay("MA Shift", "Horizontal shift applied to the moving average.", "Indicator");

		_maLength = Param(nameof(MaLength), 55)
			.SetDisplay("MA Length", "Number of periods for the moving average.", "Indicator");

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Exponential)
			.SetDisplay("MA Method", "Smoothing method for the moving average.", "Indicator");

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceType.Median)
			.SetDisplay("Applied Price", "Price type used as MA input.", "Indicator");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	public decimal DifferenceThreshold
	{
		get => _differenceThreshold.Value;
		set => _differenceThreshold.Value = value;
	}

	public int BarA
	{
		get => _barA.Value;
		set => _barA.Value = value;
	}

	public int BarB
	{
		get => _barB.Value;
		set => _barB.Value = value;
	}

	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	public bool CloseOppositePositions
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	public AppliedPriceType AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
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

		_movingAverage = null;
		_maBuffer = Array.Empty<decimal>();
		_bufferCount = 0;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (MaLength <= 0)
			throw new InvalidOperationException("MA length must be greater than zero.");

		_movingAverage = CreateMovingAverage(MaMethod, MaLength);
		_maBuffer = new decimal[Math.Max(CalculateBufferCapacity(), 1)];
		_bufferCount = 0;
		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _movingAverage);
			DrawOwnTrades(area);
		}

		var stopLoss = StopLossPips > 0 ? new Unit(StopLossPips * _pipSize, UnitTypes.Point) : null;
		var takeProfit = TakeProfitPips > 0 ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Point) : null;

		if (stopLoss != null || takeProfit != null)
			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_movingAverage == null || _maBuffer.Length == 0)
			return;

		var hour = candle.OpenTime.Hour;
		if (hour < StartHour || hour >= EndHour)
			return;

		var price = GetAppliedPrice(candle);
		var indicatorValue = _movingAverage.Process(new DecimalIndicatorValue(_movingAverage, price, candle.OpenTime));

		if (!indicatorValue.IsFinal)
			return;

		PushMaValue(indicatorValue.GetValue<decimal>());

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var indexA = BarA + MaShift;
		var indexB = BarB + MaShift;

		if (indexA < 0 || indexB < 0)
			return;

		var maxIndex = Math.Max(indexA, indexB);
		if (_bufferCount <= maxIndex)
			return;

		var maA = _maBuffer[indexA];
		var maB = _maBuffer[indexB];

		var isBuy = false;
		if (maA > maB + DifferenceThreshold)
		{
			isBuy = !ReverseSignals;
		}
		else if (maA < maB - DifferenceThreshold)
		{
			isBuy = ReverseSignals;
		}

		ExecuteOrder(isBuy);
	}

	private void ExecuteOrder(bool isBuy)
	{
		var volume = Volume;
		if (volume <= 0m)
			return;

		if (CloseOppositePositions)
		{
			if (isBuy && Position < 0m)
				volume += Math.Abs(Position);
			else if (!isBuy && Position > 0m)
				volume += Math.Abs(Position);
		}

		if (isBuy)
			BuyMarket(volume);
		else
			SellMarket(volume);
	}

	private void PushMaValue(decimal value)
	{
		var lastIndex = Math.Min(_bufferCount, _maBuffer.Length - 1);
		for (var i = lastIndex; i > 0; i--)
			_maBuffer[i] = _maBuffer[i - 1];

		_maBuffer[0] = value;

		if (_bufferCount < _maBuffer.Length)
			_bufferCount++;
	}

	private int CalculateBufferCapacity()
	{
		var shift = Math.Max(MaShift, 0);
		var maxIndex = Math.Max(BarA, BarB) + shift;
		return maxIndex + 1;
	}

	private decimal CalculatePipSize()
	{
		var step = Security.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var value = step;
		var digits = 0;
		while (value > 0m && value < 1m && digits < 8)
		{
			value *= 10m;
			digits++;
		}

		var adjust = digits == 3 || digits == 5 ? 10m : 1m;
		return step * adjust;
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			AppliedPriceType.Close => candle.ClosePrice,
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.Weighted => new WeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}

	public enum MovingAverageMethod
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
	}

	public enum AppliedPriceType
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
	}
}
