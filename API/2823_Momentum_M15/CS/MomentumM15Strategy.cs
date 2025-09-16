using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum based strategy converted from the MetaTrader 5 "Momentum-M15" expert advisor.
/// </summary>
public class MomentumM15Strategy : Strategy
{
	private readonly StrategyParam<decimal> _volumeParam;
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _maPeriodParam;
	private readonly StrategyParam<int> _maShiftParam;
	private readonly StrategyParam<MovingAverageMethod> _maMethodParam;
	private readonly StrategyParam<CandlePrice> _maPriceParam;
	private readonly StrategyParam<int> _momentumPeriodParam;
	private readonly StrategyParam<CandlePrice> _momentumPriceParam;
	private readonly StrategyParam<decimal> _momentumThresholdParam;
	private readonly StrategyParam<decimal> _momentumShiftParam;
	private readonly StrategyParam<int> _momentumOpenLengthParam;
	private readonly StrategyParam<int> _momentumCloseLengthParam;
	private readonly StrategyParam<int> _gapLevelParam;
	private readonly StrategyParam<int> _gapTimeoutParam;
	private readonly StrategyParam<decimal> _trailingStopParam;

	private IIndicator _ma = null!;
	private Momentum _momentum = null!;
	private readonly List<decimal> _maHistory = new();
	private readonly List<decimal> _momentumHistory = new();
	private decimal? _previousClose;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private int _gapTimer;

	/// <summary>
	/// Initializes a new instance of <see cref="MomentumM15Strategy"/>.
	/// </summary>
	public MomentumM15Strategy()
	{
		_volumeParam = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Default order volume", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.05m);

		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for calculations", "Common");

		_maPeriodParam = Param(nameof(MaPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average lookback length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_maShiftParam = Param(nameof(MaShift), 8)
			.SetGreaterOrEqualZero()
			.SetDisplay("MA Shift", "Horizontal shift applied to moving average", "Indicators");

		_maMethodParam = Param(nameof(MaMethod), MovingAverageMethod.Smoothed)
			.SetDisplay("MA Method", "Type of moving average", "Indicators");

		_maPriceParam = Param(nameof(MaPrice), CandlePrice.Low)
			.SetDisplay("MA Price", "Price source for moving average", "Indicators");

		_momentumPeriodParam = Param(nameof(MomentumPeriod), 23)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum indicator lookback", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 1);

		_momentumPriceParam = Param(nameof(MomentumPrice), CandlePrice.Open)
			.SetDisplay("Momentum Price", "Price source for momentum", "Indicators");

		_momentumThresholdParam = Param(nameof(MomentumThreshold), 100m)
			.SetDisplay("Momentum Threshold", "Baseline momentum threshold", "Trading Rules");

		_momentumShiftParam = Param(nameof(MomentumShift), -0.2m)
			.SetDisplay("Momentum Shift", "Shift applied to momentum threshold", "Trading Rules");

		_momentumOpenLengthParam = Param(nameof(MomentumOpenLength), 6)
			.SetGreaterOrEqualZero()
			.SetDisplay("Momentum Open Length", "Bars required for monotonic momentum on entries", "Trading Rules");

		_momentumCloseLengthParam = Param(nameof(MomentumCloseLength), 10)
			.SetGreaterOrEqualZero()
			.SetDisplay("Momentum Close Length", "Bars required for monotonic momentum on exits", "Trading Rules");

		_gapLevelParam = Param(nameof(GapLevel), 30)
			.SetGreaterOrEqualZero()
			.SetDisplay("Gap Level", "Minimum gap in price steps to pause trading", "Risk Management");

		_gapTimeoutParam = Param(nameof(GapTimeout), 100)
			.SetGreaterOrEqualZero()
			.SetDisplay("Gap Timeout", "Number of bars to skip after a large gap", "Risk Management");

		_trailingStopParam = Param(nameof(TrailingStop), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance in price steps", "Risk Management");
	}

	/// <summary>
	/// Default trade volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _volumeParam.Value;
		set => _volumeParam.Value = value;
	}

	/// <summary>
	/// Candle type for the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriodParam.Value;
		set => _maPeriodParam.Value = value;
	}

	/// <summary>
	/// Number of bars to shift the moving average.
	/// </summary>
	public int MaShift
	{
		get => _maShiftParam.Value;
		set => _maShiftParam.Value = value;
	}

	/// <summary>
	/// Moving average calculation method.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethodParam.Value;
		set => _maMethodParam.Value = value;
	}

