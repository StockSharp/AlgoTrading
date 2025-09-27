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
/// Strategy inspired by the "Fibonacci Time Zones" expert advisor.
/// It relies on monthly MACD momentum together with Bollinger band exits and
/// extensive money management features such as break-even moves, trailing stops
/// and profit based liquidation.
/// </summary>
public class FibonacciTimeZonesStrategy : Strategy
{
	private readonly StrategyParam<bool> _useTakeProfitMoney;
	private readonly StrategyParam<decimal> _takeProfitMoney;
	private readonly StrategyParam<bool> _useTakeProfitPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _enableTrailingProfit;
	private readonly StrategyParam<decimal> _trailingTakeProfitMoney;
	private readonly StrategyParam<decimal> _trailingStopLossMoney;
	private readonly StrategyParam<bool> _useStop;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<int> _numberOfTrades;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _useMoveToBreakEven;
	private readonly StrategyParam<decimal> _whenToMoveToBreakEven;
	private readonly StrategyParam<decimal> _pipsToMoveStopLoss;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _macdCandleType;

	private decimal _pipSize;
	private decimal _initialEquity;

	private bool _hasMacdState;
	private bool _isMacdAboveSignal;
	private bool _pendingBullishEntry;
	private bool _pendingBearishEntry;

	private decimal _lastEntryPrice;
	private decimal? _trailingStopPrice;
	private bool _breakEvenActivated;
	private decimal _breakEvenPrice;

	private bool _profitTrailingActivated;
	private decimal _profitPeak;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public FibonacciTimeZonesStrategy()
	{
		_useTakeProfitMoney = Param(nameof(UseTakeProfitMoney), false)
		.SetDisplay("Use TP in Money", "Close all positions when target profit in money is reached.", "Money management");

		_takeProfitMoney = Param(nameof(TakeProfitMoney), 40m)
		.SetGreaterThanZero()
		.SetDisplay("TP Money", "Target accumulated profit in account currency.", "Money management");

		_useTakeProfitPercent = Param(nameof(UseTakeProfitPercent), false)
		.SetDisplay("Use TP in %", "Close positions when profit reaches the specified percentage of initial equity.", "Money management");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 10m)
		.SetGreaterThanZero()
		.SetDisplay("TP Percent", "Target profit as percentage of initial equity.", "Money management");

		_enableTrailingProfit = Param(nameof(EnableTrailingProfit), true)
		.SetDisplay("Enable Profit Trailing", "Trail floating profit after a threshold is reached.", "Money management");

		_trailingTakeProfitMoney = Param(nameof(TrailingTakeProfitMoney), 40m)
		.SetGreaterThanZero()
		.SetDisplay("Profit Threshold", "Profit level which activates money trailing.", "Money management");

