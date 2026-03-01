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
/// Mean-reversion strategy with ATR volatility filter and drawdown control.
/// Simplified from the two-pair correlation EA to single security.
/// </summary>
public class TwoPairCorrelationStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maxDrawdownPercent;
	private readonly StrategyParam<decimal> _priceDifferenceThreshold;
	private readonly StrategyParam<decimal> _minimumTotalProfit;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private SimpleMovingAverage _sma;
	private decimal _atrValue;
	private decimal _entryPrice;
	private decimal _peakEquity;
	private bool _tradingPaused;

	/// <summary>
	/// Maximum drawdown percentage that pauses new entries.
	/// </summary>
	public decimal MaxDrawdownPercent
	{
		get => _maxDrawdownPercent.Value;
		set => _maxDrawdownPercent.Value = value;
	}

	/// <summary>
	/// Price deviation threshold from SMA for entry.
	/// </summary>
	public decimal PriceDifferenceThreshold
	{
		get => _priceDifferenceThreshold.Value;
		set => _priceDifferenceThreshold.Value = value;
	}

	/// <summary>
	/// Floating profit target for closing.
	/// </summary>
	public decimal MinimumTotalProfit
	{
		get => _minimumTotalProfit.Value;
		set => _minimumTotalProfit.Value = value;
	}

	/// <summary>
	/// ATR period for volatility filter.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for signals and ATR.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public TwoPairCorrelationStrategy()
	{
		_maxDrawdownPercent = Param(nameof(MaxDrawdownPercent), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Max Drawdown %", "Maximum drawdown before trading is paused", "Risk")
			.SetOptimize(5m, 50m, 5m);

		_priceDifferenceThreshold = Param(nameof(PriceDifferenceThreshold), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Price Deviation", "Distance from SMA required to enter", "Signals")
			.SetOptimize(1m, 20m, 1m);

		_minimumTotalProfit = Param(nameof(MinimumTotalProfit), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Target", "Floating profit required to close position", "Risk")
			.SetOptimize(1m, 10m, 1m);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Number of candles for volatility filter", "Indicators")
			.SetOptimize(5, 40, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series for signals", "Indicators");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_atr = null;
		_sma = null;
		_atrValue = 0m;
		_entryPrice = 0m;
		_peakEquity = 0m;
		_tradingPaused = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_peakEquity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_sma = new SimpleMovingAverage { Length = 20 };

		SubscribeCandles(CandleType)
			.Bind(_atr, _sma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormed)
			return;

		_atrValue = atrValue;
		var price = candle.ClosePrice;

		// Drawdown control
		UpdateDrawdownState();

		// Check profit target
		if (Position != 0 && _entryPrice > 0m)
		{
			var pnl = Position > 0
				? price - _entryPrice
				: _entryPrice - price;

			if (MinimumTotalProfit > 0m && pnl >= MinimumTotalProfit)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				else
					BuyMarket(Math.Abs(Position));

				_entryPrice = 0m;
				return;
			}
		}

		if (_tradingPaused)
			return;

		if (Position != 0)
			return;

		// Volatility filter: skip if ATR is too high
		if (_atrValue > PriceDifferenceThreshold * 2m)
			return;

		// Mean reversion entry
		var deviation = price - smaValue;

		if (deviation > PriceDifferenceThreshold)
		{
			// Price too far above SMA - sell expecting reversion
			SellMarket();
			_entryPrice = price;
		}
		else if (deviation < -PriceDifferenceThreshold)
		{
			// Price too far below SMA - buy expecting reversion
			BuyMarket();
			_entryPrice = price;
		}
	}

	private void UpdateDrawdownState()
	{
		if (Portfolio == null)
			return;

		var equity = Portfolio.CurrentValue ?? Portfolio.BeginValue ?? 0m;
		if (equity <= 0m)
			return;

		if (equity > _peakEquity)
			_peakEquity = equity;

		if (MaxDrawdownPercent <= 0m || _peakEquity <= 0m)
		{
			_tradingPaused = false;
			return;
		}

		var drawdown = (_peakEquity - equity) / _peakEquity * 100m;

		if (!_tradingPaused && drawdown >= MaxDrawdownPercent)
		{
			_tradingPaused = true;
		}
		else if (_tradingPaused && drawdown < MaxDrawdownPercent * 0.5m)
		{
			_tradingPaused = false;
		}
	}
}
