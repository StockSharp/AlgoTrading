using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale strategy that alternates between a MACD+MA filter and an envelope breakout after losing trades.
/// Recreates the core behaviour of the "Martingale Smart" MetaTrader expert advisor.
/// </summary>
public class MartingaleSmartStrategy : Strategy
{
	private readonly StrategyParam<bool> _useMoneyTakeProfit;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<bool> _usePercentTakeProfit;
	private readonly StrategyParam<decimal> _percentTakeProfit;
	private readonly StrategyParam<bool> _enableMoneyTrailing;
	private readonly StrategyParam<decimal> _moneyTrailingTarget;
	private readonly StrategyParam<decimal> _moneyTrailingDrawdown;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<bool> _useDoubleVolume;
	private readonly StrategyParam<decimal> _lotIncrement;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _envelopePeriod;
	private readonly StrategyParam<decimal> _envelopeDeviation;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _macdTimeFrame;

	private readonly SimpleMovingAverage _fastMa = new();
	private readonly SimpleMovingAverage _slowMa = new();
	private readonly SimpleMovingAverage _envelopeMa = new();
	private readonly MovingAverageConvergenceDivergenceSignal _macd = new();

	private decimal _pipSize;
	private decimal _nextVolume;
	private bool _usePrimaryStrategy = true;
	private bool _breakEvenTriggered;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal _moneyTrailPeak;

	private Sides? _entrySide;
	private decimal _entryPrice;
	private decimal _entryVolume;
	private decimal _cycleVolume;
	private decimal _cycleProfit;

	private decimal _macdLine;
	private decimal _macdSignal;
	private bool _macdReady;

	/// <summary>
	/// Initializes a new instance of <see cref="MartingaleSmartStrategy"/>.
	/// </summary>
	public MartingaleSmartStrategy()
	{
		_useMoneyTakeProfit = Param(nameof(UseMoneyTakeProfit), false)
		.SetDisplay("Use Money TP", "Close the position when floating profit exceeds the money target.", "Risk");

		_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 40m)
		.SetDisplay("Money TP", "Floating profit threshold that triggers position closing.", "Risk")
		.SetCanOptimize(true);

		_usePercentTakeProfit = Param(nameof(UsePercentTakeProfit), false)
		.SetDisplay("Use Percent TP", "Close the position once the floating profit reaches the percentage of equity.", "Risk");

		_percentTakeProfit = Param(nameof(PercentTakeProfit), 10m)
		.SetDisplay("Percent TP", "Floating profit target expressed as percentage of the portfolio value.", "Risk")
		.SetCanOptimize(true);

		_enableMoneyTrailing = Param(nameof(EnableMoneyTrailing), true)
		.SetDisplay("Enable Money Trailing", "Activate money based trailing stop logic.", "Risk");

		_moneyTrailingTarget = Param(nameof(MoneyTrailingTarget), 40m)
		.SetDisplay("Money Trailing Target", "Floating profit required before trailing starts.", "Risk")
		.SetCanOptimize(true);

		_moneyTrailingDrawdown = Param(nameof(MoneyTrailingDrawdown), 10m)
		.SetDisplay("Money Trailing Drawdown", "Maximum profit give back allowed once trailing is active.", "Risk")
		.SetCanOptimize(true);

