using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the MetaTrader expert advisor "linear regression channel".
/// The logic combines higher-timeframe momentum, weighted moving averages, and a monthly MACD filter.
/// </summary>
public class LinearRegressionChannelFibStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingActivationPoints;
	private readonly StrategyParam<decimal> _trailingOffsetPoints;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPoints;
	private readonly StrategyParam<decimal> _breakEvenOffsetPoints;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _useMoneyTakeProfit;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<bool> _usePercentTakeProfit;
	private readonly StrategyParam<decimal> _percentTakeProfit;
	private readonly StrategyParam<bool> _enableMoneyTrailing;
	private readonly StrategyParam<decimal> _moneyTrailingTrigger;
	private readonly StrategyParam<decimal> _moneyTrailingStop;
	private readonly StrategyParam<bool> _useEquityStop;
	private readonly StrategyParam<decimal> _totalEquityRisk;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _monthlyMacd = null!;

	private readonly Queue<decimal> _momentumBuffer = new();

	private bool _momentumReady;
	private bool _macroReady;
	private bool _macroBullish;
	private bool _macroBearish;
	private decimal _pointValue;
	private decimal _stepPrice;
	private decimal? _entryPrice;
	private Sides? _entrySide;
	private decimal _highestPriceSinceEntry;
	private decimal _lowestPriceSinceEntry;
	private bool _breakEvenActive;
	private decimal _breakEvenPrice;
	private decimal _maxFloatingProfit;
	private decimal _initialCapital;
	private decimal _equityPeak;
	private int _tradesOpened;

	/// <summary>
	/// Initializes a new instance of the <see cref="LinearRegressionChannelFibStrategy"/> class.
	/// </summary>
	public LinearRegressionChannelFibStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used by the strategy", "General");

		_fastMaLength = Param(nameof(FastMaLength), 6)
		.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(4, 12, 2);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
		.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(60, 120, 5);

		_momentumLength = Param(nameof(MomentumLength), 14)
		.SetDisplay("Momentum Length", "Lookback for the momentum confirmation", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 20, 1);

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
		.SetDisplay("Momentum Buy Threshold", "Minimum deviation from 100 for bullish momentum", "Filters");

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
		.SetDisplay("Momentum Sell Threshold", "Minimum deviation from 100 for bearish momentum", "Filters");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
		.SetDisplay("Take Profit (points)", "Profit target distance expressed in instrument points", "Risk Management");

		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
		.SetDisplay("Stop Loss (points)", "Stop distance expressed in instrument points", "Risk Management");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
		.SetDisplay("Use Trailing", "Enable trailing stop once profit target is reached", "Risk Management");

		_trailingActivationPoints = Param(nameof(TrailingActivationPoints), 40m)
		.SetDisplay("Trailing Activation", "Profit required before trailing starts", "Risk Management");

		_trailingOffsetPoints = Param(nameof(TrailingOffsetPoints), 40m)
		.SetDisplay("Trailing Offset", "Distance maintained from the best price", "Risk Management");

		_useBreakEven = Param(nameof(UseBreakEven), true)
		.SetDisplay("Use Break-even", "Lock trades at break-even after reaching a profit buffer", "Risk Management");

		_breakEvenTriggerPoints = Param(nameof(BreakEvenTriggerPoints), 30m)
		.SetDisplay("Break-even Trigger", "Profit required to activate break-even", "Risk Management");

		_breakEvenOffsetPoints = Param(nameof(BreakEvenOffsetPoints), 30m)
		.SetDisplay("Break-even Offset", "Offset added to the entry price when locking", "Risk Management");

		_maxTrades = Param(nameof(MaxTrades), 10)
		.SetDisplay("Max Trades", "Maximum number of entries per session", "Trading Limits");

		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetDisplay("Order Volume", "Base volume used for entries", "General");

		_useMoneyTakeProfit = Param(nameof(UseMoneyTakeProfit), false)
		.SetDisplay("Use Money TP", "Close when floating profit exceeds MoneyTakeProfit", "Money Management");

		_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 40m)
		.SetDisplay("Money Take Profit", "Profit target measured in account currency", "Money Management");

		_usePercentTakeProfit = Param(nameof(UsePercentTakeProfit), false)
		.SetDisplay("Use Percent TP", "Close when profit exceeds PercentTakeProfit of equity", "Money Management");

		_percentTakeProfit = Param(nameof(PercentTakeProfit), 10m)
		.SetDisplay("Percent Take Profit", "Profit target expressed as percent of initial equity", "Money Management");

		_enableMoneyTrailing = Param(nameof(EnableMoneyTrailing), true)
		.SetDisplay("Enable Money Trailing", "Trail floating profit after the trigger is reached", "Money Management");

		_moneyTrailingTrigger = Param(nameof(MoneyTrailingTrigger), 40m)
		.SetDisplay("Money Trailing Trigger", "Floating profit required to start trailing", "Money Management");

		_moneyTrailingStop = Param(nameof(MoneyTrailingStop), 10m)
		.SetDisplay("Money Trailing Stop", "Maximum allowed give-back after the trigger", "Money Management");

		_useEquityStop = Param(nameof(UseEquityStop), true)
		.SetDisplay("Use Equity Stop", "Protect equity by closing trades after a drawdown", "Money Management");

		_totalEquityRisk = Param(nameof(TotalEquityRisk), 1m)
		.SetDisplay("Equity Risk %", "Maximum drawdown from the equity peak before closing", "Money Management");
	}

	/// <summary>
	/// Candle type used for the main signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the fast weighted moving average.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Length of the slow weighted moving average.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Momentum indicator length.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Minimum distance from 100 required for bullish momentum confirmation.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum distance from 100 required for bearish momentum confirmation.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Profit target distance expressed in instrument points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets whether the trailing stop logic is enabled.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Profit required before the trailing stop becomes active.
	/// </summary>
	public decimal TrailingActivationPoints
	{
		get => _trailingActivationPoints.Value;
		set => _trailingActivationPoints.Value = value;
	}

	/// <summary>
	/// Offset maintained between price extremes and the trailing stop.
	/// </summary>
	public decimal TrailingOffsetPoints
	{
		get => _trailingOffsetPoints.Value;
		set => _trailingOffsetPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets whether break-even protection is enabled.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit required to activate break-even protection.
	/// </summary>
	public decimal BreakEvenTriggerPoints
	{
		get => _breakEvenTriggerPoints.Value;
		set => _breakEvenTriggerPoints.Value = value;
	}

	/// <summary>
	/// Offset in points added to the entry price when placing the break-even stop.
	/// </summary>
	public decimal BreakEvenOffsetPoints
	{
		get => _breakEvenOffsetPoints.Value;
		set => _breakEvenOffsetPoints.Value = value;
	}

	/// <summary>
	/// Maximum number of trades allowed per run.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Base order volume used when opening positions.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Gets or sets whether the floating profit target in currency is used.
	/// </summary>
	public bool UseMoneyTakeProfit
	{
		get => _useMoneyTakeProfit.Value;
		set => _useMoneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Floating profit target expressed in account currency.
	/// </summary>
	public decimal MoneyTakeProfit
	{
		get => _moneyTakeProfit.Value;
		set => _moneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Gets or sets whether the floating profit target in percent is used.
	/// </summary>
	public bool UsePercentTakeProfit
	{
		get => _usePercentTakeProfit.Value;
		set => _usePercentTakeProfit.Value = value;
	}

	/// <summary>
	/// Floating profit target expressed as a percentage of the initial capital.
	/// </summary>
	public decimal PercentTakeProfit
	{
		get => _percentTakeProfit.Value;
		set => _percentTakeProfit.Value = value;
	}

	/// <summary>
	/// Gets or sets whether the floating profit trailing logic is enabled.
	/// </summary>
	public bool EnableMoneyTrailing
	{
		get => _enableMoneyTrailing.Value;
		set => _enableMoneyTrailing.Value = value;
	}

	/// <summary>
	/// Floating profit required before trailing starts.
	/// </summary>
	public decimal MoneyTrailingTrigger
	{
		get => _moneyTrailingTrigger.Value;
		set => _moneyTrailingTrigger.Value = value;
	}

	/// <summary>
	/// Maximum allowed drawdown from the floating profit peak.
	/// </summary>
	public decimal MoneyTrailingStop
	{
		get => _moneyTrailingStop.Value;
		set => _moneyTrailingStop.Value = value;
	}

	/// <summary>
	/// Gets or sets whether the equity-based stop is enabled.
	/// </summary>
	public bool UseEquityStop
	{
		get => _useEquityStop.Value;
		set => _useEquityStop.Value = value;
	}

	/// <summary>
	/// Maximum percentage drawdown from the equity peak allowed before closing.
	/// </summary>
	public decimal TotalEquityRisk
	{
		get => _totalEquityRisk.Value;
		set => _totalEquityRisk.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
		yield break;

		yield return (Security, CandleType);

		var momentumType = GetMomentumCandleType();
		if (!momentumType.Equals(CandleType))
		yield return (Security, momentumType);

		var macroType = GetMacroCandleType();
		if (!macroType.Equals(CandleType) && !macroType.Equals(momentumType))
		yield return (Security, macroType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMa = null!;
		_slowMa = null!;
		_momentum = null!;
		_monthlyMacd = null!;

		_momentumBuffer.Clear();
		_momentumReady = false;
		_macroReady = false;
		_macroBullish = false;
		_macroBearish = false;
		_pointValue = 0m;
		_stepPrice = 0m;
		_entryPrice = null;
		_entrySide = null;
		_highestPriceSinceEntry = 0m;
		_lowestPriceSinceEntry = 0m;
		_breakEvenActive = false;
		_breakEvenPrice = 0m;
		_maxFloatingProfit = 0m;
		_initialCapital = 0m;
		_equityPeak = 0m;
		_tradesOpened = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = Security?.PriceStep ?? 1m;
		if (_pointValue <= 0m)
		_pointValue = 1m;

		_stepPrice = Security?.StepPrice ?? 0m;
		_initialCapital = Portfolio?.BeginValue ?? Portfolio?.CurrentValue ?? 0m;
		_equityPeak = _initialCapital;
		_tradesOpened = 0;
		_maxFloatingProfit = 0m;

		Volume = OrderVolume;

		_fastMa = new WeightedMovingAverage
		{
			Length = FastMaLength,
			CandlePrice = CandlePrice.Typical
		};

		_slowMa = new WeightedMovingAverage
		{
			Length = SlowMaLength,
			CandlePrice = CandlePrice.Typical
		};

		_momentum = new Momentum
		{
			Length = MomentumLength
		};

		_monthlyMacd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
		.Bind(_fastMa, _slowMa, ProcessMainCandle)
		.Start();

		var momentumSubscription = SubscribeCandles(GetMomentumCandleType());
		momentumSubscription
		.Bind(_momentum, ProcessMomentum)
		.Start();

		var macroSubscription = SubscribeCandles(GetMacroCandleType());
		macroSubscription
		.BindEx(_monthlyMacd, ProcessMonthlyMacd)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private DataType GetMomentumCandleType()
	{
		var timeFrame = CandleType.TimeFrame ?? TimeSpan.FromMinutes(15);

		var momentumFrame = timeFrame.TotalMinutes switch
		{
			<= 1 => TimeSpan.FromMinutes(15),
			5 => TimeSpan.FromMinutes(30),
			15 => TimeSpan.FromMinutes(60),
			30 => TimeSpan.FromHours(4),
			60 => TimeSpan.FromDays(1),
			240 => TimeSpan.FromDays(7),
			1440 => TimeSpan.FromDays(30),
			_ => timeFrame
		};

		return momentumFrame.TimeFrame();
	}

	private DataType GetMacroCandleType()
	{
		return TimeSpan.FromDays(30).TimeFrame();
	}

	private void ProcessMomentum(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_momentum.IsFormed)
		return;

		var deviation = Math.Abs(100m - momentumValue);
		_momentumBuffer.Enqueue(deviation);

		while (_momentumBuffer.Count > 3)
		_momentumBuffer.Dequeue();

		_momentumReady = _momentumBuffer.Count == 3;
	}

	private void ProcessMonthlyMacd(ICandleMessage candle, IIndicatorValue value)
	{
		if (!value.IsFinal)
		return;

		var macdValue = (MovingAverageConvergenceDivergenceSignalValue)value;
		if (macdValue.Macd is not decimal macdMain || macdValue.Signal is not decimal macdSignal)
		return;

		_macroBullish = macdMain > macdSignal;
		_macroBearish = macdMain < macdSignal;
		_macroReady = true;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal fastMa, decimal slowMa)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_momentumReady || !_macroReady)
		return;

		var price = candle.ClosePrice;

		if (Position != 0m)
		{
			if (TryApplyMoneyTargets(price))
			return;

			if (TryApplyEquityStop(price))
			return;

			if (Position > 0m && ManageLongPosition(candle))
			return;

			if (Position < 0m && ManageShortPosition(candle))
			return;
		}
		else
		{
			ResetProtection();
		}

		if (_tradesOpened >= MaxTrades)
		return;

		if (_momentumBuffer.Count < 3)
		return;

		var values = _momentumBuffer.ToArray();
		var hasBullMomentum = values[0] >= MomentumBuyThreshold || values[1] >= MomentumBuyThreshold || values[2] >= MomentumBuyThreshold;
		var hasBearMomentum = values[0] >= MomentumSellThreshold || values[1] >= MomentumSellThreshold || values[2] >= MomentumSellThreshold;

		var volume = Volume;
		if (volume <= 0m)
		return;

		if (fastMa > slowMa && hasBullMomentum && _macroBullish && Position <= 0m)
		{
			var orderVolume = volume + Math.Abs(Position);
			if (orderVolume > 0m)
			BuyMarket(orderVolume);
		}
		else if (fastMa < slowMa && hasBearMomentum && _macroBearish && Position >= 0m)
		{
			var orderVolume = volume + Math.Abs(Position);
			if (orderVolume > 0m)
			SellMarket(orderVolume);
		}
	}

	private void ResetProtection()
	{
		_maxFloatingProfit = 0m;
		_breakEvenActive = false;
		_breakEvenPrice = 0m;
	}

	private bool ManageLongPosition(ICandleMessage candle)
	{
		var entryPrice = _entryPrice;
		if (entryPrice == null)
		return false;

		_highestPriceSinceEntry = Math.Max(_highestPriceSinceEntry, candle.HighPrice);
		_lowestPriceSinceEntry = _lowestPriceSinceEntry == 0m ? candle.LowPrice : Math.Min(_lowestPriceSinceEntry, candle.LowPrice);

		var takeProfit = TakeProfitPoints > 0m ? entryPrice.Value + TakeProfitPoints * _pointValue : (decimal?)null;
		var stopLoss = StopLossPoints > 0m ? entryPrice.Value - StopLossPoints * _pointValue : (decimal?)null;

		if (takeProfit != null && candle.HighPrice >= takeProfit)
		{
			SellMarket(Position);
			return true;
		}

		if (stopLoss != null && candle.LowPrice <= stopLoss)
		{
			SellMarket(Position);
			return true;
		}

		if (UseTrailingStop && TrailingOffsetPoints > 0m)
		{
			var activation = TrailingActivationPoints * _pointValue;
			var offset = TrailingOffsetPoints * _pointValue;
			var move = _highestPriceSinceEntry - entryPrice.Value;

			if (move >= activation)
			{
				var trailingStop = _highestPriceSinceEntry - offset;
				if (candle.LowPrice <= trailingStop)
				{
					SellMarket(Position);
					return true;
				}
			}
		}

		if (UseBreakEven && !_breakEvenActive && BreakEvenTriggerPoints > 0m)
		{
			var trigger = entryPrice.Value + BreakEvenTriggerPoints * _pointValue;
			if (candle.HighPrice >= trigger)
			{
				_breakEvenActive = true;
				_breakEvenPrice = entryPrice.Value + BreakEvenOffsetPoints * _pointValue;
			}
		}

		if (_breakEvenActive && candle.LowPrice <= _breakEvenPrice)
		{
			SellMarket(Position);
			return true;
		}

		return false;
	}

	private bool ManageShortPosition(ICandleMessage candle)
	{
		var entryPrice = _entryPrice;
		if (entryPrice == null)
		return false;

		_highestPriceSinceEntry = Math.Max(_highestPriceSinceEntry, candle.HighPrice);
		_lowestPriceSinceEntry = _lowestPriceSinceEntry == 0m ? candle.LowPrice : Math.Min(_lowestPriceSinceEntry, candle.LowPrice);

		var takeProfit = TakeProfitPoints > 0m ? entryPrice.Value - TakeProfitPoints * _pointValue : (decimal?)null;
		var stopLoss = StopLossPoints > 0m ? entryPrice.Value + StopLossPoints * _pointValue : (decimal?)null;

		if (takeProfit != null && candle.LowPrice <= takeProfit)
		{
			BuyMarket(-Position);
			return true;
		}

		if (stopLoss != null && candle.HighPrice >= stopLoss)
		{
			BuyMarket(-Position);
			return true;
		}

		if (UseTrailingStop && TrailingOffsetPoints > 0m)
		{
			var activation = TrailingActivationPoints * _pointValue;
			var offset = TrailingOffsetPoints * _pointValue;
			var move = entryPrice.Value - _lowestPriceSinceEntry;

			if (move >= activation)
			{
				var trailingStop = _lowestPriceSinceEntry + offset;
				if (candle.HighPrice >= trailingStop)
				{
					BuyMarket(-Position);
					return true;
				}
			}
		}

		if (UseBreakEven && !_breakEvenActive && BreakEvenTriggerPoints > 0m)
		{
			var trigger = entryPrice.Value - BreakEvenTriggerPoints * _pointValue;
			if (candle.LowPrice <= trigger)
			{
				_breakEvenActive = true;
				_breakEvenPrice = entryPrice.Value - BreakEvenOffsetPoints * _pointValue;
			}
		}

		if (_breakEvenActive && candle.HighPrice >= _breakEvenPrice)
		{
			BuyMarket(-Position);
			return true;
		}

		return false;
	}

	private bool TryApplyMoneyTargets(decimal closePrice)
	{
		if (Position == 0m)
		return false;

		var profit = GetFloatingProfit(closePrice);

		if (UseMoneyTakeProfit && MoneyTakeProfit > 0m && profit >= MoneyTakeProfit)
		{
			ClosePosition();
			return true;
		}

		if (UsePercentTakeProfit && PercentTakeProfit > 0m && _initialCapital > 0m)
		{
			var target = _initialCapital * PercentTakeProfit / 100m;
			if (profit >= target)
			{
				ClosePosition();
				return true;
			}
		}

		if (EnableMoneyTrailing && MoneyTrailingStop > 0m)
		{
			if (profit >= MoneyTrailingTrigger)
			_maxFloatingProfit = Math.Max(_maxFloatingProfit, profit);

			if (_maxFloatingProfit > 0m && _maxFloatingProfit - profit >= MoneyTrailingStop)
			{
				ClosePosition();
				return true;
			}
		}

		return false;
	}

	private bool TryApplyEquityStop(decimal closePrice)
	{
		if (!UseEquityStop)
		return false;

		var floating = GetFloatingProfit(closePrice);
		var realized = PnL;
		var equity = _initialCapital + realized + floating;
		_equityPeak = Math.Max(_equityPeak, equity);

		if (floating >= 0m || _equityPeak <= 0m)
		return false;

		var threshold = _equityPeak * TotalEquityRisk / 100m;
		if (threshold <= 0m)
		return false;

		if (Math.Abs(floating) >= threshold)
		{
			ClosePosition();
			return true;
		}

		return false;
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

	private decimal GetFloatingProfit(decimal price)
	{
		if (Position == 0m || PositionPrice is not decimal entry)
		return 0m;

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m || _stepPrice <= 0m)
		return 0m;

		var direction = Position > 0m ? 1m : -1m;
		var priceDiff = (price - entry) * direction;
		var steps = priceDiff / priceStep;
		return steps * _stepPrice * Math.Abs(Position);
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (Position == 0m)
		{
			_entryPrice = null;
			_entrySide = null;
			_highestPriceSinceEntry = 0m;
			_lowestPriceSinceEntry = 0m;
			_breakEvenActive = false;
			_breakEvenPrice = 0m;
			_maxFloatingProfit = 0m;
			_tradesOpened = 0;
			return;
		}

		var tradePrice = trade.Trade.Price;

		if (Position > 0m)
		{
			if (_entrySide != Sides.Buy)
			{
				_entrySide = Sides.Buy;
				_entryPrice = tradePrice;
				_highestPriceSinceEntry = tradePrice;
				_lowestPriceSinceEntry = tradePrice;
				_breakEvenActive = false;
				_breakEvenPrice = 0m;
				_maxFloatingProfit = 0m;
				_tradesOpened = Math.Min(_tradesOpened + 1, MaxTrades);
			}
			else
			{
				_entryPrice = tradePrice;
				_highestPriceSinceEntry = Math.Max(_highestPriceSinceEntry, tradePrice);
				_lowestPriceSinceEntry = Math.Min(_lowestPriceSinceEntry, tradePrice);
			}
		}
		else if (Position < 0m)
		{
			if (_entrySide != Sides.Sell)
			{
				_entrySide = Sides.Sell;
				_entryPrice = tradePrice;
				_highestPriceSinceEntry = tradePrice;
				_lowestPriceSinceEntry = tradePrice;
				_breakEvenActive = false;
				_breakEvenPrice = 0m;
				_maxFloatingProfit = 0m;
				_tradesOpened = Math.Min(_tradesOpened + 1, MaxTrades);
			}
			else
			{
				_entryPrice = tradePrice;
				_highestPriceSinceEntry = Math.Max(_highestPriceSinceEntry, tradePrice);
				_lowestPriceSinceEntry = Math.Min(_lowestPriceSinceEntry, tradePrice);
			}
		}
	}
}

