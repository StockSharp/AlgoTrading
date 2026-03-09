namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// ValidateMe strategy: RSI crossover at oversold/overbought levels.
/// Buys when RSI crosses above 30, sells when RSI crosses below 70.
/// </summary>
public class ValidateMeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;

	private decimal _prevRsi;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }

	public ValidateMeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_period = Param(nameof(Period), 14)
			.SetGreaterThanZero()
			.SetDisplay("Period", "RSI period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_hasPrev = false;
		var rsi = new RelativeStrengthIndex { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_hasPrev)
		{
			if (_prevRsi < 30 && rsiValue >= 30 && Position <= 0)
				BuyMarket();
			else if (_prevRsi > 70 && rsiValue <= 70 && Position >= 0)
				SellMarket();
		}

		_prevRsi = rsiValue;
		_hasPrev = true;
	}
}
