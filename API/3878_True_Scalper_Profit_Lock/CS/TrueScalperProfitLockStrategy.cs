namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

public class TrueScalperProfitLockStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<bool> _useRsiMethodA;
	private readonly StrategyParam<bool> _useRsiMethodB;
	private readonly StrategyParam<decimal> _rsiThreshold;
	private readonly StrategyParam<bool> _abandonMethodA;
	private readonly StrategyParam<bool> _abandonMethodB;
	private readonly StrategyParam<int> _abandonBars;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<bool> _accountIsMini;
	private readonly StrategyParam<bool> _liveTradingMode;
	private readonly StrategyParam<bool> _useProfitLock;
	private readonly StrategyParam<decimal> _breakEvenTriggerPoints;
	private readonly StrategyParam<decimal> _breakEvenOffsetPoints;
	private readonly StrategyParam<int> _maxOpenTrades;

	private ExponentialMovingAverage? _fastEma;
	private ExponentialMovingAverage? _slowEma;
	private RelativeStrengthIndex? _rsi;

	private decimal? _previousFastEma;
	private decimal? _previousSlowEma;
	private decimal? _previousRsi;
	private decimal? _olderRsi;

	private bool _forceBuy;
	private bool _forceSell;

	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private int _barsSinceEntry;
	private bool _breakEvenApplied;

	public TrueScalperProfitLockStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle type", "Timeframe processed by the strategy.", "General");

		_fixedVolume = Param(nameof(FixedVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Fixed volume", "Base order size used when money management is disabled.", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 44)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take profit", "Distance to the profit target expressed in price steps.", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 90)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop loss", "Distance to the protective stop expressed in price steps.", "Risk");

		_useRsiMethodA = Param(nameof(UseRsiMethodA), false)
		.SetDisplay("RSI method A", "Enable the crossover based RSI confirmation.", "Filters");

		_useRsiMethodB = Param(nameof(UseRsiMethodB), true)
		.SetDisplay("RSI method B", "Enable the polarity based RSI confirmation.", "Filters");

		_rsiThreshold = Param(nameof(RsiThreshold), 50m)
		.SetDisplay("RSI threshold", "Threshold compared with the RSI value.", "Filters");

		_abandonMethodA = Param(nameof(AbandonMethodA), true)
		.SetDisplay("Abandon method A", "Close the trade after the timeout and immediately reverse.", "Trade management");

		_abandonMethodB = Param(nameof(AbandonMethodB), false)
		.SetDisplay("Abandon method B", "Close the trade after the timeout without reversing.", "Trade management");

		_abandonBars = Param(nameof(AbandonBars), 101)
		.SetGreaterOrEqualZero()
		.SetDisplay("Abandon bars", "Number of completed candles before the abandon logic triggers.", "Trade management");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), true)
		.SetDisplay("Use money management", "Recalculate volume from balance and risk settings.", "Trading");

		_riskPercent = Param(nameof(RiskPercent), 2m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Risk percent", "Risk percentage used by the money management formula.", "Trading");

		_accountIsMini = Param(nameof(AccountIsMini), false)
		.SetDisplay("Account is mini", "Mimic mini account lot sizing adjustments.", "Trading");

		_liveTradingMode = Param(nameof(LiveTradingMode), false)
		.SetDisplay("Live trading mode", "Apply the live account boundaries to the position size.", "Trading");

		_useProfitLock = Param(nameof(UseProfitLock), true)
		.SetDisplay("Use profit lock", "Move the stop to break-even after a defined profit.", "Risk");

		_breakEvenTriggerPoints = Param(nameof(BreakEvenTriggerPoints), 25m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Break-even trigger", "Profit distance that activates the break-even move.", "Risk");

		_breakEvenOffsetPoints = Param(nameof(BreakEvenOffsetPoints), 3m)
		.SetDisplay("Break-even offset", "Additional distance added when moving the stop to break-even.", "Risk");

		_maxOpenTrades = Param(nameof(MaxOpenTrades), 1)
		.SetGreaterOrEqualZero()
		.SetDisplay("Max trades", "Maximum number of simultaneous open trades.", "Trading");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public bool UseRsiMethodA
	{
		get => _useRsiMethodA.Value;
		set => _useRsiMethodA.Value = value;
	}

	public bool UseRsiMethodB
	{
		get => _useRsiMethodB.Value;
		set => _useRsiMethodB.Value = value;
	}

	public decimal RsiThreshold
	{
		get => _rsiThreshold.Value;
		set => _rsiThreshold.Value = value;
	}

	public bool AbandonMethodA
	{
		get => _abandonMethodA.Value;
		set => _abandonMethodA.Value = value;
	}

	public bool AbandonMethodB
	{
		get => _abandonMethodB.Value;
		set => _abandonMethodB.Value = value;
	}

	public int AbandonBars
	{
		get => _abandonBars.Value;
		set => _abandonBars.Value = value;
	}

	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	public bool AccountIsMini
	{
		get => _accountIsMini.Value;
		set => _accountIsMini.Value = value;
	}

	public bool LiveTradingMode
	{
		get => _liveTradingMode.Value;
		set => _liveTradingMode.Value = value;
	}

	public bool UseProfitLock
	{
		get => _useProfitLock.Value;
		set => _useProfitLock.Value = value;
	}

	public decimal BreakEvenTriggerPoints
	{
		get => _breakEvenTriggerPoints.Value;
		set => _breakEvenTriggerPoints.Value = value;
	}

	public decimal BreakEvenOffsetPoints
	{
		get => _breakEvenOffsetPoints.Value;
		set => _breakEvenOffsetPoints.Value = value;
	}

	public int MaxOpenTrades
	{
		get => _maxOpenTrades.Value;
		set => _maxOpenTrades.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = CalculateTradeVolume();

		_fastEma = new ExponentialMovingAverage { Length = 3 };
		_slowEma = new ExponentialMovingAverage { Length = 7 };
		_rsi = new RelativeStrengthIndex { Length = 2 };

		ResetRuntimeState();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_fastEma != null)
			{
				DrawIndicator(area, _fastEma);
			}
			if (_slowEma != null)
			{
				DrawIndicator(area, _slowEma);
			}
			if (_rsi != null)
			{
				DrawIndicator(area, _rsi);
			}
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return; // Use only completed candles just like the MetaTrader expert
		}

		ManageActivePosition(candle);

		if (_fastEma == null || _slowEma == null || _rsi == null)
		{
			return;
		}

		var fastValue = _fastEma.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();
		var slowValue = _slowEma.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();
		var rsiValue = _rsi.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();

		if (!_fastEma.IsFormed || !_slowEma.IsFormed || !_rsi.IsFormed)
		{
			UpdateHistory(fastValue, slowValue, rsiValue);
			return;
		}

		if (_previousFastEma is null || _previousSlowEma is null || _previousRsi is null || _olderRsi is null)
		{
			UpdateHistory(fastValue, slowValue, rsiValue);
			return;
		}

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			UpdateHistory(fastValue, slowValue, rsiValue);
			return;
		}

		var fastPrev = _previousFastEma.Value;
		var slowPrev = _previousSlowEma.Value;
		var rsiPrev = _previousRsi.Value;
		var rsiOlder = _olderRsi.Value;

		var rsiPositive = false;
		var rsiNegative = false;

		if (UseRsiMethodA)
		{
			if (rsiOlder > RsiThreshold && rsiPrev < RsiThreshold)
			{
				rsiPositive = true;
				rsiNegative = false;
			}
			else if (rsiOlder < RsiThreshold && rsiPrev > RsiThreshold)
			{
				rsiPositive = false;
				rsiNegative = true;
			}
		}

		if (UseRsiMethodB)
		{
			if (rsiOlder > RsiThreshold)
			{
				rsiPositive = true;
				rsiNegative = false;
			}
			if (rsiOlder < RsiThreshold)
			{
				rsiPositive = false;
				rsiNegative = true;
			}
		}

		var buySignal = fastPrev > slowPrev + step && rsiNegative;
		var sellSignal = fastPrev < slowPrev - step && rsiPositive;

		if ((buySignal || _forceBuy) && CanOpenLong())
		{
			ExecuteBuy(candle.ClosePrice);
			_forceBuy = false;
		}
		else if ((sellSignal || _forceSell) && CanOpenShort())
		{
			ExecuteSell(candle.ClosePrice);
			_forceSell = false;
		}

		UpdateHistory(fastValue, slowValue, rsiValue);
	}

	// Manage the open position by applying stops, break-even adjustments, and abandon logic.
	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			_barsSinceEntry++;

			if (_entryPrice is null)
			{
				_entryPrice = candle.ClosePrice;
			}

			if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (UseProfitLock)
			{
				ApplyBreakEvenForLong(candle);
			}

			if ((AbandonMethodA || AbandonMethodB) && AbandonBars > 0 && _barsSinceEntry >= AbandonBars)
			{
				SellMarket(Position);
				ResetPositionState();
				if (AbandonMethodA)
				{
					_forceSell = true;
				}
			}
		}
		else if (Position < 0m)
		{
			_barsSinceEntry++;

			if (_entryPrice is null)
			{
				_entryPrice = candle.ClosePrice;
			}

			if (_stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (UseProfitLock)
			{
				ApplyBreakEvenForShort(candle);
			}

			if ((AbandonMethodA || AbandonMethodB) && AbandonBars > 0 && _barsSinceEntry >= AbandonBars)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				if (AbandonMethodA)
				{
					_forceBuy = true;
				}
			}
		}
		else
		{
			ResetPositionState();
		}
	}

	private void ApplyBreakEvenForLong(ICandleMessage candle)
	{
		if (!UseProfitLock || BreakEvenTriggerPoints <= 0m || _entryPrice is null)
		{
			return;
		}

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			return;
		}

		var entry = _entryPrice.Value;
		var trigger = entry + BreakEvenTriggerPoints * step;

		if (_breakEvenApplied)
		{
			return;
		}

		if (candle.ClosePrice >= trigger || candle.HighPrice >= trigger)
		{
			var newStop = entry + BreakEvenOffsetPoints * step;
			if (!_stopLossPrice.HasValue || _stopLossPrice.Value < entry)
			{
				_stopLossPrice = newStop;
				_breakEvenApplied = true;
			}
		}
	}

	private void ApplyBreakEvenForShort(ICandleMessage candle)
	{
		if (!UseProfitLock || BreakEvenTriggerPoints <= 0m || _entryPrice is null)
		{
			return;
		}

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			return;
		}

		var entry = _entryPrice.Value;
		var trigger = entry - BreakEvenTriggerPoints * step;

		if (_breakEvenApplied)
		{
			return;
		}

		if (candle.ClosePrice <= trigger || candle.LowPrice <= trigger)
		{
			var newStop = entry - BreakEvenOffsetPoints * step;
			if (!_stopLossPrice.HasValue || _stopLossPrice.Value > entry)
			{
				_stopLossPrice = newStop;
				_breakEvenApplied = true;
			}
		}
	}

	// Close any short exposure and open a long position.
	private void ExecuteBuy(decimal price)
	{
		var openingVolume = CalculateTradeVolume();
		Volume = openingVolume;

		var closingVolume = Position < 0m ? Math.Abs(Position) : 0m;
		var totalVolume = closingVolume + openingVolume;
		if (totalVolume <= 0m)
		{
			return;
		}

		BuyMarket(totalVolume);

		if (openingVolume > 0m)
		{
			PrepareNewPosition(price, true);
		}
		else
		{
			ResetPositionState();
		}
	}

	// Close any long exposure and open a short position.
	private void ExecuteSell(decimal price)
	{
		var openingVolume = CalculateTradeVolume();
		Volume = openingVolume;

		var closingVolume = Position > 0m ? Position : 0m;
		var totalVolume = closingVolume + openingVolume;
		if (totalVolume <= 0m)
		{
			return;
		}

		SellMarket(totalVolume);

		if (openingVolume > 0m)
		{
			PrepareNewPosition(price, false);
		}
		else
		{
			ResetPositionState();
		}
	}

	private void PrepareNewPosition(decimal price, bool isLong)
	{
		_entryPrice = price;
		_barsSinceEntry = 0;
		_breakEvenApplied = false;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			_stopLossPrice = null;
			_takeProfitPrice = null;
			return;
		}

		if (isLong)
		{
			_stopLossPrice = StopLossPoints > 0 ? price - StopLossPoints * step : null;
			_takeProfitPrice = TakeProfitPoints > 0 ? price + TakeProfitPoints * step : null;
		}
		else
		{
			_stopLossPrice = StopLossPoints > 0 ? price + StopLossPoints * step : null;
			_takeProfitPrice = TakeProfitPoints > 0 ? price - TakeProfitPoints * step : null;
		}
	}

	private bool CanOpenLong()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return false;
		}

		if (Position > 0m)
		{
			return false;
		}

		return MaxOpenTrades <= 0 || Math.Abs(Position) < MaxOpenTrades;
	}

	private bool CanOpenShort()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return false;
		}

		if (Position < 0m)
		{
			return false;
		}

		return MaxOpenTrades <= 0 || Math.Abs(Position) < MaxOpenTrades;
	}

	private void UpdateHistory(decimal fastValue, decimal slowValue, decimal rsiValue)
	{
		_olderRsi = _previousRsi;
		_previousRsi = rsiValue;
		_previousFastEma = fastValue;
		_previousSlowEma = slowValue;
	}

	private void ResetRuntimeState()
	{
		_previousFastEma = null;
		_previousSlowEma = null;
		_previousRsi = null;
		_olderRsi = null;
		_forceBuy = false;
		_forceSell = false;
		ResetPositionState();
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_barsSinceEntry = 0;
		_breakEvenApplied = false;
	}

	private decimal CalculateTradeVolume()
	{
		if (!UseMoneyManagement)
		{
			return FixedVolume;
		}

		var balance = Portfolio?.BeginValue ?? 0m;
		if (balance <= 0m)
		{
			return FixedVolume;
		}

		var rawLots = Math.Ceiling(balance * RiskPercent / 10000m) / 10m;
		if (rawLots < 0.1m)
		{
			rawLots = FixedVolume;
		}
		if (rawLots > 1m)
		{
			rawLots = Math.Ceiling(rawLots);
		}

		if (LiveTradingMode)
		{
			if (AccountIsMini)
			{
				rawLots *= 10m;
			}
			else if (rawLots < 1m)
			{
				rawLots = 1m;
			}
		}

		if (rawLots > 100m)
		{
			rawLots = 100m;
		}

		return rawLots;
	}
}
