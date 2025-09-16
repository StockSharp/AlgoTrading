using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that looks for four consecutive candles with rising or falling highs and opens.
/// The algorithm closes positions once a profit target for the trading account is reached.
/// It also applies a trailing stop with optional partial exits when the price moves in favor of the position.
/// </summary>
public class TakeProfitBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _shift1;
	private readonly StrategyParam<int> _shift2;
	private readonly StrategyParam<int> _shift3;
	private readonly StrategyParam<int> _shift4;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<bool> _useRiskManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<bool> _partialClose;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage?[] _recentCandles = Array.Empty<ICandleMessage?>();
	private decimal _initialBalance;
	private decimal _averageEntryPrice;
	private decimal _currentPositionVolume;
	private decimal? _longStop;
	private decimal? _shortStop;

	/// <summary>
	/// Index of the first candle used in the breakout comparison.
	/// </summary>
	public int Shift1
	{
		get => _shift1.Value;
		set => _shift1.Value = value;
	}

/// <summary>
/// Index of the second candle used in the breakout comparison.
/// </summary>
public int Shift2
{
	get => _shift2.Value;
	set => _shift2.Value = value;
}

/// <summary>
/// Index of the third candle used in the breakout comparison.
/// </summary>
public int Shift3
{
	get => _shift3.Value;
	set => _shift3.Value = value;
}

/// <summary>
/// Index of the fourth candle used in the breakout comparison.
/// </summary>
public int Shift4
{
	get => _shift4.Value;
	set => _shift4.Value = value;
}

/// <summary>
/// Trailing stop distance expressed in price steps.
/// </summary>
public int TrailingStopPoints
{
	get => _trailingStopPoints.Value;
	set => _trailingStopPoints.Value = value;
}

/// <summary>
/// Stop-loss distance expressed in price steps.
/// </summary>
public int StopLossPoints
{
	get => _stopLossPoints.Value;
	set => _stopLossPoints.Value = value;
}

/// <summary>
/// Profit target applied to the account equity.
/// </summary>
public decimal ProfitTarget
{
	get => _profitTarget.Value;
	set => _profitTarget.Value = value;
}

/// <summary>
/// Fixed trade volume used when risk management is disabled.
/// </summary>
public decimal FixedVolume
{
	get => _fixedVolume.Value;
	set => _fixedVolume.Value = value;
}

/// <summary>
/// Enables risk-based position sizing.
/// </summary>
public bool UseRiskManagement
{
	get => _useRiskManagement.Value;
	set => _useRiskManagement.Value = value;
}

/// <summary>
/// Risk percentage applied when <see cref="UseRiskManagement"/> is enabled.
/// </summary>
public decimal RiskPercent
{
	get => _riskPercent.Value;
	set => _riskPercent.Value = value;
}

/// <summary>
/// Enables partial exits when the trailing stop advances.
/// </summary>
public bool PartialClose
{
	get => _partialClose.Value;
	set => _partialClose.Value = value;
}

/// <summary>
/// Maximum number of standard position units that may remain open at once.
/// </summary>
public int MaxOrders
{
	get => _maxOrders.Value;
	set => _maxOrders.Value = value;
}

/// <summary>
/// The candle type used for market analysis.
/// </summary>
public DataType CandleType
{
	get => _candleType.Value;
	set => _candleType.Value = value;
}

