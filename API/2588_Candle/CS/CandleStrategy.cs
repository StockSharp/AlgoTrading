using System;

using Ecng.Common;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Candle color reversal strategy with pip-based protection and trade cooldown.
/// </summary>
public class CandleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _minBars;
	private readonly StrategyParam<TimeSpan> _tradeCooldown;

	private decimal _pipSize;
	private int _finishedCandles;
	private DateTimeOffset _nextAllowedTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="CandleStrategy"/>.
	/// </summary>
	public CandleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for candle evaluation", "General");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetDisplay("Take Profit (pips)", "Distance to take profit in pips", "Risk")
			.SetGreaterThanZero();

		_trailingStopPips = Param(nameof(TrailingStopPips), 30m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
			.SetGreaterThanZero();

		_minBars = Param(nameof(MinBars), 26)
			.SetDisplay("Minimum Bars", "History length required before trading", "General")
			.SetGreaterThanZero();

		_tradeCooldown = Param(nameof(TradeCooldown), TimeSpan.FromSeconds(10))
			.SetDisplay("Trade Cooldown", "Waiting time after each trade", "Risk");
	}

	/// <summary>
	/// Candle type and timeframe used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum number of bars required on the chart.
	/// </summary>
	public int MinBars
	{
		get => _minBars.Value;
		set => _minBars.Value = value;
	}

	/// <summary>
	/// Cooldown between consecutive trading operations.
	/// </summary>
	public TimeSpan TradeCooldown
	{
		get => _tradeCooldown.Value;
		set => _tradeCooldown.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_finishedCandles = 0;
		_nextAllowedTime = DateTimeOffset.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = (Security?.PriceStep ?? 1m) * 10m;

		Unit? takeProfit = TakeProfitPips > 0m && _pipSize > 0m
			? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute)
			: null;

		Unit? trailingStop = TrailingStopPips > 0m && _pipSize > 0m
			? new Unit(TrailingStopPips * _pipSize, UnitTypes.Absolute)
			: null;

		// Enable automatic protective orders and trailing stop handling.
		StartProtection(
			takeProfit: takeProfit,
			stopLoss: trailingStop,
			isStopTrailing: trailingStop != null,
			useMarketOrders: true);

		// Subscribe to candle data for the configured timeframe.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		// Draw candles and executions on the chart if visualization is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Skip unfinished candles to work only with final prices.
		if (candle.State != CandleStates.Finished)
			return;

		_finishedCandles++;

		// Ensure the environment and indicators are ready for trading.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Wait until the chart has enough historical data.
		if (_finishedCandles < MinBars * 2)
			return;

		var time = candle.CloseTime;

		// Enforce cooldown between trading operations.
		if (time < _nextAllowedTime)
			return;

		var isBullish = candle.ClosePrice > candle.OpenPrice;
		var isBearish = candle.ClosePrice < candle.OpenPrice;

		var tradeExecuted = false;

		if (Position > 0 && isBearish)
		{
			// Reverse from long to short when a bearish candle appears.
			var volume = Math.Abs(Position) + Volume;
			if (volume > 0m)
			{
				SellMarket(volume);
				tradeExecuted = true;
			}
		}
		else if (Position < 0 && isBullish)
		{
			// Reverse from short to long when a bullish candle appears.
			var volume = Math.Abs(Position) + Volume;
			if (volume > 0m)
			{
				BuyMarket(volume);
				tradeExecuted = true;
			}
		}
		else if (Position == 0)
		{
			if (isBullish)
			{
				// Enter long on bullish close.
				if (Volume > 0m)
				{
					BuyMarket(Volume);
					tradeExecuted = true;
				}
			}
			else if (isBearish)
			{
				// Enter short on bearish close.
				if (Volume > 0m)
				{
					SellMarket(Volume);
					tradeExecuted = true;
				}
			}
		}

		if (tradeExecuted)
		{
			_nextAllowedTime = time + TradeCooldown;
		}
	}
}
