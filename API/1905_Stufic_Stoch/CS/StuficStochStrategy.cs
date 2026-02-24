using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on fast/slow moving averages and Stochastic oscillator.
/// Buys when %K crosses above %D below oversold level while trend is up.
/// Sells when %K crosses below %D above overbought level while trend is down.
/// </summary>
public class StuficStochStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastMa;
	private SimpleMovingAverage _slowMa;

	private decimal _prevK;
	private decimal _prevD;
	private bool _isFirst = true;

	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }
	public int StochKPeriod { get => _stochKPeriod.Value; set => _stochKPeriod.Value = value; }
	public int StochDPeriod { get => _stochDPeriod.Value; set => _stochDPeriod.Value = value; }
	public decimal OverboughtLevel { get => _overboughtLevel.Value; set => _overboughtLevel.Value = value; }
	public decimal OversoldLevel { get => _oversoldLevel.Value; set => _oversoldLevel.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public StuficStochStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast moving average period", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow moving average period", "Indicators");

		_stochKPeriod = Param(nameof(StochKPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stoch %K", "%K period for Stochastic", "Indicators");

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stoch %D", "%D period for Stochastic", "Indicators");

		_overboughtLevel = Param(nameof(OverboughtLevel), 80m)
			.SetDisplay("Overbought", "Overbought level", "Trading");

		_oversoldLevel = Param(nameof(OversoldLevel), 20m)
			.SetDisplay("Oversold", "Oversold level", "Trading");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevK = 0;
		_prevD = 0;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastMa = new SimpleMovingAverage { Length = FastMaPeriod };
		_slowMa = new SimpleMovingAverage { Length = SlowMaPeriod };

		var stochastic = new StochasticOscillator();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			takeProfit: null
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// process MAs manually
		var fastResult = _fastMa.Process(candle.ClosePrice, candle.OpenTime, true);
		var slowResult = _slowMa.Process(candle.ClosePrice, candle.OpenTime, true);

		if (!fastResult.IsFormed || !slowResult.IsFormed)
			return;

		var fast = fastResult.ToDecimal();
		var slow = slowResult.ToDecimal();

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		if (_isFirst)
		{
			_prevK = k;
			_prevD = d;
			_isFirst = false;
			return;
		}

		// Bullish: %K crosses above %D in oversold zone, trend up
		if (_prevK <= _prevD && k > d && k < OversoldLevel && fast > slow && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Bearish: %K crosses below %D in overbought zone, trend down
		else if (_prevK >= _prevD && k < d && k > OverboughtLevel && fast < slow && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevK = k;
		_prevD = d;
	}
}