/// <summary>
/// Initializes strategy parameters.
/// </summary>
public TakeProfitBreakoutStrategy()
{
	_shift1 = Param(nameof(Shift1), 0)
	.SetRange(0, int.MaxValue)
	.SetDisplay("Shift #1", "Index of the most recent candle", "Pattern");

	_shift2 = Param(nameof(Shift2), 1)
	.SetRange(0, int.MaxValue)
	.SetDisplay("Shift #2", "Index of the second candle", "Pattern");

	_shift3 = Param(nameof(Shift3), 2)
	.SetRange(0, int.MaxValue)
	.SetDisplay("Shift #3", "Index of the third candle", "Pattern");

	_shift4 = Param(nameof(Shift4), 3)
	.SetRange(0, int.MaxValue)
	.SetDisplay("Shift #4", "Index of the fourth candle", "Pattern");

	_trailingStopPoints = Param(nameof(TrailingStopPoints), 1)
	.SetRange(0, int.MaxValue)
	.SetDisplay("Trailing Stop", "Trailing distance in price steps", "Risk Management");

	_stopLossPoints = Param(nameof(StopLossPoints), 0)
	.SetRange(0, int.MaxValue)
	.SetDisplay("Stop Loss", "Initial stop distance in price steps", "Risk Management");

	_profitTarget = Param(nameof(ProfitTarget), 1m)
	.SetGreaterOrEqualZero()
	.SetDisplay("Profit Target", "Profit target applied to account equity", "Risk Management");

	_fixedVolume = Param(nameof(FixedVolume), 1m)
	.SetGreaterThanZero()
	.SetDisplay("Lots", "Fixed trading volume when risk management is disabled", "Trading");

	_useRiskManagement = Param(nameof(UseRiskManagement), true)
	.SetDisplay("Risk Management", "Enable risk-based position sizing", "Risk Management");

	_riskPercent = Param(nameof(RiskPercent), 1m)
	.SetRange(1m, 100m)
	.SetDisplay("Risk Percent", "Risk percentage applied to portfolio equity", "Risk Management");

	_partialClose = Param(nameof(PartialClose), true)
	.SetDisplay("Partial Close", "Close half the position when the trailing stop advances", "Risk Management");

	_maxOrders = Param(nameof(MaxOrders), 1)
	.SetRange(0, int.MaxValue)
	.SetDisplay("Max Orders", "Maximum simultaneous position units", "Trading");

	_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
	.SetDisplay("Candle Type", "Candle type used for signals", "General");
}

/// <inheritdoc />
public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
	return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnReseted()
{
	base.OnReseted();

	_recentCandles = Array.Empty<ICandleMessage?>();
	_initialBalance = 0m;
	_averageEntryPrice = 0m;
	_currentPositionVolume = 0m;
	_longStop = null;
	_shortStop = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
	base.OnStarted(time);

	_initialBalance = (Portfolio?.BeginValue ?? Portfolio?.CurrentValue) ?? 0m;
	_averageEntryPrice = 0m;
	_currentPositionVolume = Position;
	_longStop = null;
	_shortStop = null;

	var maxShift = Math.Max(Math.Max(Shift1, Shift2), Math.Max(Shift3, Shift4));
	if (maxShift < 0)
	maxShift = 0;

	_recentCandles = new ICandleMessage?[maxShift + 1];

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(ProcessCandle)
	.Start();
}

private void ProcessCandle(ICandleMessage candle)
{
	if (candle.State != CandleStates.Finished)
	return;

	if (CheckProfitTarget())
	return;

	if (Position != 0)
	{
		UpdateTrailingStops(candle);
		if (CheckProtectiveStops(candle))
		return;
	}

StoreCandle(candle);

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (!TryGetCandle(Shift1, out var c1) ||
!TryGetCandle(Shift2, out var c2) ||
!TryGetCandle(Shift3, out var c3) ||
!TryGetCandle(Shift4, out var c4))
{
	return;
}

var buySignal = c1.HighPrice > c2.HighPrice &&
c2.HighPrice > c3.HighPrice &&
c3.HighPrice > c4.HighPrice &&
c1.OpenPrice > c2.OpenPrice &&
c2.OpenPrice > c3.OpenPrice &&
c3.OpenPrice > c4.OpenPrice;

var sellSignal = c1.HighPrice < c2.HighPrice &&
c2.HighPrice < c3.HighPrice &&
c3.HighPrice < c4.HighPrice &&
c1.OpenPrice < c2.OpenPrice &&
c2.OpenPrice < c3.OpenPrice &&
c3.OpenPrice < c4.OpenPrice;

if (buySignal)
{
	TryEnterLong(candle.ClosePrice);
}
else if (sellSignal)
{
	TryEnterShort(candle.ClosePrice);
}
}

