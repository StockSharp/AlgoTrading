using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Triple Top and Triple Bottom" MetaTrader strategy.
/// The strategy trades pullbacks in the prevailing trend using linear weighted averages,
/// momentum strength confirmation and a MACD filter that avoids fading strong trends.
/// </summary>
public class TripleTopTripleBottomStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _stopLossDistance;
	private readonly StrategyParam<decimal> _takeProfitDistance;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal? _momentumDeviation1;
	private decimal? _momentumDeviation2;
	private decimal? _momentumDeviation3;
	private decimal? _macdMain;
	private decimal? _macdSignal;

	private decimal? _entryPrice;
	private decimal _highestSinceEntry;
	private decimal _lowestSinceEntry;

	/// <summary>
	/// Initializes a new instance of <see cref="TripleTopTripleBottomStrategy"/>.
	/// </summary>
	public TripleTopTripleBottomStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Entry Candle", "Time frame used to evaluate entry signals", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Period", "Momentum lookback used for strength confirmation", "Indicators");

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Threshold", "Minimal deviation from 100 required for confirmation", "Indicators");

		_stopLossDistance = Param(nameof(StopLossDistance), 0.0020m)
		.SetNonNegative()
		.SetDisplay("Stop Loss", "Protective stop distance in price units", "Risk");

		_takeProfitDistance = Param(nameof(TakeProfitDistance), 0.0050m)
		.SetNonNegative()
		.SetDisplay("Take Profit", "Profit target distance in price units", "Risk");
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
	/// Length of the fast linear weighted moving average.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Length of the slow linear weighted moving average.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum lookback period expressed in bars.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimal deviation from the neutral 100 momentum level required to trigger orders.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Protective stop distance expressed in absolute price units.
	/// </summary>
	public decimal StopLossDistance
	{
		get => _stopLossDistance.Value;
		set => _stopLossDistance.Value = value;
	}

	/// <summary>
	/// Profit target distance expressed in absolute price units.
	/// </summary>
	public decimal TakeProfitDistance
	{
		get => _takeProfitDistance.Value;
		set => _takeProfitDistance.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_fastMa = new LinearWeightedMovingAverage
		{
			Length = FastMaPeriod,
			CandlePrice = CandlePrice.Typical
		};

		_slowMa = new LinearWeightedMovingAverage
		{
			Length = SlowMaPeriod,
			CandlePrice = CandlePrice.Typical
		};

		_momentum = new Momentum
		{
			Length = MomentumPeriod
		};

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			FastLength = 12,
			SlowLength = 26,
			SignalLength = 9
		};

		_momentumDeviation1 = null;
		_momentumDeviation2 = null;
		_momentumDeviation3 = null;
		_macdMain = null;
		_macdSignal = null;
		_entryPrice = null;
		_highestSinceEntry = 0m;
		_lowestSinceEntry = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastMa, _slowMa, _momentum, ProcessCandle);
		subscription.BindEx(_macd, ProcessMacd);
		subscription.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);

			var oscArea = CreateChartArea();
			if (oscArea != null)
			{
				DrawIndicator(oscArea, _momentum);
				DrawIndicator(oscArea, _macd);
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			_entryPrice = null;
			_highestSinceEntry = 0m;
			_lowestSinceEntry = 0m;
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Trade == null)
		return;

		_entryPrice = trade.Trade.Price;
		_highestSinceEntry = trade.Trade.Price;
		_lowestSinceEntry = trade.Trade.Price;
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue value)
	{
		if (!value.IsFinal)
		return;

		if (value is not MovingAverageConvergenceDivergenceSignalValue typed)
		return;

		if (typed.Signal is not decimal signal || typed.Macd is not decimal macd)
		return;

		_macdSignal = signal;
		_macdMain = macd;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMa, decimal slowMa, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		UpdateMomentum(momentumValue);
		ManageOpenPosition(candle);

		if (!TryGetMomentumStrength(out var hasLongMomentum, out var hasShortMomentum))
		return;

		if (_macdMain is null || _macdSignal is null)
		return;

		var price = candle.ClosePrice;

		if (fastMa > slowMa && hasLongMomentum && _macdMain > _macdSignal && Position <= 0m)
		{
			if (Position < 0m)
			{
				ClosePosition();
				return;
			}

			BuyMarket();
			ApplyRiskOrders(price, Position + Volume);
		}
		else if (fastMa < slowMa && hasShortMomentum && _macdMain < _macdSignal && Position >= 0m)
		{
			if (Position > 0m)
			{
				ClosePosition();
				return;
			}

			SellMarket();
			ApplyRiskOrders(price, Position - Volume);
		}
	}

	private void UpdateMomentum(decimal momentumValue)
	{
		var deviation = Math.Abs(momentumValue - 100m);

		_momentumDeviation1 = _momentumDeviation2;
		_momentumDeviation2 = _momentumDeviation3;
		_momentumDeviation3 = deviation;
	}

	private bool TryGetMomentumStrength(out bool hasLongMomentum, out bool hasShortMomentum)
	{
		hasLongMomentum = false;
		hasShortMomentum = false;

		if (_momentumDeviation1 is null || _momentumDeviation2 is null || _momentumDeviation3 is null)
		return false;

		var threshold = MomentumThreshold;
		hasLongMomentum = _momentumDeviation1 >= threshold || _momentumDeviation2 >= threshold || _momentumDeviation3 >= threshold;
		hasShortMomentum = hasLongMomentum;
		return true;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (_entryPrice is null || Position == 0m)
		return;

		var entryPrice = _entryPrice.Value;
		_highestSinceEntry = Math.Max(_highestSinceEntry, candle.HighPrice);
		_lowestSinceEntry = _lowestSinceEntry == 0m ? candle.LowPrice : Math.Min(_lowestSinceEntry, candle.LowPrice);

		var stopLoss = StopLossDistance;
		var takeProfit = TakeProfitDistance;

		if (Position > 0m)
		{
			if (stopLoss > 0m && candle.LowPrice <= entryPrice - stopLoss)
			{
				ClosePosition();
				return;
			}

			if (takeProfit > 0m && candle.HighPrice >= entryPrice + takeProfit)
			{
				ClosePosition();
			}
		}
		else if (Position < 0m)
		{
			if (stopLoss > 0m && candle.HighPrice >= entryPrice + stopLoss)
			{
				ClosePosition();
				return;
			}

			if (takeProfit > 0m && candle.LowPrice <= entryPrice - takeProfit)
			{
				ClosePosition();
			}
		}
	}

	private void ApplyRiskOrders(decimal price, decimal resultingPosition)
	{
		var stopLoss = StopLossDistance;
		var takeProfit = TakeProfitDistance;

		if (stopLoss > 0m)
		SetStopLoss(stopLoss, price, resultingPosition);

		if (takeProfit > 0m)
		SetTakeProfit(takeProfit, price, resultingPosition);
	}
}
