using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Expert RSI Stochastic MA strategy converted from MetaTrader 5 version.
/// Combines a moving average trend filter with RSI and Stochastic oscillators,
/// supports fixed loss protection and trailing exits driven by the Stochastic signal.
/// </summary>
public class ExpertRsiStochasticMaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<AppliedPriceType> _rsiPriceType;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<int> _stochSlowing;
	private readonly StrategyParam<decimal> _stochUpperLevel;
	private readonly StrategyParam<decimal> _stochLowerLevel;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<AppliedPriceType> _maPriceType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<decimal> _allowLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;

	private IIndicator _movingAverage = null!;
	private RelativeStrengthIndex _rsi = null!;
	private StochasticOscillator _stochastic = null!;
	private readonly List<decimal> _maHistory = new();
	private decimal _pipSize;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private Sides? _activeSide;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Default volume used for orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Source price applied to the RSI.
	/// </summary>
	public AppliedPriceType RsiPriceType
	{
		get => _rsiPriceType.Value;
		set => _rsiPriceType.Value = value;
	}

	/// <summary>
	/// Upper RSI threshold considered overbought.
	/// </summary>
	public decimal RsiUpperLevel
	{
		get => _rsiUpperLevel.Value;
		set => _rsiUpperLevel.Value = value;
	}

	/// <summary>
	/// Lower RSI threshold considered oversold.
	/// </summary>
	public decimal RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
	}

	/// <summary>
	/// Number of periods for the %K line.
	/// </summary>
	public int StochKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	/// <summary>
	/// Number of periods for the %D smoothing.
	/// </summary>
	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	/// <summary>
	/// Slowing factor applied to the %K line.
	/// </summary>
	public int StochSlowing
	{
		get => _stochSlowing.Value;
		set => _stochSlowing.Value = value;
	}

	/// <summary>
	/// Upper Stochastic level used for exit conditions.
	/// </summary>
	public decimal StochUpperLevel
	{
		get => _stochUpperLevel.Value;
		set => _stochUpperLevel.Value = value;
	}

	/// <summary>
	/// Lower Stochastic level used for entry and exit conditions.
	/// </summary>
	public decimal StochLowerLevel
	{
		get => _stochLowerLevel.Value;
		set => _stochLowerLevel.Value = value;
	}

	/// <summary>
	/// Type of moving average used for trend filtering.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Applied price for the moving average.
	/// </summary>
	public AppliedPriceType MaPriceType
	{
		get => _maPriceType.Value;
		set => _maPriceType.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Shift for the moving average value in bars.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Allowed loss size in points before closing a losing trade.
	/// </summary>
	public decimal AllowLossPoints
	{
		get => _allowLossPoints.Value;
		set => _allowLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ExpertRsiStochasticMaStrategy"/>.
	/// </summary>
	public ExpertRsiStochasticMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for calculations", "General");

		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Default volume for new positions", "Trading");

		_rsiPeriod = Param(nameof(RsiPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Number of bars for RSI", "RSI");

		_rsiPriceType = Param(nameof(RsiPriceType), AppliedPriceType.Close)
		.SetDisplay("RSI Price", "Applied price for RSI calculation", "RSI");

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 80m)
		.SetRange(0m, 100m)
		.SetDisplay("RSI Overbought", "Upper RSI threshold", "RSI");

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 20m)
		.SetRange(0m, 100m)
		.SetDisplay("RSI Oversold", "Lower RSI threshold", "RSI");

		_stochKPeriod = Param(nameof(StochKPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("%K Period", "Length of Stochastic %K", "Stochastic");

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("%D Period", "Length of Stochastic %D", "Stochastic");

		_stochSlowing = Param(nameof(StochSlowing), 3)
		.SetGreaterThanZero()
		.SetDisplay("Slowing", "Smoothing factor for %K", "Stochastic");

		_stochUpperLevel = Param(nameof(StochUpperLevel), 70m)
		.SetRange(0m, 100m)
		.SetDisplay("Stoch Overbought", "Upper Stochastic threshold", "Stochastic");

		_stochLowerLevel = Param(nameof(StochLowerLevel), 30m)
		.SetRange(0m, 100m)
		.SetDisplay("Stoch Oversold", "Lower Stochastic threshold", "Stochastic");

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Simple)
		.SetDisplay("MA Method", "Type of moving average", "Moving Average");

		_maPriceType = Param(nameof(MaPriceType), AppliedPriceType.Close)
		.SetDisplay("MA Price", "Applied price for moving average", "Moving Average");

		_maPeriod = Param(nameof(MaPeriod), 150)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Length of moving average", "Moving Average");

		_maShift = Param(nameof(MaShift), 0)
		.SetNotNegative()
		.SetDisplay("MA Shift", "Bars to shift the MA value", "Moving Average");

		_allowLossPoints = Param(nameof(AllowLossPoints), 30m)
		.SetNotNegative()
		.SetDisplay("Allow Loss", "Loss threshold in points", "Risk Management");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 30m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop", "Trailing distance in points", "Risk Management");
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

		_maHistory.Clear();
		_pipSize = 0m;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_activeSide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_pipSize = CalculatePipSize();

		_movingAverage = CreateMovingAverage(MaMethod, MaPeriod);
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_stochastic = new StochasticOscillator
		{
			KPeriod = StochKPeriod,
			DPeriod = StochDPeriod,
			Slowing = StochSlowing
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_stochastic, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _movingAverage);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var maInput = GetAppliedPrice(candle, MaPriceType);
		var maValue = _movingAverage.Process(maInput, candle.OpenTime, true).ToDecimal();
		_maHistory.Add(maValue);

		var maxHistory = Math.Max(MaPeriod + MaShift + 5, MaPeriod * 2);
		if (_maHistory.Count > maxHistory)
		_maHistory.RemoveRange(0, _maHistory.Count - maxHistory);

		var rsiInput = GetAppliedPrice(candle, RsiPriceType);
		var rsiValue = _rsi.Process(rsiInput, candle.OpenTime, true).ToDecimal();

		if (!stochValue.IsFinal)
		return;

		if (!_movingAverage.IsFormed || !_rsi.IsFormed || !_stochastic.IsFormed)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var maShifted = GetShiftedMaValue();
		if (maShifted is null)
		return;

		var stoch = (StochasticOscillatorValue)stochValue;
		var mainValue = stoch.K;
		var signalValue = stoch.D;

		ManagePositions(candle, maShifted.Value, rsiValue, mainValue, signalValue);
	}

	private void ManagePositions(ICandleMessage candle, decimal maValue, decimal rsiValue, decimal stochMain, decimal stochSignal)
	{
		var allowLossOffset = GetPriceOffset(AllowLossPoints);
		var trailingOffset = GetPriceOffset(TrailingStopPoints);

		if (Position > 0)
		{
			if (_activeSide != Sides.Buy)
			{
				_activeSide = Sides.Buy;
				_longTrailingStop = null;
				_shortTrailingStop = null;
			}

			var entryPrice = Position.AveragePrice;
			var unrealizedLoss = entryPrice - candle.ClosePrice;

			if (AllowLossPoints == 0m)
			{
				if (unrealizedLoss > 0m && stochMain > StochUpperLevel)
				{
					SellMarket(Position);
					_activeSide = null;
					_longTrailingStop = null;
					return;
				}
			}
			else if (unrealizedLoss >= allowLossOffset && stochMain > StochLowerLevel)
			{
				SellMarket(Position);
				_activeSide = null;
				_longTrailingStop = null;
				return;
			}

			if (TrailingStopPoints > 0m && candle.ClosePrice >= entryPrice && stochMain > StochUpperLevel)
			{
				var candidate = candle.ClosePrice - trailingOffset;
				_longTrailingStop = _longTrailingStop.HasValue
				? Math.Max(_longTrailingStop.Value, candidate)
				: candidate;
			}
			else if (TrailingStopPoints == 0m && stochMain > StochUpperLevel && candle.ClosePrice >= entryPrice)
			{
				SellMarket(Position);
				_activeSide = null;
				_longTrailingStop = null;
				return;
			}

			if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
			{
				SellMarket(Position);
				_activeSide = null;
				_longTrailingStop = null;
				return;
			}
		}
		else if (Position < 0)
		{
			if (_activeSide != Sides.Sell)
			{
				_activeSide = Sides.Sell;
				_shortTrailingStop = null;
				_longTrailingStop = null;
			}

			var entryPrice = Position.AveragePrice;
			var unrealizedLoss = candle.ClosePrice - entryPrice;

			if (AllowLossPoints == 0m)
			{
				if (unrealizedLoss > 0m && stochMain < StochLowerLevel)
				{
					BuyMarket(Math.Abs(Position));
					_activeSide = null;
					_shortTrailingStop = null;
					return;
				}
			}
			else if (unrealizedLoss >= allowLossOffset && stochMain < StochUpperLevel)
			{
				BuyMarket(Math.Abs(Position));
				_activeSide = null;
				_shortTrailingStop = null;
				return;
			}

			if (TrailingStopPoints > 0m && candle.ClosePrice <= entryPrice && stochMain < StochLowerLevel)
			{
				var candidate = candle.ClosePrice + trailingOffset;
				_shortTrailingStop = _shortTrailingStop.HasValue
				? Math.Min(_shortTrailingStop.Value, candidate)
				: candidate;
			}
			else if (TrailingStopPoints == 0m && stochMain < StochLowerLevel && candle.ClosePrice <= entryPrice)
			{
				BuyMarket(Math.Abs(Position));
				_activeSide = null;
				_shortTrailingStop = null;
				return;
			}

			if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				_activeSide = null;
				_shortTrailingStop = null;
				return;
			}
		}
		else
		{
			_activeSide = null;
			_longTrailingStop = null;
			_shortTrailingStop = null;
		}

		if (Position <= 0 && candle.ClosePrice > maValue && rsiValue < RsiLowerLevel && stochMain < StochLowerLevel && stochSignal < StochLowerLevel)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_activeSide = Sides.Buy;
			_longTrailingStop = null;
			_shortTrailingStop = null;
			return;
		}

		if (Position >= 0 && candle.ClosePrice < maValue && rsiValue > RsiUpperLevel && stochMain > StochUpperLevel && stochSignal > StochUpperLevel)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_activeSide = Sides.Sell;
			_longTrailingStop = null;
			_shortTrailingStop = null;
		}
	}

	private decimal? GetShiftedMaValue()
	{
		var index = _maHistory.Count - 1 - MaShift;
		return index >= 0 ? _maHistory[index] : null;
	}

	private decimal GetPriceOffset(decimal points)
	{
		if (points <= 0m)
		return 0m;

		return points * (_pipSize > 0m ? _pipSize : 1m);
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return 1m;

		var decimals = Security?.Decimals ?? 0;
		return decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}

	private static IIndicator CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceType type)
	{
		return type switch
		{
			AppliedPriceType.Close => candle.ClosePrice,
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}
}

/// <summary>
/// Applied price options matching MetaTrader style enumerations.
/// </summary>
public enum AppliedPriceType
{
	Close,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted
}

/// <summary>
/// Moving average calculation methods supported by the strategy.
/// </summary>
public enum MovingAverageMethod
{
	Simple,
	Exponential,
	Smoothed,
	Weighted
}
