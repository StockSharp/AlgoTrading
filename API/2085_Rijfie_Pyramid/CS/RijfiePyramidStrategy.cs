using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pyramid style strategy that buys additional lots as price drops by a fixed percentage.
/// Uses Stochastic oscillator oversold signal for initial entry.
/// </summary>
public class RijfiePyramidStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stepLevel;
	private readonly StrategyParam<decimal> _takeProfitPct;

	private StochasticOscillator _stochastic;
	private decimal _nextBuyPrice;
	private decimal? _prevK;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public decimal StepLevel
	{
		get => _stepLevel.Value;
		set => _stepLevel.Value = value;
	}

	public decimal TakeProfitPct
	{
		get => _takeProfitPct.Value;
		set => _takeProfitPct.Value = value;
	}

	public RijfiePyramidStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_lowLevel = Param(nameof(LowLevel), 20m)
			.SetDisplay("Stochastic Low", "Oversold threshold", "Parameters");

		_maPeriod = Param(nameof(MaPeriod), 10)
			.SetDisplay("EMA Period", "EMA length", "Parameters")
			.SetGreaterThanZero();

		_stepLevel = Param(nameof(StepLevel), 1m)
			.SetDisplay("Step Level", "Percent drop for next buy", "Parameters");

		_takeProfitPct = Param(nameof(TakeProfitPct), 2m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");
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
		_stochastic = default;
		_nextBuyPrice = 0;
		_prevK = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stochastic = new StochasticOscillator();
		var ema = new ExponentialMovingAverage { Length = MaPeriod };

		Indicators.Add(_stochastic);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, (candle, emaValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var stochResult = _stochastic.Process(candle);
				if (!stochResult.IsFormed)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var stochVal = (StochasticOscillatorValue)stochResult;
				if (stochVal.K is not decimal k)
					return;

				var price = candle.ClosePrice;

				// Initial buy when Stochastic crosses above the low level
				if (_prevK.HasValue && _prevK.Value < LowLevel && k >= LowLevel && Position == 0)
				{
					BuyMarket();
					_nextBuyPrice = price * (1m - StepLevel / 100m);
				}
				// Additional buys when price drops below the threshold but stays above EMA
				else if (Position > 0 && _nextBuyPrice > 0 && price <= _nextBuyPrice && price > emaValue)
				{
					BuyMarket();
					_nextBuyPrice = price * (1m - StepLevel / 100m);
				}

				// Exit on Stochastic overbought
				if (Position > 0 && k > 80m)
				{
					SellMarket();
					_nextBuyPrice = 0;
				}

				_prevK = k;
			})
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPct, UnitTypes.Percent),
			stopLoss: new Unit(TakeProfitPct * 2, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}
}
