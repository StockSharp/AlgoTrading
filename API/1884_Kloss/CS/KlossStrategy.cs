using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines Weighted Moving Average, CCI and Stochastic oscillator.
/// Generates signals when CCI and Stochastic cross predefined levels and price crosses the MA.
/// </summary>
public class KlossStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<int> _priceShift;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _cciShift;
	private readonly StrategyParam<decimal> _cciDiffer;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<int> _stochSmooth;
	private readonly StrategyParam<int> _stochShift;
	private readonly StrategyParam<decimal> _stochDiffer;
	private readonly StrategyParam<int> _commonShift;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _revClose;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _ma = null!;
	private CommodityChannelIndex _cci = null!;
	private StochasticOscillator _stoch = null!;
	private Shift _maShifter = null!;
	private Shift _priceShifter = null!;
	private Shift _cciShifter = null!;
	private Shift _stochShifter = null!;

	/// <summary>
	/// Initialize <see cref="KlossStrategy"/>.
	/// </summary>
	public KlossStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 1)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length of weighted MA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 50, 1);

		_maShift = Param(nameof(MaShift), 5)
			.SetNotNegative()
			.SetDisplay("MA Shift", "Shift for MA values", "Indicators");

		_priceShift = Param(nameof(PriceShift), 1)
			.SetNotNegative()
			.SetDisplay("Price Shift", "Shift for price values", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Length of CCI", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_cciShift = Param(nameof(CciShift), 0)
			.SetNotNegative()
			.SetDisplay("CCI Shift", "Shift for CCI values", "Indicators");

		_cciDiffer = Param(nameof(CciDiffer), 120m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Level", "Distance from zero to trigger signal", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50m, 200m, 10m);

		_stochKPeriod = Param(nameof(StochKPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Period of %K line", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "Period of %D line", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_stochSmooth = Param(nameof(StochSmooth), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Smooth", "Smoothing for %K", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_stochShift = Param(nameof(StochShift), 0)
			.SetNotNegative()
			.SetDisplay("Stochastic Shift", "Shift for stochastic values", "Indicators");

		_stochDiffer = Param(nameof(StochDiffer), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Level", "Distance from 50 to trigger", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5m, 40m, 5m);

		_commonShift = Param(nameof(CommonShift), 1)
			.SetNotNegative()
			.SetDisplay("Common Shift", "Added to all shift parameters", "Indicators");

		_stopLoss = Param(nameof(StopLoss), 550m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pts)", "Stop loss in points", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 550m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pts)", "Take profit in points", "Risk");

		_revClose = Param(nameof(RevClose), true)
			.SetDisplay("Reverse Close", "Close position on opposite signal", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for calculations", "General");
	}

	/// <summary>Moving Average period.</summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	/// <summary>Shift applied to MA.</summary>
	public int MaShift { get => _maShift.Value; set => _maShift.Value = value; }
	/// <summary>Shift applied to price.</summary>
	public int PriceShift { get => _priceShift.Value; set => _priceShift.Value = value; }
	/// <summary>CCI calculation period.</summary>
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	/// <summary>Shift applied to CCI.</summary>
	public int CciShift { get => _cciShift.Value; set => _cciShift.Value = value; }
	/// <summary>CCI level for signals.</summary>
	public decimal CciDiffer { get => _cciDiffer.Value; set => _cciDiffer.Value = value; }
	/// <summary>Stochastic %K period.</summary>
	public int StochKPeriod { get => _stochKPeriod.Value; set => _stochKPeriod.Value = value; }
	/// <summary>Stochastic %D period.</summary>
	public int StochDPeriod { get => _stochDPeriod.Value; set => _stochDPeriod.Value = value; }
	/// <summary>Smoothing for %K.</summary>
	public int StochSmooth { get => _stochSmooth.Value; set => _stochSmooth.Value = value; }
	/// <summary>Shift applied to Stochastic.</summary>
	public int StochShift { get => _stochShift.Value; set => _stochShift.Value = value; }
	/// <summary>Stochastic level for signals.</summary>
	public decimal StochDiffer { get => _stochDiffer.Value; set => _stochDiffer.Value = value; }
	/// <summary>Common shift added to all indicators.</summary>
	public int CommonShift { get => _commonShift.Value; set => _commonShift.Value = value; }
	/// <summary>Stop loss in points.</summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	/// <summary>Take profit in points.</summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	/// <summary>Close position on opposite signal.</summary>
	public bool RevClose { get => _revClose.Value; set => _revClose.Value = value; }
	/// <summary>Candle type used for calculations.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = new WeightedMovingAverage { Length = MaPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_stoch = new StochasticOscillator { KPeriod = StochKPeriod, DPeriod = StochDPeriod, Smooth = StochSmooth };

		var shiftAdd = CommonShift;
		_maShifter = new Shift { Length = MaShift + shiftAdd };
		_priceShifter = new Shift { Length = PriceShift + shiftAdd };
		_cciShifter = new Shift { Length = CciShift + shiftAdd };
		_stochShifter = new Shift { Length = StochShift + shiftAdd };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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
			DrawIndicator(area, _cci);
			DrawIndicator(area, _stoch);
			DrawOwnTrades(area);
		}
	}

	/// <summary>
	/// Process incoming candles and generate trading signals.
	/// </summary>
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var maValue = _ma.Process(candle).ToDecimal();
		var maShifted = _maShifter.Process(maValue, candle.OpenTime, true).ToDecimal();

		var priceShifted = _priceShifter.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();

		var cciValue = _cci.Process(candle).ToDecimal();
		var cciShifted = _cciShifter.Process(cciValue, candle.OpenTime, true).ToDecimal();

		var stochVal = (StochasticOscillatorValue)_stoch.Process(candle);
		var stochK = stochVal.K;
		var stochShifted = _stochShifter.Process(stochK, candle.OpenTime, true).ToDecimal();

		var buySignal = cciShifted < -CciDiffer &&
			stochShifted < 50m - StochDiffer &&
			priceShifted > maShifted;

		var sellSignal = cciShifted > CciDiffer &&
			stochShifted > 50m + StochDiffer &&
			priceShifted < maShifted;

		if (RevClose)
		{
			if (buySignal && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (sellSignal && Position > 0)
			{
				SellMarket(Math.Abs(Position));
				return;
			}
		}

		if (buySignal && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (sellSignal && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
	}
}
