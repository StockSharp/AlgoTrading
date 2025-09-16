using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy implementing a basic trailing stop with CCI and RSI signals.
/// </summary>
public class BasicTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BasicTrailingStopStrategy"/> class.
	/// </summary>
	public BasicTrailingStopStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Pips", "Trailing stop distance in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 50m, 5m);

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Commodity Channel Index period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 7);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Relative Strength Index period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 7);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		// Reset trailing stop level
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create indicators
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		// Subscribe to candles and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, rsi, ProcessCandle)
			.Start();

		// Prepare chart visuals
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}
	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal rsiValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure the strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = Security.Step ?? 1m;
		var stopOffset = StopLossPips * step;

		if (Position > 0)
		{
			// Update trailing stop for long position
			var newStop = candle.ClosePrice - stopOffset;
			if (newStop > _stopPrice)
				_stopPrice = newStop;

			// Exit if price hits trailing stop
			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				_stopPrice = 0m;
			}

			return;
		}

		if (Position < 0)
		{
			// Update trailing stop for short position
			var newStop = candle.ClosePrice + stopOffset;
			if (_stopPrice == 0m || newStop < _stopPrice)
				_stopPrice = newStop;

			// Exit if price hits trailing stop
			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = 0m;
			}

			return;
		}

		// No position - evaluate entry signals
		var longSignal = cciValue > -150m && cciValue <= -100m && rsiValue > 0m && rsiValue <= 30m;
		var shortSignal = cciValue > 100m && cciValue <= 250m && rsiValue > 70m && rsiValue <= 100m;

		if (longSignal)
		{
			BuyMarket(Volume);
			_stopPrice = candle.ClosePrice - stopOffset;
		}
		else if (shortSignal)
		{
			SellMarket(Volume);
			_stopPrice = candle.ClosePrice + stopOffset;
		}
	}
}
