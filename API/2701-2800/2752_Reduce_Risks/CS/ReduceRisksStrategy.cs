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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy converted from the "Reduce risks" MQL5 expert.
/// Uses SMA hierarchy (short/medium/long) for trend detection with risk control exits.
/// Enters on confirmed SMA crossover, exits on reverse cross or stop/take profit.
/// </summary>
public class ReduceRisksStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<decimal> _initialDeposit;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _smaShort;
	private SimpleMovingAverage _smaMedium;
	private SimpleMovingAverage _smaLong;

	private decimal? _smaShortCurr;
	private decimal? _smaShortPrev;
	private decimal? _smaMediumCurr;
	private decimal? _smaMediumPrev;
	private decimal? _smaLongCurr;
	private decimal? _smaLongPrev;

	private decimal _riskThreshold;
	private int _riskExceededCounter;
	private int _barsSinceEntry;
	private decimal _entryPrice;
	private int _barsShortAboveMedium;
	private int _barsShortBelowMedium;
	private bool _enteredLong;
	private bool _enteredShort;

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Reference initial deposit used for equity based risk limitation.
	/// </summary>
	public decimal InitialDeposit
	{
		get => _initialDeposit.Value;
		set => _initialDeposit.Value = value;
	}

	/// <summary>
	/// Percentage of the initial deposit allowed to be lost before new entries are blocked.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Candle timeframe for trading.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ReduceRisksStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 30)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Protective stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 60)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Target distance in pips", "Risk");

		_initialDeposit = Param(nameof(InitialDeposit), 1000000m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Deposit", "Reference equity for drawdown protection", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 5m)
			.SetRange(0m, 100m)
			.SetDisplay("Risk Percent", "Maximum loss allowed relative to the initial deposit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Timeframe", "Trading timeframe", "Timeframes");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_smaShort = null;
		_smaMedium = null;
		_smaLong = null;

		_smaShortCurr = null;
		_smaShortPrev = null;
		_smaMediumCurr = null;
		_smaMediumPrev = null;
		_smaLongCurr = null;
		_smaLongPrev = null;

		_riskThreshold = 0m;
		_riskExceededCounter = 0;
		_barsSinceEntry = 0;
		_entryPrice = 0m;
		_barsShortAboveMedium = 0;
		_barsShortBelowMedium = 0;
		_enteredLong = false;
		_enteredShort = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_riskThreshold = InitialDeposit * (100m - RiskPercent) / 100m;

		// SMA periods: ~2h / ~6h / ~12h on 5-min candles
		_smaShort = new SimpleMovingAverage { Length = 24 };
		_smaMedium = new SimpleMovingAverage { Length = 72 };
		_smaLong = new SimpleMovingAverage { Length = 144 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _smaShort);
			DrawIndicator(area, _smaMedium);
			DrawIndicator(area, _smaLong);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_smaShort is null || _smaMedium is null || _smaLong is null)
			return;

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		UpdateSma(_smaShort, typical, candle.OpenTime, ref _smaShortCurr, ref _smaShortPrev);
		UpdateSma(_smaMedium, typical, candle.OpenTime, ref _smaMediumCurr, ref _smaMediumPrev);
		UpdateSma(_smaLong, typical, candle.OpenTime, ref _smaLongCurr, ref _smaLongPrev);

		if (_smaShortCurr is not decimal smaS ||
			_smaMediumCurr is not decimal smaM ||
			_smaLongCurr is not decimal smaL)
			return;

		// Track consecutive bars of SMA position
		if (smaS > smaM)
		{
			_barsShortAboveMedium++;
			_barsShortBelowMedium = 0;
		}
		else
		{
			_barsShortBelowMedium++;
			_barsShortAboveMedium = 0;
		}

		// Risk check
		var equity = Portfolio?.CurrentValue ?? InitialDeposit;
		var riskExceeded = equity <= _riskThreshold && InitialDeposit > 0m;

		if (riskExceeded)
		{
			if (_riskExceededCounter < 15)
			{
				LogWarning("Entry blocked. Risk limit of {0}% reached (equity={1:0.##}).", RiskPercent, equity);
				_riskExceededCounter++;
			}
		}
		else
		{
			_riskExceededCounter = 0;
		}

		// When SMA crosses in opposite direction, allow new entry of that type
		if (_barsShortBelowMedium >= 72)
			_enteredLong = false;
		if (_barsShortAboveMedium >= 72)
			_enteredShort = false;

		if (Position == 0 && !riskExceeded)
		{
			// LONG: short crosses above medium, not already entered on this cross
			if (_barsShortAboveMedium == 1 && candle.ClosePrice > smaS && !_enteredLong)
			{
				BuyMarket();
				_barsSinceEntry = 0;
				_enteredLong = true;
			}
			// SHORT: short crosses below medium, not already entered on this cross
			else if (_barsShortBelowMedium == 1 && candle.ClosePrice < smaS && !_enteredShort)
			{
				SellMarket();
				_barsSinceEntry = 0;
				_enteredShort = true;
			}
		}
		else if (Position != 0)
		{
			_barsSinceEntry++;

			if (Position > 0)
			{
				var entryPrice = _entryPrice;
				// Exit on reverse cross after min hold
				var reverseCross = _barsShortBelowMedium >= 3 && _barsSinceEntry >= 30;
				// Stop loss: 4%
				var stopLoss = entryPrice > 0 && candle.ClosePrice < entryPrice * 0.96m;
				// Take profit: 6%
				var takeProfit = entryPrice > 0 && candle.ClosePrice > entryPrice * 1.06m;

				if (reverseCross || stopLoss || takeProfit || riskExceeded)
				{
					SellMarket(Position.Abs());
				}
			}
			else if (Position < 0)
			{
				var entryPrice = _entryPrice;
				// Exit on reverse cross after min hold
				var reverseCross = _barsShortAboveMedium >= 3 && _barsSinceEntry >= 30;
				// Stop loss: 4%
				var stopLoss = entryPrice > 0 && candle.ClosePrice > entryPrice * 1.04m;
				// Take profit: 6%
				var takeProfit = entryPrice > 0 && candle.ClosePrice < entryPrice * 0.94m;

				if (reverseCross || stopLoss || takeProfit || riskExceeded)
				{
					BuyMarket(Position.Abs());
				}
			}
		}

		if (Position == 0)
		{
			_entryPrice = 0m;
			_barsSinceEntry = 0;
		}
	}

	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		if (trade?.Trade == null) return;
		if (Position != 0 && _entryPrice == 0m)
			_entryPrice = trade.Trade.Price;
	}

	private void UpdateSma(SimpleMovingAverage sma, decimal input, DateTimeOffset time, ref decimal? curr, ref decimal? prev)
	{
		var indicatorValue = sma.Process(new DecimalIndicatorValue(sma, input, time.UtcDateTime) { IsFinal = true });
		if (!sma.IsFormed || indicatorValue is not DecimalIndicatorValue decimalValue)
			return;

		prev = curr;
		curr = decimalValue.Value;
	}
}
