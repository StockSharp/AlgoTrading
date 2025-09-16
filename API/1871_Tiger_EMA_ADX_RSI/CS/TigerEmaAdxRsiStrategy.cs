using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy using EMA crossover with ADX and RSI filters.
/// </summary>
public class TigerEmaAdxRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _rsiLower;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _takePrice;
	private decimal _stopPrice;

	/// <summary>
	/// Fast EMA period (default: 21).
	/// </summary>
	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }

	/// <summary>
	/// Slow EMA period (default: 89).
	/// </summary>
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }

	/// <summary>
	/// ADX calculation period (default: 14).
	/// </summary>
	public int AdxPeriod { get => _adxPeriod.Value; set => _adxPeriod.Value = value; }

	/// <summary>
	/// Minimum ADX value to allow trading (default: 27).
	/// </summary>
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }

	/// <summary>
	/// RSI calculation period (default: 14).
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// Upper RSI bound for long entries (default: 65).
	/// </summary>
	public decimal RsiUpper { get => _rsiUpper.Value; set => _rsiUpper.Value = value; }

	/// <summary>
	/// Lower RSI bound for short entries (default: 35).
	/// </summary>
	public decimal RsiLower { get => _rsiLower.Value; set => _rsiLower.Value = value; }

	/// <summary>
	/// Take profit distance in price points (default: 0.0007).
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Stop loss distance in price points (default: 0.05).
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public TigerEmaAdxRsiStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 21)
			.SetDisplay("Fast EMA", "Fast EMA period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 89)
			.SetDisplay("Slow EMA", "Slow EMA period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "ADX calculation period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_adxThreshold = Param(nameof(AdxThreshold), 27m)
			.SetDisplay("ADX Threshold", "Minimum ADX value", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI calculation period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_rsiUpper = Param(nameof(RsiUpper), 65m)
			.SetDisplay("RSI Upper", "Upper RSI bound", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(60m, 80m, 5m);

		_rsiLower = Param(nameof(RsiLower), 35m)
			.SetDisplay("RSI Lower", "Lower RSI bound", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20m, 40m, 5m);

		_takeProfit = Param(nameof(TakeProfit), 0.0007m)
			.SetDisplay("Take Profit", "Take profit distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.0005m, 0.0015m, 0.0002m);

		_stopLoss = Param(nameof(StopLoss), 0.05m)
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.02m, 0.1m, 0.01m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var fastEma = new ExponentialMovingAverage { Length = FastMaPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowMaPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, adx, rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal adx, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var trendUp = fast > slow;
		var trendDown = fast < slow;
		var canTrade = adx > AdxThreshold && rsi > RsiLower && rsi < RsiUpper;

		if (Position == 0)
		{
		if (canTrade)
		{
		if (trendUp)
		{
		BuyMarket();
		_takePrice = candle.ClosePrice + TakeProfit;
		_stopPrice = candle.ClosePrice - StopLoss;
		}
		else if (trendDown)
		{
		SellMarket();
		_takePrice = candle.ClosePrice - TakeProfit;
		_stopPrice = candle.ClosePrice + StopLoss;
		}
		}
		}
		else if (Position > 0)
		{
		if (candle.ClosePrice >= _takePrice || candle.ClosePrice <= _stopPrice)
		SellMarket();
		}
		else if (Position < 0)
		{
		if (candle.ClosePrice <= _takePrice || candle.ClosePrice >= _stopPrice)
		BuyMarket();
		}
	}
}