	/// <summary>
	/// Price source for the moving average.
	/// </summary>
	public CandlePrice MaPrice
	{
		get => _maPriceParam.Value;
		set => _maPriceParam.Value = value;
	}

	/// <summary>
	/// Momentum indicator lookback period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriodParam.Value;
		set => _momentumPeriodParam.Value = value;
	}

	/// <summary>
	/// Price source for the momentum indicator.
	/// </summary>
	public CandlePrice MomentumPrice
	{
		get => _momentumPriceParam.Value;
		set => _momentumPriceParam.Value = value;
	}

	/// <summary>
	/// Baseline momentum threshold.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThresholdParam.Value;
		set => _momentumThresholdParam.Value = value;
	}

	/// <summary>
	/// Shift applied to the momentum threshold.
	/// </summary>
	public decimal MomentumShift
	{
		get => _momentumShiftParam.Value;
		set => _momentumShiftParam.Value = value;
	}

	/// <summary>
	/// Sequence length for entry momentum validation.
	/// </summary>
	public int MomentumOpenLength
	{
		get => _momentumOpenLengthParam.Value;
		set => _momentumOpenLengthParam.Value = value;
	}

	/// <summary>
	/// Sequence length for exit momentum validation.
	/// </summary>
	public int MomentumCloseLength
	{
		get => _momentumCloseLengthParam.Value;
		set => _momentumCloseLengthParam.Value = value;
	}

	/// <summary>
	/// Minimum gap (in price steps) that suspends new entries.
	/// </summary>
	public int GapLevel
	{
		get => _gapLevelParam.Value;
		set => _gapLevelParam.Value = value;
	}

	/// <summary>
	/// Number of bars to wait after a gap before trading resumes.
	/// </summary>
	public int GapTimeout
	{
		get => _gapTimeoutParam.Value;
		set => _gapTimeoutParam.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStopParam.Value;
		set => _trailingStopParam.Value = value;
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

		_ma = null!;
		_momentum = null!;
		_maHistory.Clear();
		_momentumHistory.Clear();
		_previousClose = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_gapTimer = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = CreateMovingAverage(MaMethod, MaPeriod);
		_momentum = new Momentum { Length = MomentumPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawIndicator(area, _momentum);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_ma is null || _momentum is null)
		return;

		var maValue = ProcessMovingAverage(candle);
		var momentumValue = ProcessMomentum(candle);

		if (maValue is null || momentumValue is null)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var previousClose = _previousClose;
		_previousClose = candle.ClosePrice;

		if (previousClose is null)
		return;

		HandleGapFilter(previousClose.Value, candle.OpenPrice);

		if (_gapTimer > 0)
		{
			_gapTimer--;
			if (_gapTimer > 0)
			return;
		}

		if (Position == 0)
		{
			TryOpenPositions(previousClose.Value, candle.OpenPrice, maValue.Value, momentumValue.Value);
		}
		else
		{
			ManageExistingPosition(previousClose.Value, candle, maValue.Value, momentumValue.Value);
		}
	}

	private decimal? ProcessMovingAverage(ICandleMessage candle)
	{
		var price = GetPrice(candle, MaPrice);
		var value = _ma.Process(price, candle.OpenTime, true);

		if (!value.IsFinal)
		return null;

		var ma = value.ToDecimal();
		_maHistory.Add(ma);

		var maxCount = MaShift + 1;
		while (_maHistory.Count > maxCount)
		_maHistory.RemoveAt(0);

		var index = _maHistory.Count - 1 - MaShift;
		if (index < 0 || index >= _maHistory.Count)
		return null;

		return _maHistory[index];
	}

	private decimal? ProcessMomentum(ICandleMessage candle)
	{
		var price = GetPrice(candle, MomentumPrice);
		var value = _momentum.Process(price, candle.OpenTime, true);

		if (!value.IsFinal)
		return null;

		var momentum = value.ToDecimal();
		_momentumHistory.Add(momentum);

		var maxLen = Math.Max(Math.Max(MomentumOpenLength, MomentumCloseLength), 1);
		while (_momentumHistory.Count > maxLen)
		_momentumHistory.RemoveAt(0);

		return momentum;
	}

	private void HandleGapFilter(decimal previousClose, decimal currentOpen)
	{
		var priceStep = Security.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return;

		var gap = (currentOpen - previousClose) / priceStep;
		if (gap > GapLevel)
		_gapTimer = GapTimeout;
	}

	private void TryOpenPositions(decimal previousClose, decimal currentOpen, decimal maValue, decimal momentumValue)
	{
		var longMomentumOk = MomentumOpenLength > 0 && IsMomentumDownSequence(MomentumOpenLength);
		var shortMomentumOk = MomentumOpenLength > 0 && IsMomentumUpSequence(MomentumOpenLength);

		var longCondition = momentumValue < MomentumThreshold + MomentumShift
		&& previousClose < maValue
		&& currentOpen < maValue
		&& longMomentumOk;

		var shortCondition = momentumValue > MomentumThreshold - MomentumShift
		&& previousClose > maValue
		&& currentOpen > maValue
		&& shortMomentumOk;

		if (longCondition)
		{
			BuyMarket(TradeVolume);
			_longTrailingStop = null;
			_shortTrailingStop = null;
		}
		else if (shortCondition)
		{
			SellMarket(TradeVolume);
			_longTrailingStop = null;
			_shortTrailingStop = null;
		}
	}

	private void ManageExistingPosition(decimal previousClose, ICandleMessage candle, decimal maValue, decimal momentumValue)
	{
		if (Position > 0)
		{
			var exitMomentum = MomentumCloseLength > 0 && IsMomentumDownSequence(MomentumCloseLength);
			var shouldClose = exitMomentum || previousClose < maValue;

			if (shouldClose)
			{
				SellMarket(Position);
				_longTrailingStop = null;
				return;
			}

			UpdateLongTrailingStop(candle);
		}
		else if (Position < 0)
		{
			var exitMomentum = MomentumCloseLength > 0 && IsMomentumUpSequence(MomentumCloseLength);
			var shouldClose = exitMomentum || previousClose > maValue;

			if (shouldClose)
			{
				BuyMarket(Math.Abs(Position));
				_shortTrailingStop = null;
				return;
			}

			UpdateShortTrailingStop(candle);
		}
	}

	private void UpdateLongTrailingStop(ICandleMessage candle)
	{
		if (TrailingStop <= 0m)
		return;

		var priceStep = Security.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return;

		var distance = TrailingStop * priceStep;
		var candidate = candle.LowPrice - distance;

		if (_longTrailingStop is null || candidate > _longTrailingStop)
		_longTrailingStop = candidate;

		if (_longTrailingStop is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(Position);
			_longTrailingStop = null;
		}
	}

	private void UpdateShortTrailingStop(ICandleMessage candle)
	{
		if (TrailingStop <= 0m)
		return;

		var priceStep = Security.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return;

		var distance = TrailingStop * priceStep;
		var candidate = candle.HighPrice + distance;

		if (_shortTrailingStop is null || candidate < _shortTrailingStop)
		_shortTrailingStop = candidate;

		if (_shortTrailingStop is decimal stop && candle.HighPrice >= stop)
		{
			BuyMarket(Math.Abs(Position));
			_shortTrailingStop = null;
		}
	}

	private bool IsMomentumDownSequence(int length)
	{
		if (length <= 0 || _momentumHistory.Count < length)
		return false;

		var start = _momentumHistory.Count - length;
		var previous = _momentumHistory[start];

		for (var i = start + 1; i < _momentumHistory.Count; i++)
		{
			var current = _momentumHistory[i];
			if (current > previous)
			return false;

			previous = current;
		}

		return true;
	}

	private bool IsMomentumUpSequence(int length)
	{
		if (length <= 0 || _momentumHistory.Count < length)
		return false;

		var start = _momentumHistory.Count - length;
		var previous = _momentumHistory[start];

		for (var i = start + 1; i < _momentumHistory.Count; i++)
		{
			var current = _momentumHistory[i];
			if (current < previous)
			return false;

			previous = current;
		}

		return true;
	}

	private static decimal GetPrice(ICandleMessage candle, CandlePrice price)
	{
		return price switch
		{
			CandlePrice.Open => candle.OpenPrice,
			CandlePrice.High => candle.HighPrice,
			CandlePrice.Low => candle.LowPrice,
			CandlePrice.Close => candle.ClosePrice,
			CandlePrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			CandlePrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			CandlePrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private static IIndicator CreateMovingAverage(MovingAverageMethod method, int period)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = period },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = period },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = period },
			MovingAverageMethod.Weighted => new WeightedMovingAverage { Length = period },
			_ => new SimpleMovingAverage { Length = period },
		};
	}
}

/// <summary>
/// Moving average method options aligned with the original expert advisor inputs.
/// </summary>
public enum MovingAverageMethod
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	Simple,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Exponential,

	/// <summary>
	/// Smoothed moving average.
	/// </summary>
	Smoothed,

	/// <summary>
	/// Weighted moving average.
	/// </summary>
	Weighted
}
