using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on "Three White Soldiers" candlestick pattern.
/// This strategy looks for three consecutive bullish candles with
/// closing prices higher than previous candle, indicating a strong uptrend.
/// </summary>
public class ThreeWhiteSoldiersStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _maLength;

	private ICandleMessage _firstCandle;
	private ICandleMessage _secondCandle;
	private ICandleMessage _currentCandle;

	/// <summary>
	/// Candle type and timeframe for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop-loss percent from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Moving average length for exit signal.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ThreeWhiteSoldiersStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						.SetDisplay("Candle Type", "Type of candles for strategy calculation", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
							.SetRange(0.1m, 5m)
							.SetDisplay("Stop Loss %", "Stop loss as percentage below low of pattern", "Risk Management");

		_maLength = Param(nameof(MaLength), 20)
					.SetRange(10, 50)
					.SetDisplay("MA Length", "Period of moving average for exit signal", "Indicators");
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

				_firstCandle = null;
				_secondCandle = null;
				_currentCandle = null;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
				base.OnStarted(time);

				// Create a simple moving average indicator for exit signal
				var ma = new SimpleMovingAverage { Length = MaLength };

	// Create subscription and bind to process candles
	var subscription = SubscribeCandles(CandleType);
	subscription
		.Bind(ma, ProcessCandle)
		.Start();

	// Setup protection with stop loss
	StartProtection(
		takeProfit: null,
		stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
		isStopTrailing: false
	);

	// Setup chart visualization if available
	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, subscription);
		DrawIndicator(area, ma);
		DrawOwnTrades(area);
	}
}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Shift candles
		_firstCandle = _secondCandle;
		_secondCandle = _currentCandle;
		_currentCandle = candle;

		// Check if we have enough candles to analyze
		if (_firstCandle == null || _secondCandle == null || _currentCandle == null)
			return;

		// Check for "Three White Soldiers" pattern
		var isWhiteSoldiers = 
			// First candle is bullish
			_firstCandle.OpenPrice < _firstCandle.ClosePrice &&
			// Second candle is bullish
			_secondCandle.OpenPrice < _secondCandle.ClosePrice &&
			// Third candle is bullish
			_currentCandle.OpenPrice < _currentCandle.ClosePrice &&
			// Each close is higher than previous
			_currentCandle.ClosePrice > _secondCandle.ClosePrice && 
			_secondCandle.ClosePrice > _firstCandle.ClosePrice;

		// Check for long entry condition
		if (isWhiteSoldiers && Position == 0)
		{
			LogInfo("Three White Soldiers pattern detected. Going long.");
			BuyMarket(Volume);
		}
		// Check for exit condition
		else if (Position > 0 && candle.ClosePrice < maValue)
		{
			LogInfo("Price fell below MA. Exiting long position.");
			SellMarket(Math.Abs(Position));
		}
	}
}