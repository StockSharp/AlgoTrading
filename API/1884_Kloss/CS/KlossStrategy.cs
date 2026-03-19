using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines Weighted Moving Average, CCI and Stochastic oscillator.
/// </summary>
public class KlossStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciLevel;
	private readonly StrategyParam<decimal> _stochLevel;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private int _previousSignal;
	private int _cooldownRemaining;

	/// <summary>Moving Average period.</summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	/// <summary>CCI calculation period.</summary>
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	/// <summary>CCI level for signals.</summary>
	public decimal CciLevel { get => _cciLevel.Value; set => _cciLevel.Value = value; }
	/// <summary>Stochastic level offset from 50.</summary>
	public decimal StochLevel { get => _stochLevel.Value; set => _stochLevel.Value = value; }
	/// <summary>Stop loss in price steps.</summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	/// <summary>Take profit in price steps.</summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	/// <summary>Candle type used for calculations.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	/// <summary>Completed candles to wait after a position change.</summary>
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	/// <summary>
	/// Initialize <see cref="KlossStrategy"/>.
	/// </summary>
	public KlossStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length of weighted MA", "Indicators")
			.SetOptimize(5, 50, 5);

		_cciPeriod = Param(nameof(CciPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Length of CCI", "Indicators")
			.SetOptimize(5, 30, 5);

		_cciLevel = Param(nameof(CciLevel), 50m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Level", "Distance from zero to trigger signal", "Indicators")
			.SetOptimize(50m, 200m, 10m);

		_stochLevel = Param(nameof(StochLevel), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Level", "Distance from 50 to trigger", "Indicators")
			.SetOptimize(5m, 40m, 5m);

		_stopLoss = Param(nameof(StopLoss), 550m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss in price steps", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 550m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles for calculations", "General");

		_cooldownBars = Param(nameof(CooldownBars), 3)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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
		_previousSignal = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ma = new WeightedMovingAverage { Length = MaPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var stoch = new StochasticOscillator();
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(ma, cci, stoch, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue cciValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished || !maValue.IsFinal || !cciValue.IsFinal || !stochValue.IsFinal)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var ma = maValue.ToDecimal();
		var cci = cciValue.ToDecimal();
		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal stochK || stoch.D is not decimal stochD)
			return;

		var price = candle.ClosePrice;
		var buySignal = cci < -CciLevel && stochK < 50m - StochLevel && stochD < 50m - StochLevel && price > ma;
		var sellSignal = cci > CciLevel && stochK > 50m + StochLevel && stochD > 50m + StochLevel && price < ma;
		var currentSignal = buySignal ? 1 : sellSignal ? -1 : 0;

		if (_cooldownRemaining == 0 && Position == 0)
		{
			if (currentSignal > 0 && _previousSignal <= 0)
			{
				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (currentSignal < 0 && _previousSignal >= 0)
			{
				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}

		if (currentSignal != 0)
			_previousSignal = currentSignal;
	}
}
