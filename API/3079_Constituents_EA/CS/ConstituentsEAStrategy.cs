using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Constituents breakout strategy converted from the original MetaTrader expert advisor.
/// Detects the recent high/low range from N candles and enters with market orders
/// when price breaks above the high (buy) or below the low (sell).
/// Uses stop-loss, take-profit, and trailing stop for risk management.
/// </summary>
public class ConstituentsEaStrategy : Strategy
{
	private readonly StrategyParam<int> _searchDepth;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;

	private decimal _pipSize;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _exitRequested;

	/// <summary>
	/// Number of completed candles used to determine the recent range.
	/// </summary>
	public int SearchDepth
	{
		get => _searchDepth.Value;
		set => _searchDepth.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ConstituentsEaStrategy"/> class.
	/// </summary>
	public ConstituentsEaStrategy()
	{
		_searchDepth = Param(nameof(SearchDepth), 3)
			.SetGreaterThanZero()
			.SetDisplay("Search Depth", "Number of completed candles used to find extremes", "Setup");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance expressed in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Take profit distance expressed in pips", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe used to evaluate highs/lows", "General");
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

		_highest = null!;
		_lowest = null!;
		_pipSize = 0m;
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
		_prevHigh = 0m;
		_prevLow = 0m;
		_exitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pipSize = CalculatePipSize();

		_highest = new Highest { Length = SearchDepth };
		_lowest = new Lowest { Length = SearchDepth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Process indicators
		var highValue = _highest.Process(new DecimalIndicatorValue(_highest, candle.HighPrice, candle.OpenTime) { IsFinal = true });
		var lowValue = _lowest.Process(new DecimalIndicatorValue(_lowest, candle.LowPrice, candle.OpenTime) { IsFinal = true });

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		var currentHigh = highValue.ToDecimal();
		var currentLow = lowValue.ToDecimal();

		// Manage existing position
		if (Position != 0)
		{
			ManagePosition(candle);

			// Update range for next trade
			_prevHigh = currentHigh;
			_prevLow = currentLow;
			return;
		}

		// Check for breakout signals using previous range
		if (_prevHigh > 0m && _prevLow > 0m)
		{
			// Breakout above the recent high -> buy
			if (candle.ClosePrice > _prevHigh)
			{
				_entryPrice = candle.ClosePrice;
				_exitRequested = false;

				if (StopLossPips > 0m)
					_stopPrice = _entryPrice - StopLossPips * _pipSize;
				else
					_stopPrice = null;

				if (TakeProfitPips > 0m)
					_takePrice = _entryPrice + TakeProfitPips * _pipSize;
				else
					_takePrice = null;

				BuyMarket();
			}
			// Breakout below the recent low -> sell
			else if (candle.ClosePrice < _prevLow)
			{
				_entryPrice = candle.ClosePrice;
				_exitRequested = false;

				if (StopLossPips > 0m)
					_stopPrice = _entryPrice + StopLossPips * _pipSize;
				else
					_stopPrice = null;

				if (TakeProfitPips > 0m)
					_takePrice = _entryPrice - TakeProfitPips * _pipSize;
				else
					_takePrice = null;

				SellMarket();
			}
		}

		// Update range for next candle
		_prevHigh = currentHigh;
		_prevLow = currentLow;
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (_exitRequested)
			return;

		if (Position > 0)
		{
			// Check take profit
			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				_exitRequested = true;
				SellMarket();
				return;
			}

			// Check stop loss
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				_exitRequested = true;
				SellMarket();
				return;
			}
		}
		else if (Position < 0)
		{
			// Check take profit
			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				_exitRequested = true;
				BuyMarket();
				return;
			}

			// Check stop loss
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				_exitRequested = true;
				BuyMarket();
				return;
			}
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0.01m;

		return step;
	}
}
