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
/// Strategy that combines Weighted Moving Average, CCI and Stochastic oscillator.
/// Generates buy signals when CCI is below negative level, stochastic is oversold,
/// and price is above the MA. Opposite conditions for sell.
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

	private WeightedMovingAverage _ma = null!;
	private CommodityChannelIndex _cci = null!;
	private StochasticOscillator _stoch = null!;

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

		_cciLevel = Param(nameof(CciLevel), 120m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Level", "Distance from zero to trigger signal", "Indicators")
			.SetOptimize(50m, 200m, 10m);

		_stochLevel = Param(nameof(StochLevel), 20m)
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
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ma = new WeightedMovingAverage { Length = MaPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_stoch = new StochasticOscillator();

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_stoch, ProcessCandle).Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
			stopLoss: new Unit(StopLoss * step, UnitTypes.Absolute),
			takeProfit: new Unit(TakeProfit * step, UnitTypes.Absolute)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Process WMA and CCI manually
		var maResult = _ma.Process(candle.ClosePrice, candle.OpenTime, candle.State == CandleStates.Finished);
		var cciResult = _cci.Process(candle);

		if (!_ma.IsFormed || !_cci.IsFormed || !_stoch.IsFormed)
			return;

		var maVal = maResult.ToDecimal();
		var cciVal = cciResult.ToDecimal();

		var stochTyped = (StochasticOscillatorValue)stochValue;
		if (stochTyped.K is not decimal stochK)
			return;

		var price = candle.ClosePrice;

		// Buy when CCI is below negative level and stochastic is oversold
		var buySignal = cciVal < -CciLevel && stochK < 50m - StochLevel;

		// Sell when CCI is above positive level and stochastic is overbought
		var sellSignal = cciVal > CciLevel && stochK > 50m + StochLevel;

		if (buySignal && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (sellSignal && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}
