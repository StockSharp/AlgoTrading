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
/// MACD and stochastic based swing strategy.
/// Buys when MACD turns up in negative zone + stochastic is oversold.
/// Sells when MACD turns down in positive zone + stochastic is overbought.
/// </summary>
public class MacdStochastic2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _oversoldThreshold;
	private readonly StrategyParam<decimal> _overboughtThreshold;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _macdPrev1;
	private decimal _macdPrev2;
	private int _macdCount;

	public decimal OversoldThreshold { get => _oversoldThreshold.Value; set => _oversoldThreshold.Value = value; }
	public decimal OverboughtThreshold { get => _overboughtThreshold.Value; set => _overboughtThreshold.Value = value; }
	public int MacdFastPeriod { get => _macdFastPeriod.Value; set => _macdFastPeriod.Value = value; }
	public int MacdSlowPeriod { get => _macdSlowPeriod.Value; set => _macdSlowPeriod.Value = value; }
	public int StochasticKPeriod { get => _stochasticKPeriod.Value; set => _stochasticKPeriod.Value = value; }
	public int StochasticDPeriod { get => _stochasticDPeriod.Value; set => _stochasticDPeriod.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdStochastic2Strategy()
	{
		_oversoldThreshold = Param(nameof(OversoldThreshold), 20m)
			.SetDisplay("Oversold", "Stochastic oversold threshold", "Stochastic");

		_overboughtThreshold = Param(nameof(OverboughtThreshold), 80m)
			.SetDisplay("Overbought", "Stochastic overbought threshold", "Stochastic");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length for MACD", "MACD");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length for MACD", "MACD");

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic K", "Lookback for %K", "Stochastic");

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic D", "Smoothing for %D", "Stochastic");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_macdPrev1 = 0m;
		_macdPrev2 = 0m;
		_macdCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var macd = new MovingAverageConvergenceDivergence(
			new ExponentialMovingAverage { Length = MacdSlowPeriod },
			new ExponentialMovingAverage { Length = MacdFastPeriod });

		var rsi = new RelativeStrengthIndex { Length = StochasticKPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, (candle, rsiValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				// Process MACD manually
				var macdResult = macd.Process(candle.ClosePrice, candle.CloseTime, true);
				if (!macd.IsFormed)
					return;

				var macdValue = macdResult.ToDecimal();

				_macdCount++;
				if (_macdCount < 3)
				{
					_macdPrev2 = _macdPrev1;
					_macdPrev1 = macdValue;
					return;
				}

				var macd0 = macdValue;
				var macd1 = _macdPrev1;
				var macd2 = _macdPrev2;

				// Buy: MACD in negative zone turning up + stochastic oversold
				var longSignal = macd0 < 0m && macd1 < 0m && macd2 < 0m &&
					macd0 > macd1 && macd1 < macd2 &&
					rsiValue < OversoldThreshold;

				// Sell: MACD in positive zone turning down + stochastic overbought
				var shortSignal = macd0 > 0m && macd1 > 0m && macd2 > 0m &&
					macd0 < macd1 && macd1 > macd2 &&
					rsiValue > OverboughtThreshold;

				if (longSignal && Position <= 0)
					BuyMarket();
				else if (shortSignal && Position >= 0)
					SellMarket();

				_macdPrev2 = _macdPrev1;
				_macdPrev1 = macdValue;
			})
			.Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Absolute),
			new Unit(StopLoss, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}
}