private bool CheckProfitTarget()
{
	if (ProfitTarget <= 0m)
	return false;

	var portfolio = Portfolio;
	if (portfolio == null)
	return false;

	var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
	if (equity <= 0m)
	return false;

	if (equity < _initialBalance + ProfitTarget)
	return false;

	if (Position != 0)
	{
		ClosePosition();
	}

CancelActiveOrders();
LogInfo($"Profit target reached. Equity={equity:F2}. Closing all positions.");
return true;
}

private void UpdateTrailingStops(ICandleMessage candle)
{
	var step = GetPriceStep();
	if (step <= 0m)
	return;

	var trailingDistance = TrailingStopPoints * step;
	if (trailingDistance <= 0m)
	return;

	var positionVolume = Position;
	if (positionVolume > 0m)
	{
		if (_averageEntryPrice <= 0m)
		return;

		if (candle.ClosePrice - _averageEntryPrice < trailingDistance)
		return;

		var targetStop = candle.ClosePrice - trailingDistance;
		if (!_longStop.HasValue || _longStop.Value < targetStop)
		{
			_longStop = targetStop;
			LogInfo($"Trailing stop for long position moved to {_longStop.Value:F5}.");

			if (PartialClose && TryGetPartialVolume(positionVolume, out var partVolume))
			{
				SellMarket(partVolume);
				LogInfo($"Partial close executed for long position. Volume={partVolume}.");
			}
	}
}
else if (positionVolume < 0m)
{
	if (_averageEntryPrice <= 0m)
	return;

	if (_averageEntryPrice - candle.ClosePrice < trailingDistance)
	return;

	var targetStop = candle.ClosePrice + trailingDistance;
	if (!_shortStop.HasValue || _shortStop.Value > targetStop)
	{
		_shortStop = targetStop;
		LogInfo($"Trailing stop for short position moved to {_shortStop.Value:F5}.");

		if (PartialClose && TryGetPartialVolume(-positionVolume, out var partVolume))
		{
			BuyMarket(partVolume);
			LogInfo($"Partial close executed for short position. Volume={partVolume}.");
		}
}
}
}