		_useBreakEven = Param(nameof(UseBreakEven), true)
		.SetDisplay("Use Break-Even", "Move the stop loss to break-even after the configured distance.", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 10m)
		.SetDisplay("Break-Even Trigger (pips)", "Profit in pips required before moving the stop.", "Risk")
		.SetCanOptimize(true);

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 5m)
		.SetDisplay("Break-Even Offset (pips)", "Extra distance in pips applied when moving the stop to break-even.", "Risk")
		.SetCanOptimize(true);

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 2m)
		.SetDisplay("Martingale Multiplier", "Multiplier applied to the next trade volume after a loss.", "Money Management")
		.SetCanOptimize(true);

		_initialVolume = Param(nameof(InitialVolume), 0.01m)
		.SetDisplay("Initial Volume", "Base trade volume for the first entry of a cycle.", "Money Management")
		.SetCanOptimize(true);

		_useDoubleVolume = Param(nameof(UseDoubleVolume), false)
		.SetDisplay("Use Double Volume", "If true the next order volume is multiplied, otherwise an increment is added.", "Money Management");

		_lotIncrement = Param(nameof(LotIncrement), 0.01m)
		.SetDisplay("Lot Increment", "Fixed lot increment applied after a loss when doubling is disabled.", "Money Management")
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 30m)
		.SetDisplay("Trailing Stop (pips)", "Pip distance used by the classic price trailing stop.", "Risk")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 5m)
		.SetDisplay("Stop Loss (pips)", "Initial protective stop distance in pips.", "Risk")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 5m)
		.SetDisplay("Take Profit (pips)", "Initial take profit distance in pips.", "Risk")
		.SetCanOptimize(true);

		_fastMaPeriod = Param(nameof(FastMaPeriod), 1)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA Period", "Length of the fast moving average used in the primary filter.", "Indicators")
		.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 50)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA Period", "Length of the slow moving average used in the primary filter.", "Indicators")
		.SetCanOptimize(true);

		_envelopePeriod = Param(nameof(EnvelopePeriod), 13)
		.SetGreaterThanZero()
		.SetDisplay("Envelope Period", "Moving average length for the envelope based secondary filter.", "Indicators")
		.SetCanOptimize(true);

		_envelopeDeviation = Param(nameof(EnvelopeDeviation), 0.2m)
		.SetDisplay("Envelope Deviation", "Envelope width in percent above and below the moving average.", "Indicators")
		.SetCanOptimize(true);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length inside the MACD indicator.", "Indicators")
		.SetCanOptimize(true);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length inside the MACD indicator.", "Indicators")
		.SetCanOptimize(true);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA length inside the MACD indicator.", "Indicators")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Signal Candles", "Timeframe that drives the strategy decisions.", "General");

		_macdTimeFrame = Param(nameof(MacdTimeFrame), TimeSpan.FromDays(30).TimeFrame())
		.SetDisplay("MACD Candles", "Secondary timeframe used to calculate the MACD filter.", "General");
	}

	/// <summary>
	/// Close the position when floating profit exceeds the money target.
	/// </summary>
	public bool UseMoneyTakeProfit
	{
		get => _useMoneyTakeProfit.Value;
		set => _useMoneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Floating profit threshold that triggers position closing.
	/// </summary>
	public decimal MoneyTakeProfit
	{
		get => _moneyTakeProfit.Value;
		set => _moneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Enable floating profit target expressed as percentage of equity.
	/// </summary>
	public bool UsePercentTakeProfit
	{
		get => _usePercentTakeProfit.Value;
		set => _usePercentTakeProfit.Value = value;
	}

	/// <summary>
	/// Floating profit target expressed as percentage of the portfolio value.
	/// </summary>
	public decimal PercentTakeProfit
	{
		get => _percentTakeProfit.Value;
		set => _percentTakeProfit.Value = value;
	}

	/// <summary>
	/// Activate money based trailing stop logic.
	/// </summary>
	public bool EnableMoneyTrailing
	{
		get => _enableMoneyTrailing.Value;
		set => _enableMoneyTrailing.Value = value;
	}

	/// <summary>
	/// Floating profit required before the trailing drawdown rule engages.
	/// </summary>
	public decimal MoneyTrailingTarget
	{
		get => _moneyTrailingTarget.Value;
		set => _moneyTrailingTarget.Value = value;
	}

	/// <summary>
	/// Maximum profit give back allowed after the money trailing is active.
	/// </summary>
	public decimal MoneyTrailingDrawdown
	{
		get => _moneyTrailingDrawdown.Value;
		set => _moneyTrailingDrawdown.Value = value;
	}

	/// <summary>
	/// Enable moving the stop loss to break-even after the configured distance.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit in pips required before moving the stop loss to break-even.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Additional distance in pips applied when the stop jumps to break-even.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the next trade volume after a losing cycle.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Base trade volume for the first entry of a cycle.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// If true the next order volume is multiplied, otherwise an increment is added.
	/// </summary>
	public bool UseDoubleVolume
	{
		get => _useDoubleVolume.Value;
		set => _useDoubleVolume.Value = value;
	}

	/// <summary>
	/// Fixed lot increment applied after a loss when doubling is disabled.
	/// </summary>
	public decimal LotIncrement
	{
		get => _lotIncrement.Value;
		set => _lotIncrement.Value = value;
	}

	/// <summary>
	/// Pip distance used by the classic price trailing stop.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Initial stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Initial take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Length of the fast moving average used in the primary filter.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Length of the slow moving average used in the primary filter.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Moving average length for the envelope filter.
	/// </summary>
	public int EnvelopePeriod
	{
		get => _envelopePeriod.Value;
		set => _envelopePeriod.Value = value;
	}

	/// <summary>
	/// Envelope width in percent above and below the moving average.
	/// </summary>
	public decimal EnvelopeDeviation
	{
		get => _envelopeDeviation.Value;
		set => _envelopeDeviation.Value = value;
	}

	/// <summary>
	/// Fast EMA length inside the MACD indicator.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length inside the MACD indicator.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal EMA length inside the MACD indicator.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Timeframe that drives the strategy decisions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Secondary timeframe used to calculate the MACD filter.
	/// </summary>
	public DataType MacdTimeFrame
	{
		get => _macdTimeFrame.Value;
		set => _macdTimeFrame.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMa.Reset();
		_slowMa.Reset();
		_envelopeMa.Reset();
		_macd.Reset();

		_nextVolume = NormalizeVolume(InitialVolume);
		_usePrimaryStrategy = true;
		_breakEvenTriggered = false;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_moneyTrailPeak = 0m;

		_entrySide = null;
		_entryPrice = 0m;
		_entryVolume = 0m;
		_cycleVolume = 0m;
		_cycleProfit = 0m;

		_macdLine = 0m;
		_macdSignal = 0m;
		_macdReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var security = Security;
		var step = security?.PriceStep ?? security?.Step ?? 0m;
		_pipSize = step > 0m ? step : 0.0001m;

		_fastMa.Length = FastMaPeriod;
		_slowMa.Length = SlowMaPeriod;
		_envelopeMa.Length = EnvelopePeriod;
		_macd.FastLength = MacdFastLength;
		_macd.SlowLength = MacdSlowLength;
		_macd.SignalLength = MacdSignalLength;

		_nextVolume = NormalizeVolume(InitialVolume);

		var signalSubscription = SubscribeCandles(CandleType);
		signalSubscription
		.Bind(_fastMa, _slowMa, _envelopeMa, ProcessSignal)
		.Start();

		var macdSubscription = SubscribeCandles(MacdTimeFrame);
		macdSubscription
		.BindEx(_macd, ProcessMacd)
		.Start();
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (!macdValue.IsFinal)
		return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (typed.Macd is not decimal macdLine || typed.Signal is not decimal macdSignal)
		return;

		_macdLine = macdLine;
		_macdSignal = macdSignal;
		_macdReady = true;
	}

	private void ProcessSignal(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal envelopeBasis)
	{
		if (candle.State != CandleStates.Finished)
		return;

		ManageOpenPosition(candle.ClosePrice);

		if (Position != 0m)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_macdReady || !_fastMa.IsFormed || !_slowMa.IsFormed || !_envelopeMa.IsFormed)
		return;

		var signal = _usePrimaryStrategy ? EvaluatePrimarySignal(candle.ClosePrice, fastValue, slowValue) : EvaluateSecondarySignal(candle.ClosePrice, envelopeBasis);

		if (signal == null)
		return;

		var volume = NormalizeVolume(_nextVolume);
		if (volume <= 0m)
		return;

		var price = candle.ClosePrice;

		if (signal == Sides.Buy)
		{
			BuyMarket(volume);
			InitializeRisk(price, true);
		}
		else
		{
			SellMarket(volume);
			InitializeRisk(price, false);
		}

		_cycleVolume = volume;
		_cycleProfit = 0m;
		_moneyTrailPeak = 0m;
	}

	private Sides? EvaluatePrimarySignal(decimal closePrice, decimal fastValue, decimal slowValue)
	{
		var macdLine = _macdLine;
		var macdSignal = _macdSignal;

		if (fastValue < slowValue)
		{
			if ((macdLine > 0m && macdLine > macdSignal) || (macdLine < 0m && macdLine > macdSignal))
			return Sides.Buy;
		}
		else if (fastValue > slowValue)
		{
			if ((macdLine > 0m && macdLine < macdSignal) || (macdLine < 0m && macdLine < macdSignal))
			return Sides.Sell;
		}

		return null;
	}

	private Sides? EvaluateSecondarySignal(decimal closePrice, decimal envelopeBasis)
	{
		var deviation = EnvelopeDeviation / 100m;
		var lower = envelopeBasis * (1m - deviation);
		var upper = envelopeBasis * (1m + deviation);

		if (closePrice > lower)
		return Sides.Buy;

		if (closePrice < upper)
		return Sides.Sell;

		return null;
	}

	private void InitializeRisk(decimal entryPrice, bool isLong)
	{
		_breakEvenTriggered = false;

		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;

		_stopLossPrice = isLong ? entryPrice - stopDistance : entryPrice + stopDistance;
		_takeProfitPrice = isLong ? entryPrice + takeDistance : entryPrice - takeDistance;
	}

	private void ManageOpenPosition(decimal price)
	{
		if (Position == 0m)
		{
			_stopLossPrice = null;
			_takeProfitPrice = null;
			return;
		}

		ApplyPriceBasedStops(price);
		ApplyMoneyTargets(price);
	}

	private void ApplyPriceBasedStops(decimal price)
	{
		if (_entrySide == null)
		return;

		var isLong = _entrySide == Sides.Buy;
		var entryPrice = _entryPrice;
		var move = isLong ? price - entryPrice : entryPrice - price;

		if (UseBreakEven && !_breakEvenTriggered && BreakEvenTriggerPips > 0m)
		{
			var trigger = BreakEvenTriggerPips * _pipSize;
			if (move >= trigger)
			{
				var offset = BreakEvenOffsetPips * _pipSize;
				_stopLossPrice = isLong ? entryPrice + offset : entryPrice - offset;
				_breakEvenTriggered = true;
			}
		}

		if (TrailingStopPips > 0m)
		{
			var trailing = TrailingStopPips * _pipSize;
			if (isLong)
			{
				var candidate = price - trailing;
				if (_stopLossPrice is null || candidate > _stopLossPrice)
				_stopLossPrice = candidate;
			}
			else
			{
				var candidate = price + trailing;
				if (_stopLossPrice is null || candidate < _stopLossPrice)
				_stopLossPrice = candidate;
			}
		}

		if (_stopLossPrice is decimal stop)
		{
			if ((isLong && price <= stop) || (!isLong && price >= stop))
			{
				ClosePosition();
				return;
			}
		}

		if (_takeProfitPrice is decimal take)
		{
			if ((isLong && price >= take) || (!isLong && price <= take))
			{
				ClosePosition();
			}
		}
	}

	private void ApplyMoneyTargets(decimal price)
	{
		var floating = GetFloatingProfit(price);

		if (UseMoneyTakeProfit && MoneyTakeProfit > 0m && floating >= MoneyTakeProfit)
		{
			ClosePosition();
			return;
		}

		if (UsePercentTakeProfit && PercentTakeProfit > 0m)
		{
			var equity = GetPortfolioValue();
			if (equity > 0m)
			{
				var target = equity * PercentTakeProfit / 100m;
				if (floating >= target)
				{
					ClosePosition();
					return;
				}
			}
		}

		if (EnableMoneyTrailing && MoneyTrailingTarget > 0m)
		{
			if (floating >= MoneyTrailingTarget)
			{
				_moneyTrailPeak = Math.Max(_moneyTrailPeak, floating);
				if (MoneyTrailingDrawdown > 0m && floating <= _moneyTrailPeak - MoneyTrailingDrawdown)
				{
					ClosePosition();
					return;
				}
			}
			else
			{
				_moneyTrailPeak = Math.Max(_moneyTrailPeak, floating);
			}
		}
	}

	private decimal GetFloatingProfit(decimal price)
	{
		if (_entrySide == null || _entryVolume <= 0m)
		return 0m;

		var diff = _entrySide == Sides.Buy ? price - _entryPrice : _entryPrice - price;
		return ConvertPriceToMoney(diff, _entryVolume);
	}

	private void ClosePosition()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position);
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
		return;

		var orderSide = trade.Order.Side;
		var tradeVolume = trade.Trade.Volume;
		var tradePrice = trade.Trade.Price;

		if (_entrySide == null)
		{
			if (Position > 0m && orderSide == Sides.Buy)
			{
				_entrySide = Sides.Buy;
				_entryVolume = Position;
				_entryPrice = tradePrice;
				_cycleVolume = _entryVolume;
				_cycleProfit = 0m;
				_moneyTrailPeak = 0m;
			}
			else if (Position < 0m && orderSide == Sides.Sell)
			{
				_entrySide = Sides.Sell;
				_entryVolume = Math.Abs(Position);
				_entryPrice = tradePrice;
				_cycleVolume = _entryVolume;
				_cycleProfit = 0m;
				_moneyTrailPeak = 0m;
			}
			return;
		}

		if (orderSide == _entrySide)
		{
			var totalVolume = _entryVolume + tradeVolume;
			_entryPrice = (_entryPrice * _entryVolume + tradePrice * tradeVolume) / totalVolume;
			_entryVolume = totalVolume;
			_cycleVolume = _entryVolume;
			return;
		}

		var profitDiff = _entrySide == Sides.Buy ? tradePrice - _entryPrice : _entryPrice - tradePrice;
		var profit = ConvertPriceToMoney(profitDiff, tradeVolume);
		_cycleProfit += profit;

		var remaining = Math.Abs(Position);
		_entryVolume = remaining;

		if (remaining == 0m)
		{
			HandleCycleClosed(_cycleProfit, _cycleVolume);

			_entrySide = null;
			_entryPrice = 0m;
			_entryVolume = 0m;
			_cycleVolume = 0m;
			_cycleProfit = 0m;
			_moneyTrailPeak = 0m;
		}
	}

	private void HandleCycleClosed(decimal profit, decimal volume)
	{
		if (volume <= 0m)
		{
			_nextVolume = NormalizeVolume(InitialVolume);
			return;
		}

		if (profit < 0m)
		{
			_usePrimaryStrategy = !_usePrimaryStrategy;
			var nextVolume = UseDoubleVolume ? volume * MartingaleMultiplier : volume + LotIncrement;
			_nextVolume = NormalizeVolume(nextVolume);
		}
		else
		{
			_nextVolume = NormalizeVolume(InitialVolume);
		}

		_breakEvenTriggered = false;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		var min = security.VolumeMin ?? 0m;
		if (min > 0m && volume < min)
		volume = min;

		var max = security.VolumeMax ?? 0m;
		if (max > 0m && volume > max)
		volume = max;

		return volume;
	}

	private decimal ConvertPriceToMoney(decimal priceDiff, decimal volume)
	{
		var security = Security;
		if (security == null)
		return priceDiff * volume;

		var priceStep = security.PriceStep ?? security.Step ?? 0m;
		var stepPrice = security.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
		return priceDiff * volume;

		return priceDiff / priceStep * stepPrice * volume;
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
		return 0m;

		var value = portfolio.CurrentValue;
		if (value == 0m)
		value = portfolio.BeginValue;

		return value;
	}
}
