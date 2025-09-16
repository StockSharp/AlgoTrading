using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Larry Connors RSI-2 strategy with a 200-period SMA filter and optional stop management.
/// </summary>
public class LarryConnersRsi2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _fastSmaPeriod;
	private readonly StrategyParam<int> _slowSmaPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLongEntry;
	private readonly StrategyParam<decimal> _rsiShortEntry;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	/// <summary>
	/// Order volume for entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Fast SMA period used for timing exits.
	/// </summary>
	public int FastSmaPeriod
	{
		get => _fastSmaPeriod.Value;
		set => _fastSmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow SMA period used as a trend filter.
	/// </summary>
	public int SlowSmaPeriod
	{
		get => _slowSmaPeriod.Value;
		set => _slowSmaPeriod.Value = value;
	}

	/// <summary>
	/// RSI lookback length.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI threshold for long entries.
	/// </summary>
	public decimal RsiLongEntry
	{
		get => _rsiLongEntry.Value;
		set => _rsiLongEntry.Value = value;
	}

	/// <summary>
	/// RSI threshold for short entries.
	/// </summary>
	public decimal RsiShortEntry
	{
		get => _rsiShortEntry.Value;
		set => _rsiShortEntry.Value = value;
	}

	/// <summary>
	/// Enables stop-loss handling in price pips.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop-loss size expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Enables take-profit handling in price pips.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Take-profit size expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Timeframe used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="LarryConnersRsi2Strategy"/>.
	/// </summary>
	public LarryConnersRsi2Strategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume", "Trading")
			.SetCanOptimize(true);

		_fastSmaPeriod = Param(nameof(FastSmaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA Period", "Fast SMA length", "Indicators")
			.SetCanOptimize(true);

		_slowSmaPeriod = Param(nameof(SlowSmaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA Period", "Slow SMA length", "Indicators")
			.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI lookback", "Indicators")
			.SetCanOptimize(true);

		_rsiLongEntry = Param(nameof(RsiLongEntry), 6m)
			.SetDisplay("RSI Long Entry", "RSI threshold for longs", "Signals")
			.SetCanOptimize(true);

		_rsiShortEntry = Param(nameof(RsiShortEntry), 95m)
			.SetDisplay("RSI Short Entry", "RSI threshold for shorts", "Signals")
			.SetCanOptimize(true);

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable stop-loss management", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
			.SetCanOptimize(true);

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Enable take-profit management", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 60m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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

		// Clear internal state between runs.
		_pipSize = 0m;
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Use configured trade volume for default order size.
		Volume = TradeVolume;

		// Pre-compute pip size multiplier for risk management calculations.
		var priceStep = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;
		var pipMultiplier = decimals is 1 or 3 or 5 ? 10m : 1m;
		_pipSize = priceStep * pipMultiplier;
		if (_pipSize <= 0m)
			_pipSize = priceStep;

		// Prepare technical indicators.
		var fastSma = new SimpleMovingAverage { Length = FastSmaPeriod };
		var slowSma = new SimpleMovingAverage { Length = SlowSmaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		// Subscribe to candles and bind indicators for combined processing.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, slowSma, rsi, ProcessCandle)
			.Start();

		// Build optional chart visuals to monitor the strategy.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastSma, decimal slowSma, decimal rsi)
	{
		// Act only on fully formed candles to mimic MQL bar-close execution.
		if (candle.State != CandleStates.Finished)
			return;

		// Manage open long position exits before generating new signals.
		if (Position > 0)
		{
			if (UseStopLoss && _longEntryPrice.HasValue)
			{
				var stopPrice = _longEntryPrice.Value - StopLossPips * _pipSize;
				if (candle.LowPrice <= stopPrice)
				{
					SellMarket(Math.Abs(Position));
					ResetLongState();
					return;
				}
			}

			if (UseTakeProfit && _longEntryPrice.HasValue)
			{
				var targetPrice = _longEntryPrice.Value + TakeProfitPips * _pipSize;
				if (candle.HighPrice >= targetPrice)
				{
					SellMarket(Math.Abs(Position));
					ResetLongState();
					return;
				}
			}

			if (candle.ClosePrice > fastSma)
			{
				SellMarket(Math.Abs(Position));
				ResetLongState();
				return;
			}
		}
		else if (Position < 0)
		{
			if (UseStopLoss && _shortEntryPrice.HasValue)
			{
				var stopPrice = _shortEntryPrice.Value + StopLossPips * _pipSize;
				if (candle.HighPrice >= stopPrice)
				{
					BuyMarket(Math.Abs(Position));
					ResetShortState();
					return;
				}
			}

			if (UseTakeProfit && _shortEntryPrice.HasValue)
			{
				var targetPrice = _shortEntryPrice.Value - TakeProfitPips * _pipSize;
				if (candle.LowPrice <= targetPrice)
				{
					BuyMarket(Math.Abs(Position));
					ResetShortState();
					return;
				}
			}

			if (candle.ClosePrice < fastSma)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Generate new entries only when flat to match the MQL logic.
		if (Position == 0)
		{
			var canGoLong = rsi < RsiLongEntry && candle.ClosePrice > slowSma;
			if (canGoLong)
			{
				BuyMarket(TradeVolume);
				_longEntryPrice = candle.ClosePrice;
				_shortEntryPrice = null;
				return;
			}

			var canGoShort = rsi > RsiShortEntry && candle.ClosePrice < slowSma;
			if (canGoShort)
			{
				SellMarket(TradeVolume);
				_shortEntryPrice = candle.ClosePrice;
				_longEntryPrice = null;
			}
		}
	}

	private void ResetLongState()
	{
		// Drop long tracking data after an exit.
		_longEntryPrice = null;
	}

	private void ResetShortState()
	{
		// Drop short tracking data after an exit.
		_shortEntryPrice = null;
	}
}