private bool CheckProtectiveStops(ICandleMessage candle)
{
	var positionVolume = Position;

	if (positionVolume > 0m && _longStop.HasValue && candle.LowPrice <= _longStop.Value)
	{
		var volume = Math.Abs(positionVolume);
		if (volume > 0m)
		{
			SellMarket(volume);
			LogInfo($"Long stop-loss triggered at {_longStop.Value:F5}.");
		}

	ResetPositionState();
	return true;
}

if (positionVolume < 0m && _shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
{
	var volume = Math.Abs(positionVolume);
	if (volume > 0m)
	{
		BuyMarket(volume);
		LogInfo($"Short stop-loss triggered at {_shortStop.Value:F5}.");
	}

ResetPositionState();
return true;
}

return false;
}

private void TryEnterLong(decimal price)
{
	var volume = CalculateTradeVolume(price);
	if (volume <= 0m)
	return;

	if (!CanOpenAdditionalPosition(volume))
	return;

	BuyMarket(volume);
	LogInfo($"Opening long position at {price:F5} with volume {volume}.");
}

private void TryEnterShort(decimal price)
{
	var volume = CalculateTradeVolume(price);
	if (volume <= 0m)
	return;

	if (!CanOpenAdditionalPosition(volume))
	return;

	SellMarket(volume);
	LogInfo($"Opening short position at {price:F5} with volume {volume}.");
}

private bool CanOpenAdditionalPosition(decimal volume)
{
	if (MaxOrders <= 0)
	return true;

	var current = Math.Abs(Position);
	var unitVolume = volume <= 0m ? 1m : volume;
	return current < MaxOrders * unitVolume;
}

private decimal CalculateTradeVolume(decimal price)
{
	var baseVolume = FixedVolume > 0m ? FixedVolume : 1m;

	if (!UseRiskManagement)
	return NormalizeVolume(baseVolume);

	if (RiskPercent <= 0m)
	return NormalizeVolume(baseVolume);

	var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
	if (portfolioValue <= 0m)
	return NormalizeVolume(baseVolume);

	if (price <= 0m)
	return NormalizeVolume(baseVolume);

	var riskAmount = portfolioValue * (RiskPercent / 100m);
	if (riskAmount <= 0m)
	return NormalizeVolume(baseVolume);

	var step = GetPriceStep();
	var stopDistance = StopLossPoints > 0 ? StopLossPoints * step : 0m;
	var perUnitRisk = stopDistance > 0m ? stopDistance : price;
	if (perUnitRisk <= 0m)
	return NormalizeVolume(baseVolume);

	var volume = riskAmount / perUnitRisk;
	if (volume <= 0m)
	volume = baseVolume;

	return NormalizeVolume(volume);
}

private decimal NormalizeVolume(decimal volume)
{
	var security = Security;
	if (security != null)
	{
		var step = security.VolumeStep ?? 1m;
		if (step <= 0m)
		step = 1m;

		var steps = Math.Max(1m, Math.Floor(volume / step));
		volume = steps * step;
	}

if (volume <= 0m)
volume = 1m;

return volume;
}

private bool TryGetPartialVolume(decimal currentVolume, out decimal partialVolume)
{
	partialVolume = 0m;

	var security = Security;
	if (security == null)
	return false;

	var step = security.VolumeStep ?? 1m;
	if (step <= 0m)
	step = 1m;

	var half = Math.Floor((currentVolume / 2m) / step) * step;
	if (half < step)
	return false;

	if (half >= currentVolume)
	return false;

	partialVolume = half;
	return partialVolume > 0m;
}

private void StoreCandle(ICandleMessage candle)
{
	if (_recentCandles.Length == 0)
	return;

	for (var i = _recentCandles.Length - 1; i > 0; i--)
	{
		_recentCandles[i] = _recentCandles[i - 1];
	}

_recentCandles[0] = candle;
}

private bool TryGetCandle(int shift, out ICandleMessage candle)
{
	candle = default!;

	if (shift < 0 || shift >= _recentCandles.Length)
	return false;

	var stored = _recentCandles[shift];
	if (stored == null)
	return false;

	candle = stored;
	return true;
}

private decimal GetPriceStep()
{
	var security = Security;
	if (security == null)
	return 0m;

	var step = security.PriceStep ?? 0m;
	if (step <= 0m)
	step = security.MinPriceStep ?? 0m;

	return step > 0m ? step : 0m;
}

private void ResetPositionState()
{
	_averageEntryPrice = 0m;
	_currentPositionVolume = Position;
	_longStop = null;
	_shortStop = null;
}

/// <inheritdoc />
protected override void OnNewMyTrade(MyTrade trade)
{
	base.OnNewMyTrade(trade);

	if (trade.Order?.Security != Security)
	return;

	var prevVolume = _currentPositionVolume;
	var newVolume = Position;
	var prevAbs = Math.Abs(prevVolume);
	var newAbs = Math.Abs(newVolume);
	var tradePrice = trade.Trade.Price;

	if (newAbs > prevAbs)
	{
		if (prevAbs > 0m && Math.Sign(prevVolume) == Math.Sign(newVolume))
		{
			var addedVolume = newAbs - prevAbs;
			if (addedVolume > 0m)
			{
				_averageEntryPrice = ((prevAbs * _averageEntryPrice) + (addedVolume * tradePrice)) / newAbs;
			}
	}
else
{
	_averageEntryPrice = tradePrice;
}

if (newVolume > 0m)
{
	_shortStop = null;
	ApplyInitialStop(true);
}
else if (newVolume < 0m)
{
	_longStop = null;
	ApplyInitialStop(false);
}
}
else if (newAbs < prevAbs)
{
	if (newVolume == 0m)
	{
		ResetPositionState();
	}
}
else if (newAbs == 0m)
{
	ResetPositionState();
}

_currentPositionVolume = newVolume;
}

private void ApplyInitialStop(bool isLong)
{
	var step = GetPriceStep();
	if (step <= 0m)
	return;

	if (StopLossPoints <= 0)
	return;

	var distance = StopLossPoints * step;
	if (distance <= 0m)
	return;

	if (isLong)
	{
		_longStop = _averageEntryPrice - distance;
		LogInfo($"Initial long stop placed at {_longStop.Value:F5}.");
	}
else
{
	_shortStop = _averageEntryPrice + distance;
	LogInfo($"Initial short stop placed at {_shortStop.Value:F5}.");
}
}
}
