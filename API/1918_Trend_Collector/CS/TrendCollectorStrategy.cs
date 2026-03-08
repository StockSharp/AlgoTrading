using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy with stochastic and EMA crossover.
/// Buys when fast EMA above slow EMA and stochastic oversold.
/// Sells when fast EMA below slow EMA and stochastic overbought.
/// </summary>
public class TrendCollectorStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<decimal> _stochasticUpper;
	private readonly StrategyParam<decimal> _stochasticLower;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastMa;
	private ExponentialMovingAverage _slowMa;
	private StochasticOscillator _stochastic;

	public int FastMaLength { get => _fastMaLength.Value; set => _fastMaLength.Value = value; }
	public int SlowMaLength { get => _slowMaLength.Value; set => _slowMaLength.Value = value; }
	public decimal StochasticUpper { get => _stochasticUpper.Value; set => _stochasticUpper.Value = value; }
	public decimal StochasticLower { get => _stochasticLower.Value; set => _stochasticLower.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrendCollectorStrategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Fast EMA length", "Parameters");

		_slowMaLength = Param(nameof(SlowMaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Slow EMA length", "Parameters");

		_stochasticUpper = Param(nameof(StochasticUpper), 60m)
			.SetDisplay("Stochastic Upper", "Upper stochastic level", "Parameters");

		_stochasticLower = Param(nameof(StochasticLower), 40m)
			.SetDisplay("Stochastic Lower", "Lower stochastic level", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		_fastMa = default;
		_slowMa = default;
		_stochastic = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastMa = new ExponentialMovingAverage { Length = FastMaLength };
		_slowMa = new ExponentialMovingAverage { Length = SlowMaLength };
		_stochastic = new StochasticOscillator
		{
			K = { Length = 14 },
			D = { Length = 3 },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastResult = _fastMa.Process(candle.ClosePrice, candle.OpenTime, true);
		var slowResult = _slowMa.Process(candle.ClosePrice, candle.OpenTime, true);

		if (!fastResult.IsFormed || !slowResult.IsFormed || !stochValue.IsFormed)
			return;

		var fast = fastResult.ToDecimal();
		var slow = slowResult.ToDecimal();

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal stochK)
			return;

		// Buy: fast EMA above slow EMA, stochastic oversold
		if (Position <= 0 && fast > slow && stochK < StochasticLower)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Sell: fast EMA below slow EMA, stochastic overbought
		else if (Position >= 0 && fast < slow && stochK > StochasticUpper)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}
