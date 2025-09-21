using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// StockSharp port of the MetaTrader "SimpleTrade" expert advisor.
/// Compares the opening price of the current bar with the bar from several periods ago and flips the position accordingly.
/// </summary>
public class SimpleTradeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<int> _lookbackBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _openHistory = new();
	private DateTimeOffset _lastProcessedOpenTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="SimpleTradeStrategy"/> class.
	/// </summary>
	public SimpleTradeStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order size in lots", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 120m)
			.SetNotNegative()
			.SetDisplay("Stop-Loss Points", "Protective stop distance expressed in instrument points", "Risk");

		_lookbackBars = Param(nameof(LookbackBars), 3)
			.SetGreaterOrEqual(1)
			.SetDisplay("Lookback Bars", "Number of bars used for the open price comparison", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for signal calculations", "General");
	}

	/// <summary>
	/// Order size submitted with each entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Number of historical bars used for the open price comparison.
	/// </summary>
	public int LookbackBars
	{
		get => _lookbackBars.Value;
		set => _lookbackBars.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Candle type that defines the working timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_openHistory.Clear();
		_lastProcessedOpenTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 0m;
		Unit? stopLossUnit = null;

		if (StopLossPoints > 0m && step > 0m)
			stopLossUnit = new Unit(StopLossPoints * step, UnitTypes.Absolute);

		StartProtection(stopLoss: stopLossUnit);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State == CandleStates.Finished)
		{
			// Store the open price of the finished candle for future comparisons.
			_openHistory.Add(candle.OpenPrice);

			var maxHistory = Math.Max(LookbackBars + 5, 5);
			if (_openHistory.Count > maxHistory)
				_openHistory.RemoveRange(0, _openHistory.Count - maxHistory);

			return;
		}

		if (candle.State != CandleStates.Active)
			return;

		// Run the entry logic only once per freshly opened bar.
		if (candle.OpenTime <= _lastProcessedOpenTime)
			return;

		_lastProcessedOpenTime = candle.OpenTime;

		var lookback = LookbackBars;
		if (_openHistory.Count < lookback)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		var currentOpen = candle.OpenPrice;
		var referenceOpen = _openHistory[^lookback];

		// Close any existing position before flipping to the new direction.
		if (Position > 0m)
			SellMarket(Position);
		else if (Position < 0m)
			BuyMarket(Math.Abs(Position));

		if (currentOpen > referenceOpen)
		{
			// New bar opened higher than the reference bar -> go long.
			BuyMarket(volume);
		}
		else
		{
			// Otherwise the open is equal or lower -> go short.
			SellMarket(volume);
		}
	}
}
