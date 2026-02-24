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
/// ABC trading strategy using moving averages and RSI for signal.
/// Enters long when price is above all averages and RSI crosses above midpoint.
/// Enters short when price is below all averages and RSI crosses below midpoint.
/// </summary>
public class TradingABCStrategy : Strategy
{
	private readonly StrategyParam<int> _sma1Length;
	private readonly StrategyParam<int> _sma2Length;
	private readonly StrategyParam<int> _ema1Length;
	private readonly StrategyParam<int> _ema2Length;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private decimal _lastLow;
	private decimal _lastHigh;

	public int Sma1Length { get => _sma1Length.Value; set => _sma1Length.Value = value; }
	public int Sma2Length { get => _sma2Length.Value; set => _sma2Length.Value = value; }
	public int Ema1Length { get => _ema1Length.Value; set => _ema1Length.Value = value; }
	public int Ema2Length { get => _ema2Length.Value; set => _ema2Length.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TradingABCStrategy()
	{
		_sma1Length = Param(nameof(Sma1Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("SMA1 Length", "Length of first SMA", "Trend");

		_sma2Length = Param(nameof(Sma2Length), 100)
			.SetGreaterThanZero()
			.SetDisplay("SMA2 Length", "Length of second SMA", "Trend");

		_ema1Length = Param(nameof(Ema1Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA1 Length", "Length of first EMA", "Trend");

		_ema2Length = Param(nameof(Ema2Length), 40)
			.SetGreaterThanZero()
			.SetDisplay("EMA2 Length", "Length of second EMA", "Trend");

		_rsiLength = Param(nameof(RsiLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Length of RSI oscillator", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for calculations", "General");
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
		_prevRsi = 0m;
		_lastLow = 0m;
		_lastHigh = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma1 = new SimpleMovingAverage { Length = Sma1Length };
		var sma2 = new SimpleMovingAverage { Length = Sma2Length };
		var ema1 = new ExponentialMovingAverage { Length = Ema1Length };
		var ema2 = new ExponentialMovingAverage { Length = Ema2Length };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma1, sma2, ema1, ema2, rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma1);
			DrawIndicator(area, sma2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sma1Val, decimal sma2Val, decimal ema1Val, decimal ema2Val, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// Exit checks first (stop at previous bar extreme)
		if (Position > 0 && _lastLow > 0 && close < _lastLow)
		{
			SellMarket();
			_lastLow = candle.LowPrice;
			_lastHigh = candle.HighPrice;
			_prevRsi = rsiVal;
			return;
		}
		else if (Position < 0 && _lastHigh > 0 && close > _lastHigh)
		{
			BuyMarket();
			_lastLow = candle.LowPrice;
			_lastHigh = candle.HighPrice;
			_prevRsi = rsiVal;
			return;
		}

		_lastLow = candle.LowPrice;
		_lastHigh = candle.HighPrice;

		if (_prevRsi == 0)
		{
			_prevRsi = rsiVal;
			return;
		}

		var upTrend = close > sma1Val && close > sma2Val && close > ema1Val && close > ema2Val;
		var downTrend = close < sma1Val && close < sma2Val && close < ema1Val && close < ema2Val;

		// RSI crossover as entry signal (replacing stochastic K/D cross)
		if (upTrend && _prevRsi <= 50 && rsiVal > 50 && Position <= 0)
		{
			BuyMarket();
		}
		else if (downTrend && _prevRsi >= 50 && rsiVal < 50 && Position >= 0)
		{
			SellMarket();
		}

		_prevRsi = rsiVal;
	}
}