		_trailingStopLossMoney = Param(nameof(TrailingStopLossMoney), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Profit Drawdown", "Allowed profit giveback before liquidating.", "Money management");

		_useStop = Param(nameof(UseStop), true)
		.SetDisplay("Use Bollinger Stop", "Close trades when price touches the opposite Bollinger band.", "Stops");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pips)", "Initial stop loss distance in pips.", "Stops");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (pips)", "Initial take profit distance in pips.", "Stops");

		_numberOfTrades = Param(nameof(NumberOfTrades), 3)
		.SetGreaterThanZero()
		.SetDisplay("Trades per Signal", "How many market orders should be sent when a signal appears.", "Execution");

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips.", "Stops");

		_useMoveToBreakEven = Param(nameof(UseMoveToBreakEven), true)
		.SetDisplay("Use Break-even", "Move stop to break-even after price advances.", "Stops");

		_whenToMoveToBreakEven = Param(nameof(WhenToMoveToBreakEven), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Break-even Trigger (pips)", "Distance in pips before break-even activates.", "Stops");

		_pipsToMoveStopLoss = Param(nameof(PipsToMoveStopLoss), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Break-even Offset (pips)", "Additional pips added to the break-even price.", "Stops");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Trading Candles", "Primary timeframe used for trade management.", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
		.SetDisplay("MACD Candles", "Timeframe for the MACD momentum filter.", "General");
	}

	/// <summary>
	/// Enable take profit in money.
	/// </summary>
	public bool UseTakeProfitMoney
	{
		get => _useTakeProfitMoney.Value;
		set => _useTakeProfitMoney.Value = value;
	}

	/// <summary>
	/// Target profit in account currency.
	/// </summary>
	public decimal TakeProfitMoney
	{
		get => _takeProfitMoney.Value;
		set => _takeProfitMoney.Value = value;
	}

	/// <summary>
	/// Enable take profit in percent of equity.
	/// </summary>
	public bool UseTakeProfitPercent
	{
		get => _useTakeProfitPercent.Value;
		set => _useTakeProfitPercent.Value = value;
	}

	/// <summary>
	/// Target profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Enable trailing of floating profit in money terms.
	/// </summary>
	public bool EnableTrailingProfit
	{
		get => _enableTrailingProfit.Value;
		set => _enableTrailingProfit.Value = value;
	}

	/// <summary>
	/// Profit level that activates money trailing.
	/// </summary>
	public decimal TrailingTakeProfitMoney
	{
		get => _trailingTakeProfitMoney.Value;
		set => _trailingTakeProfitMoney.Value = value;
	}

	/// <summary>
	/// Maximum profit giveback allowed during trailing.
	/// </summary>
	public decimal TrailingStopLossMoney
	{
		get => _trailingStopLossMoney.Value;
		set => _trailingStopLossMoney.Value = value;
	}

	/// <summary>
	/// Whether to use Bollinger band exits.
	/// </summary>
	public bool UseStop
	{
		get => _useStop.Value;
		set => _useStop.Value = value;
	}

	/// <summary>
	/// Stop loss in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Number of market orders per entry signal.
	/// </summary>
	public int NumberOfTrades
	{
		get => _numberOfTrades.Value;
		set => _numberOfTrades.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Enable break-even stop logic.
	/// </summary>
	public bool UseMoveToBreakEven
	{
		get => _useMoveToBreakEven.Value;
		set => _useMoveToBreakEven.Value = value;
	}

	/// <summary>
	/// Distance in pips before break-even activates.
	/// </summary>
	public decimal WhenToMoveToBreakEven
	{
		get => _whenToMoveToBreakEven.Value;
		set => _whenToMoveToBreakEven.Value = value;
	}

	/// <summary>
	/// Additional distance in pips applied to the break-even stop.
	/// </summary>
	public decimal PipsToMoveStopLoss
	{
		get => _pipsToMoveStopLoss.Value;
		set => _pipsToMoveStopLoss.Value = value;
	}

	/// <summary>
	/// Trading candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type used by the MACD filter.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (CandleType == MacdCandleType)
		return [(Security, CandleType)];

		return [(Security, CandleType), (Security, MacdCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_pipSize = 0m;
		_initialEquity = 0m;
		_hasMacdState = false;
		_isMacdAboveSignal = false;
		_pendingBullishEntry = false;
		_pendingBearishEntry = false;
		_lastEntryPrice = 0m;
		_trailingStopPrice = null;
		_breakEvenActivated = false;
		_breakEvenPrice = 0m;
		_profitTrailingActivated = false;
		_profitPeak = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		step = 0.0001m;

		// Replicate the pip calculation from the MQL version.
		_pipSize = step;
		if (_pipSize == 0.00001m || _pipSize == 0.01m)
		_pipSize *= 10m;

		_initialEquity = GetPortfolioValue();

		Unit stopLossUnit = StopLossPips > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null;
		Unit takeProfitUnit = TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null;
		Unit trailingUnit = TrailingStopPips > 0m ? new Unit(TrailingStopPips * _pipSize, UnitTypes.Absolute) : null;

		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit, trailingStop: trailingUnit);

		var tradingSubscription = SubscribeCandles(CandleType);
		var bollinger = new BollingerBands
		{
			Length = 20,
			Width = 2m
		};

		tradingSubscription
		.Bind(bollinger, ProcessTradingCandle)
		.Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		macdSubscription
		.BindEx(macd, ProcessMacd)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradingSubscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);

			var macdArea = CreateChartArea();
			if (macdArea != null)
			{
				DrawCandles(macdArea, macdSubscription);
				DrawIndicator(macdArea, macd);
			}
		}
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!macdValue.IsFinal)
		return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = macdTyped.Macd;
		var signal = macdTyped.Signal;
		var isAbove = macd > signal;

		if (!_hasMacdState)
		{
			_isMacdAboveSignal = isAbove;
			_hasMacdState = true;
			return;
		}

		if (isAbove && !_isMacdAboveSignal)
		{
			_pendingBullishEntry = true;
			_pendingBearishEntry = false;
		}
		else if (!isAbove && _isMacdAboveSignal)
		{
			_pendingBearishEntry = true;
			_pendingBullishEntry = false;
		}

		_isMacdAboveSignal = isAbove;
	}

	private void ProcessTradingCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		HandleMoneyTargets(candle);

		if (UseStop)
		HandleBollingerExit(candle.ClosePrice, upper, lower);

		HandleBreakEven(candle);
		HandleTrailingStop(candle);

		if (Position == 0)
		{
			_trailingStopPrice = null;
			_breakEvenActivated = false;
			_profitTrailingActivated = false;
		}

		if (Position == 0 && _pendingBullishEntry)
		{
			EnterLong();
			_pendingBullishEntry = false;
		}
		else if (Position == 0 && _pendingBearishEntry)
		{
			EnterShort();
			_pendingBearishEntry = false;
		}
	}

	private void HandleMoneyTargets(ICandleMessage candle)
	{
		var totalProfit = PnL + GetUnrealizedPnL(candle);

		if (UseTakeProfitMoney && totalProfit >= TakeProfitMoney)
		{
			ClosePosition();
			return;
		}

		if (UseTakeProfitPercent && _initialEquity > 0m)
		{
			var percentProfit = totalProfit / _initialEquity * 100m;
			if (percentProfit >= TakeProfitPercent)
			{
				ClosePosition();
				return;
			}
		}

		if (!EnableTrailingProfit)
		{
			_profitTrailingActivated = false;
			_profitPeak = 0m;
			return;
		}

		if (!_profitTrailingActivated)
		{
			if (totalProfit >= TrailingTakeProfitMoney)
			{
				_profitTrailingActivated = true;
				_profitPeak = totalProfit;
			}
		}
		else
		{
			if (totalProfit > _profitPeak)
			_profitPeak = totalProfit;

			if (_profitPeak - totalProfit >= TrailingStopLossMoney)
			{
				ClosePosition();
				_profitTrailingActivated = false;
			}
		}
	}

	private void HandleBollingerExit(decimal closePrice, decimal upper, decimal lower)
	{
		if (Position > 0 && closePrice >= upper)
		ClosePosition();
		else if (Position < 0 && closePrice <= lower)
		ClosePosition();
	}

	private void HandleBreakEven(ICandleMessage candle)
	{
		if (!UseMoveToBreakEven || Position == 0 || _lastEntryPrice == 0m)
		return;

		var triggerDistance = GetPipDistance(WhenToMoveToBreakEven);
		var offset = GetPipDistance(PipsToMoveStopLoss);

		if (Position > 0)
		{
			if (!_breakEvenActivated && candle.HighPrice >= _lastEntryPrice + triggerDistance)
			{
				_breakEvenActivated = true;
				_breakEvenPrice = _lastEntryPrice + offset;
			}

			if (_breakEvenActivated && candle.LowPrice <= _breakEvenPrice)
			{
				ClosePosition();
			}
		}
		else if (Position < 0)
		{
			if (!_breakEvenActivated && candle.LowPrice <= _lastEntryPrice - triggerDistance)
			{
				_breakEvenActivated = true;
				_breakEvenPrice = _lastEntryPrice - offset;
			}

			if (_breakEvenActivated && candle.HighPrice >= _breakEvenPrice)
			{
				ClosePosition();
			}
		}
	}

	private void HandleTrailingStop(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || Position == 0 || _lastEntryPrice == 0m)
		return;

		var trailingDistance = GetPipDistance(TrailingStopPips);

		if (Position > 0)
		{
			if (candle.ClosePrice >= _lastEntryPrice + trailingDistance)
			{
				var candidate = candle.ClosePrice - trailingDistance;
				if (_trailingStopPrice == null || candidate > _trailingStopPrice)
				_trailingStopPrice = candidate;
			}

			if (_trailingStopPrice != null && candle.LowPrice <= _trailingStopPrice)
			ClosePosition();
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice <= _lastEntryPrice - trailingDistance)
			{
				var candidate = candle.ClosePrice + trailingDistance;
				if (_trailingStopPrice == null || candidate < _trailingStopPrice)
				_trailingStopPrice = candidate;
			}

			if (_trailingStopPrice != null && candle.HighPrice >= _trailingStopPrice)
			ClosePosition();
		}
	}

	private void EnterLong()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position < 0)
		BuyMarket(-Position);

		for (var i = 0; i < NumberOfTrades; i++)
		BuyMarket();
	}

	private void EnterShort()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position > 0)
		SellMarket(Position);

		for (var i = 0; i < NumberOfTrades; i++)
		SellMarket();
	}

	private void ClosePosition()
	{
		if (Position > 0)
		SellMarket(Position);
		else if (Position < 0)
		BuyMarket(-Position);
	}

	private decimal GetPipDistance(decimal pips)
	{
		return pips * _pipSize;
	}

	private decimal GetUnrealizedPnL(ICandleMessage candle)
	{
		if (Position == 0)
		return 0m;

		var entry = PositionAvgPrice;
		if (entry == 0m)
		return 0m;

		var diff = candle.ClosePrice - entry;
		return diff * Position;
	}

	private decimal GetPortfolioValue()
	{
		var portfolio = Portfolio;
		if (portfolio?.CurrentValue > 0m)
		return portfolio.CurrentValue;

		if (portfolio?.BeginValue > 0m)
		return portfolio.BeginValue;

		return 0m;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		_lastEntryPrice = PositionAvgPrice;

		if (Position == 0)
		{
			_trailingStopPrice = null;
			_breakEvenActivated = false;
			_profitTrailingActivated = false;
			_profitPeak = 0m;
		}
	}
}

