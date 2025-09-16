using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that reacts to large single-bar moves expecting a mean reversion.
/// </summary>
public class InverseReactionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _slippagePoints;
	private readonly StrategyParam<decimal> _minCriteriaPoints;
	private readonly StrategyParam<decimal> _maxCriteriaPoints;
	private readonly StrategyParam<decimal> _coefficient;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private InverseReactionIndicator _inverseReaction;
	private bool _previousSignal;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Stop-loss distance in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trade volume used for orders.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Allowed slippage in points (for information purposes).
	/// </summary>
	public decimal SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Minimum bar size in points required to trigger a signal.
	/// </summary>
	public decimal MinCriteriaPoints
	{
		get => _minCriteriaPoints.Value;
		set => _minCriteriaPoints.Value = value;
	}

	/// <summary>
	/// Maximum bar size in points allowed for a valid signal.
	/// </summary>
	public decimal MaxCriteriaPoints
	{
		get => _maxCriteriaPoints.Value;
		set => _maxCriteriaPoints.Value = value;
	}

	/// <summary>
	/// Confidence coefficient applied to the dynamic threshold.
	/// </summary>
	public decimal Coefficient
	{
		get => _coefficient.Value;
		set => _coefficient.Value = value;
	}

	/// <summary>
	/// Moving average period used inside the indicator.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle type that the strategy processes.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="InverseReactionStrategy"/>.
	/// </summary>
	public InverseReactionStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 250m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take-profit distance in points", "Risk")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume", "Risk");

		_slippagePoints = Param(nameof(SlippagePoints), 3m)
			.SetGreaterThanOrEqualToZero()
			.SetDisplay("Slippage", "Allowed slippage in points", "Risk");

		_minCriteriaPoints = Param(nameof(MinCriteriaPoints), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Bar Size", "Lower bound for candle size", "Signal")
			.SetCanOptimize(true);

		_maxCriteriaPoints = Param(nameof(MaxCriteriaPoints), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Maximum Bar Size", "Upper bound for candle size", "Signal")
			.SetCanOptimize(true);

		_coefficient = Param(nameof(Coefficient), 1.618m)
			.SetGreaterThanZero()
			.SetDisplay("Coefficient", "Confidence coefficient", "Signal")
			.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average length", "Signal")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");
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

		_inverseReaction = default;
		_previousSignal = false;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (MaPeriod < 3)
			throw new InvalidOperationException("MaPeriod must be at least 3.");

		if (MaxCriteriaPoints <= MinCriteriaPoints)
			throw new InvalidOperationException("MaxCriteriaPoints must be greater than MinCriteriaPoints.");

		StartProtection();

		Volume = TradeVolume;

		_inverseReaction = new InverseReactionIndicator
		{
			Period = MaPeriod,
			Coefficient = Coefficient
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_inverseReaction, ProcessCandle).Start();

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

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		ManageOpenPosition(candle);

		if (indicatorValue is not InverseReactionValue irValue || !indicatorValue.IsFinal || !irValue.Indicator.IsFormed)
		{
			_previousSignal = false;
			return;
		}

		var minThreshold = MinCriteriaPoints * step;
		var maxThreshold = MaxCriteriaPoints * step;
		if (maxThreshold <= minThreshold)
		{
			_previousSignal = false;
			return;
		}

		var absChange = Math.Abs(irValue.Change);
		var hasSignal = absChange > irValue.DynamicThreshold && absChange > minThreshold && absChange < maxThreshold;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousSignal = hasSignal;
			return;
		}

		var isNewSignal = hasSignal && !_previousSignal && Position == 0m;

		if (isNewSignal)
		{
			var volume = TradeVolume > 0m ? TradeVolume : Volume;
			if (volume > 0m)
			{
				var entryPrice = candle.ClosePrice;
				if (irValue.Change < 0m)
				{
					BuyMarket(volume);
					SetTargets(entryPrice, step, true);
				}
				else
				{
					SellMarket(volume);
					SetTargets(entryPrice, step, false);
				}
			}
		}

		_previousSignal = hasSignal;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			var volume = Position;
			if (_stopLossPrice is decimal sl && candle.LowPrice <= sl)
			{
				ResetTargets();
				SellMarket(volume);
				return;
			}

			if (_takeProfitPrice is decimal tp && candle.HighPrice >= tp)
			{
				ResetTargets();
				SellMarket(volume);
			}
		}
		else if (Position < 0m)
		{
			var volume = Math.Abs(Position);
			if (_stopLossPrice is decimal sl && candle.HighPrice >= sl)
			{
				ResetTargets();
				BuyMarket(volume);
				return;
			}

			if (_takeProfitPrice is decimal tp && candle.LowPrice <= tp)
			{
				ResetTargets();
				BuyMarket(volume);
			}
		}
		else if (_stopLossPrice != null || _takeProfitPrice != null)
		{
			ResetTargets();
		}
	}

	private void SetTargets(decimal entryPrice, decimal step, bool isLong)
	{
		var stopOffset = StopLossPoints * step;
		var takeOffset = TakeProfitPoints * step;

		_stopLossPrice = stopOffset > 0m ? entryPrice + (isLong ? -stopOffset : stopOffset) : null;
		_takeProfitPrice = takeOffset > 0m ? entryPrice + (isLong ? takeOffset : -takeOffset) : null;
	}

	private void ResetTargets()
	{
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	private sealed class InverseReactionIndicator : Indicator<ICandleMessage>
	{
		public int Period { get; set; } = 3;
		public decimal Coefficient { get; set; } = 1.618m;

		private readonly SimpleMovingAverage _absChangeSma = new();

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			var change = candle.ClosePrice - candle.OpenPrice;
			var absChange = Math.Abs(change);

			_absChangeSma.Length = Math.Max(1, Period);
			var smaValue = _absChangeSma.Process(new DecimalIndicatorValue(_absChangeSma, absChange, input.Time));
			var average = smaValue.ToDecimal();
			var threshold = average * Coefficient;

			IsFormed = _absChangeSma.IsFormed;
			return new InverseReactionValue(this, input, change, threshold, average);
		}

		public override void Reset()
		{
			base.Reset();
			_absChangeSma.Reset();
		}
	}

	private sealed class InverseReactionValue : ComplexIndicatorValue
	{
		public InverseReactionValue(IIndicator indicator, IIndicatorValue input, decimal change, decimal threshold, decimal average)
			: base(indicator, input, (nameof(Change), change), (nameof(DynamicThreshold), threshold), (nameof(AverageChange), average))
		{
		}

		public decimal Change => (decimal)GetValue(nameof(Change));
		public decimal DynamicThreshold => (decimal)GetValue(nameof(DynamicThreshold));
		public decimal AverageChange => (decimal)GetValue(nameof(AverageChange));
	}
}
