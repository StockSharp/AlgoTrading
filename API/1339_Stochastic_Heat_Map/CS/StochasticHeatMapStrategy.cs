using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic Heat Map strategy.
/// Uses multiple stochastic oscillators averaged together for trend detection.
/// Simplified to use a single stochastic K/D crossover with SMA filter.
/// </summary>
public class StochasticHeatMapStrategy : Strategy
{
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevK;
	private decimal _prevD;
	private bool _prevReady;

	public int StochLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}

	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public StochasticHeatMapStrategy()
	{
		_stochLength = Param(nameof(StochLength), 14)
			.SetDisplay("Stochastic Length", "Stochastic oscillator period", "Parameters");

		_smaLength = Param(nameof(SmaLength), 50)
			.SetDisplay("SMA Length", "SMA trend filter length", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevReady = false;

		var stoch = new StochasticOscillator
		{
			K = { Length = StochLength },
			D = { Length = 3 },
		};

		var sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stoch, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stochTyped = (StochasticOscillatorValue)stochValue;
		if (stochTyped.K is not decimal k || stochTyped.D is not decimal d)
			return;

		var smaVal = smaValue.IsFormed ? smaValue.GetValue<decimal>() : (decimal?)null;
		if (smaVal == null)
			return;

		var close = candle.ClosePrice;

		if (_prevReady)
		{
			// K crosses above D in oversold zone + price above SMA => buy
			if (_prevK <= _prevD && k > d && k < 30 && close > smaVal && Position <= 0)
			{
				BuyMarket();
			}
			// K crosses below D in overbought zone + price below SMA => sell
			else if (_prevK >= _prevD && k < d && k > 70 && close < smaVal && Position >= 0)
			{
				SellMarket();
			}
		}

		_prevK = k;
		_prevD = d;
		_prevReady = true;
	}
}
