using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the CoensioTrader1 V06 MQL strategy.
/// The strategy buys after a lower Bollinger Band rejection paired with a higher low pattern and bullish DEMA trend.
/// It sells after an upper Bollinger Band rejection with a lower high structure and bearish DEMA trend.
/// </summary>
public class CoensioTrader1V06Strategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _demaPeriod;
	private readonly StrategyParam<Unit> _stopLossDistance;
	private readonly StrategyParam<Unit> _takeProfitDistance;
	private readonly StrategyParam<bool> _closeOnSignal;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevOpen;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevClose;

	private decimal? _prev2High;
	private decimal? _prev2Low;
	private decimal? _prev3High;
	private decimal? _prev3Low;

	private decimal? _prevUpperBand;
	private decimal? _prevLowerBand;

	private decimal? _prevDema;
	private decimal? _prev2Dema;

	/// <summary>
	/// Initializes a new instance of the <see cref="CoensioTrader1V06Strategy"/> class.
	/// </summary>
	public CoensioTrader1V06Strategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Length of Bollinger Bands", "Indicators")
			.SetCanOptimize();

		_bollingerDeviation = Param(nameof(BollingerDeviation), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Indicators")
			.SetCanOptimize();

		_demaPeriod = Param(nameof(DemaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("DEMA Period", "Length of double exponential moving average", "Indicators")
			.SetCanOptimize();

		_stopLossDistance = Param(nameof(StopLossDistance), new Unit(0m, UnitTypes.Absolute))
			.SetDisplay("Stop Loss", "Absolute stop loss offset from entry", "Risk");

		_takeProfitDistance = Param(nameof(TakeProfitDistance), new Unit(0m, UnitTypes.Absolute))
			.SetDisplay("Take Profit", "Absolute take profit offset from entry", "Risk");

		_closeOnSignal = Param(nameof(CloseOnSignal), false)
			.SetDisplay("Close On Opposite Signal", "Close current trades when opposite setup appears", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for signal calculations", "General");
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Double exponential moving average period.
	/// </summary>
	public int DemaPeriod
	{
		get => _demaPeriod.Value;
		set => _demaPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance from entry price.
	/// </summary>
	public Unit StopLossDistance
	{
		get => _stopLossDistance.Value;
		set => _stopLossDistance.Value = value;
	}

	/// <summary>
	/// Take-profit distance from entry price.
	/// </summary>
	public Unit TakeProfitDistance
	{
		get => _takeProfitDistance.Value;
		set => _takeProfitDistance.Value = value;
	}

	/// <summary>
	/// Close current position when an opposite signal appears.
	/// </summary>
	public bool CloseOnSignal
	{
		get => _closeOnSignal.Value;
		set => _closeOnSignal.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_prevOpen = null;
		_prevHigh = null;
		_prevLow = null;
		_prevClose = null;

		_prev2High = null;
		_prev2Low = null;
		_prev3High = null;
		_prev3Low = null;

		_prevUpperBand = null;
		_prevLowerBand = null;

		_prevDema = null;
		_prev2Dema = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
			stopLoss: StopLossDistance,
			takeProfit: TakeProfitDistance,
			isStopTrailing: false,
			useMarketOrders: true);

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var dema = new DoubleExponentialMovingAverage
		{
			Length = DemaPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bollinger, dema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, dema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal demaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevOpen.HasValue && _prevClose.HasValue && _prevLow.HasValue && _prevHigh.HasValue &&
			_prev2Low.HasValue && _prev3Low.HasValue && _prev2High.HasValue && _prev3High.HasValue &&
			_prevLowerBand.HasValue && _prevUpperBand.HasValue && _prevDema.HasValue && _prev2Dema.HasValue)
		{
			// Long setup: lower band rejection with higher low sequence and rising DEMA.
			var crossedLowerBand = _prevOpen.Value < _prevLowerBand.Value && _prevClose.Value > _prevLowerBand.Value;
			var higherLowPattern = _prevLow.Value > _prev2Low.Value && _prev2Low.Value < _prev3Low.Value;
			var bullishTrend = demaValue > _prevDema.Value && _prevDema.Value > _prev2Dema.Value;

			if (crossedLowerBand && higherLowPattern && bullishTrend)
			{
				if (CloseOnSignal && Position < 0)
					ClosePosition();

				if (Position <= 0)
					BuyMarket();
			}

			// Short setup: upper band rejection with lower high sequence and falling DEMA.
			var crossedUpperBand = _prevOpen.Value > _prevUpperBand.Value && _prevClose.Value < _prevUpperBand.Value;
			var lowerHighPattern = _prevHigh.Value < _prev2High.Value && _prev2High.Value > _prev3High.Value;
			var bearishTrend = demaValue < _prevDema.Value && _prevDema.Value < _prev2Dema.Value;

			if (crossedUpperBand && lowerHighPattern && bearishTrend)
			{
				if (CloseOnSignal && Position > 0)
					ClosePosition();

				if (Position >= 0)
					SellMarket();
			}
		}

		_prev3Low = _prev2Low;
		_prev3High = _prev2High;
		_prev2Low = _prevLow;
		_prev2High = _prevHigh;

		_prevLowerBand = lower;
		_prevUpperBand = upper;

		_prev2Dema = _prevDema;
		_prevDema = demaValue;

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_prevLow = candle.LowPrice;
		_prevHigh = candle.HighPrice;
	}
}

