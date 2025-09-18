using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Logging;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum crossing its own moving average strategy converted from MetaTrader 5 (MA on Momentum Min Profit.mq5).
/// </summary>
public class MaOnMomentumMinProfitStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<int> _momentumMaPeriod;
	private readonly StrategyParam<MomentumMovingAverageType> _momentumMaType;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<bool> _useCurrentCandle;
	private readonly StrategyParam<decimal> _stopLossMoney;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _momentumReference;

	private Momentum _momentumIndicator = null!;
	private LengthIndicator<decimal> _momentumAverage = null!;
	private decimal? _previousMomentum;
	private decimal? _previousAverage;
	private DateTimeOffset? _lastSignalBar;

	/// <summary>
	/// Candle type used for signal calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Momentum period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Moving average period applied to momentum values.
	/// </summary>
	public int MomentumMovingAveragePeriod
	{
		get => _momentumMaPeriod.Value;
		set => _momentumMaPeriod.Value = value;
	}

	/// <summary>
	/// Moving average calculation mode applied to momentum.
	/// </summary>
	public MomentumMovingAverageType MomentumMovingAverageType
	{
		get => _momentumMaType.Value;
		set => _momentumMaType.Value = value;
	}

	/// <summary>
	/// Reverse trading signals.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Close the opposite exposure before opening a new trade.
	/// </summary>
	public bool CloseOpposite
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Allow only one net position to remain open.
	/// </summary>
	public bool OnlyOnePosition
	{
		get => _onlyOnePosition.Value;
		set => _onlyOnePosition.Value = value;
	}

	/// <summary>
	/// Use the current forming candle instead of waiting for the close.
	/// </summary>
	public bool UseCurrentCandle
	{
		get => _useCurrentCandle.Value;
		set => _useCurrentCandle.Value = value;
	}

	/// <summary>
	/// Portfolio level stop-loss in money.
	/// </summary>
	public decimal StopLossMoney
	{
		get => _stopLossMoney.Value;
		set => _stopLossMoney.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points (multiplied by <see cref="Security.PriceStep"/>).
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Momentum baseline that splits bullish and bearish zones (100.0 in MetaTrader).
	/// </summary>
	public decimal MomentumReference
	{
		get => _momentumReference.Value;
		set => _momentumReference.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="MaOnMomentumMinProfitStrategy"/>.
	/// </summary>
	public MaOnMomentumMinProfitStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for the momentum calculation", "General");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Lookback for the momentum indicator", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 5);

		_momentumMaPeriod = Param(nameof(MomentumMovingAveragePeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Momentum MA", "Period of the moving average applied to momentum", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(3, 30, 3);

		_momentumMaType = Param(nameof(MomentumMovingAverageType), MomentumMovingAverageType.Smoothed)
			.SetDisplay("MA Type", "Moving-average algorithm applied to momentum", "Momentum");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert MetaTrader buy/sell rules", "Trading Rules");

		_closeOpposite = Param(nameof(CloseOpposite), true)
			.SetDisplay("Close Opposite", "Flatten the opposite exposure before entering", "Trading Rules");

		_onlyOnePosition = Param(nameof(OnlyOnePosition), true)
			.SetDisplay("Only One", "Keep a single net position", "Trading Rules");

		_useCurrentCandle = Param(nameof(UseCurrentCandle), false)
			.SetDisplay("Use Current Candle", "React on the forming candle instead of the closed one", "Trading Rules");

		_stopLossMoney = Param(nameof(StopLossMoney), 15m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss $", "Maximum permitted drawdown in account currency", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 460m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit Points", "Distance from entry to close trades in profit", "Risk");

		_momentumReference = Param(nameof(MomentumReference), 100m)
			.SetDisplay("Momentum Reference", "Neutral momentum level copied from MetaTrader", "Momentum");
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

		_previousMomentum = null;
		_previousAverage = null;
		_lastSignalBar = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_momentumIndicator = new Momentum
		{
			Length = MomentumPeriod
		};

		_momentumAverage = CreateMovingAverage(MomentumMovingAverageType, MomentumMovingAveragePeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_momentumIndicator, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _momentumIndicator);
			DrawIndicator(area, _momentumAverage);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue momentumValue)
	{
		if (!UseCurrentCandle && candle.State != CandleStates.Finished)
			return;

		if (!momentumValue.IsFinal)
			return;

		var momentum = momentumValue.GetValue<decimal>();
		var averageValue = _momentumAverage.Process(new DecimalIndicatorValue(_momentumAverage, momentum, candle.OpenTime));
		if (!averageValue.IsFinal)
			return;

		var average = averageValue.GetValue<decimal>();

		if (_previousMomentum is null || _previousAverage is null)
		{
			_previousMomentum = momentum;
			_previousAverage = average;
			return;
		}

		var previousMomentum = _previousMomentum.Value;
		var previousAverage = _previousAverage.Value;
		var crossedAbove = previousMomentum < previousAverage && momentum > average;
		var crossedBelow = previousMomentum > previousAverage && momentum < average;

		if ((crossedAbove || crossedBelow) && (!_lastSignalBar.HasValue || _lastSignalBar.Value != candle.OpenTime))
		{
			var reference = MomentumReference;
			if (crossedAbove && previousMomentum < reference)
			{
				HandleSignal(Sides.Buy, candle);
			}
			else if (crossedBelow && previousMomentum > reference)
			{
				HandleSignal(Sides.Sell, candle);
			}
		}

		_previousMomentum = momentum;
		_previousAverage = average;

		CheckRiskGuards(candle);
	}

	private void HandleSignal(Sides originalSide, ICandleMessage candle)
	{
		var finalSide = ReverseSignals ? (originalSide == Sides.Buy ? Sides.Sell : Sides.Buy) : originalSide;
		var position = Position;

		if (OnlyOnePosition && position != 0 && Math.Sign(position) == (finalSide == Sides.Buy ? 1 : -1))
		{
			return;
		}

		if (!CloseOpposite)
		{
			if (finalSide == Sides.Buy && position < 0)
				return;

			if (finalSide == Sides.Sell && position > 0)
				return;
		}

		var volume = Volume;
		if (volume <= 0m)
			return;

		if (CloseOpposite)
		{
			if (finalSide == Sides.Buy && position < 0)
			{
				volume += Math.Abs(position);
			}
			else if (finalSide == Sides.Sell && position > 0)
			{
				volume += Math.Abs(position);
			}
		}

		if (finalSide == Sides.Buy)
		{
			if (OnlyOnePosition && position > 0)
				return;

			LogInfo($"Buy signal at {candle.OpenTime:O}. Momentum crossed above its average.");
			BuyMarket(volume);
		}
		else
		{
			if (OnlyOnePosition && position < 0)
				return;

			LogInfo($"Sell signal at {candle.OpenTime:O}. Momentum crossed below its average.");
			SellMarket(volume);
		}

		_lastSignalBar = candle.OpenTime;
	}

	private void CheckRiskGuards(ICandleMessage candle)
	{
		if (StopLossMoney > 0m)
		{
			var price = candle.ClosePrice;
			var unrealized = Position != 0m ? Position * (price - PositionPrice) : 0m;
			var totalPnL = PnL + unrealized;

			if (totalPnL <= -StopLossMoney)
			{
				LogInfo($"Equity stop triggered. Total PnL {totalPnL:F2} <= {-StopLossMoney:F2}.");
				CloseAll();
				_lastSignalBar = candle.OpenTime;
			}
		}

		if (TakeProfitPoints > 0m && Security?.PriceStep > 0m)
		{
			var distance = TakeProfitPoints * Security.PriceStep.Value;
			if (distance > 0m && Position != 0m && PositionPrice != 0m)
			{
				if (Position > 0m && candle.HighPrice >= PositionPrice + distance)
				{
					LogInfo($"Long take-profit hit at {candle.HighPrice:F4}.");
					SellMarket(Math.Abs(Position));
				}
				else if (Position < 0m && candle.LowPrice <= PositionPrice - distance)
				{
					LogInfo($"Short take-profit hit at {candle.LowPrice:F4}.");
					BuyMarket(Math.Abs(Position));
				}
			}
		}
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MomentumMovingAverageType type, int length)
	{
		return type switch
		{
			MomentumMovingAverageType.Simple => new SimpleMovingAverage { Length = length },
			MomentumMovingAverageType.Exponential => new ExponentialMovingAverage { Length = length },
			MomentumMovingAverageType.Smoothed => new SmoothedMovingAverage { Length = length },
			MomentumMovingAverageType.Weighted => new WeightedMovingAverage { Length = length },
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}

	/// <summary>
	/// Moving-average calculation modes available for momentum smoothing.
	/// </summary>
	public enum MomentumMovingAverageType
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}
}
