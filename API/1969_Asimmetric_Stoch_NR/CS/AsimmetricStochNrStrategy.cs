using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on stochastic oscillator crossover with asymmetric periods.
/// The strategy opens or closes positions when %K crosses %D.
/// </summary>
public class AsimmetricStochNrStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriodShort;
	private readonly StrategyParam<int> _kPeriodLong;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevK;
	private decimal _prevD;
	private bool _isInitialized;

	/// <summary>
	/// Constructor.
	/// </summary>
	public AsimmetricStochNrStrategy()
	{
		_kPeriodShort = Param(nameof(KPeriodShort), 5)
			.SetGreaterThanZero()
			.SetDisplay("Short %K period", "Fast %K period for asymmetric calculation", "Indicator");

		_kPeriodLong = Param(nameof(KPeriodLong), 12)
			.SetGreaterThanZero()
			.SetDisplay("Long %K period", "Slow %K period for asymmetric calculation", "Indicator");

		_dPeriod = Param(nameof(DPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("%D period", "Smoothing period for signal line", "Indicator");

		_slowing = Param(nameof(Slowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Smoothing of %K line", "Indicator");

		_overbought = Param(nameof(Overbought), 80m)
			.SetDisplay("Overbought", "Overbought level", "Indicator");

		_oversold = Param(nameof(Oversold), 20m)
			.SetDisplay("Oversold", "Oversold level", "Indicator");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Allow Buy", "Allow opening long positions", "General");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Allow Sell", "Allow opening short positions", "General");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Close Long", "Allow closing long positions", "General");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Close Short", "Allow closing short positions", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator calculation", "General");
	}

	/// <summary>
	/// Short %K period.
	/// </summary>
	public int KPeriodShort
	{
		get => _kPeriodShort.Value;
		set => _kPeriodShort.Value = value;
	}

	/// <summary>
	/// Long %K period.
	/// </summary>
	public int KPeriodLong
	{
		get => _kPeriodLong.Value;
		set => _kPeriodLong.Value = value;
	}

	/// <summary>
	/// %D smoothing period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Slowing value for %K.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Overbought level.
	/// </summary>
	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// Oversold level.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Permission to open long positions.
	/// </summary>
	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Permission to open short positions.
	/// </summary>
	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Permission to close long positions.
	/// </summary>
	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	/// <summary>
	/// Permission to close short positions.
	/// </summary>
	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
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
		_prevK = 0m;
		_prevD = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var stochastic = new StochasticOscillator
		{
			Length = KPeriodLong,
			K = { Length = Slowing },
			D = { Length = DPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessStochastic)
			.Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Absolute),
			new Unit(StopLoss, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessStochastic(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var value = (StochasticOscillatorValue)stochValue;

		if (value.K is not decimal k || value.D is not decimal d)
			return;

		if (!_isInitialized)
		{
			_prevK = k;
			_prevD = d;
			_isInitialized = true;
			return;
		}

		var crossUp = _prevK < _prevD && k > d;
		var crossDown = _prevK > _prevD && k < d;

		if (crossUp)
		{
			if (BuyOpen && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));

			if (SellClose && Position < 0)
				BuyMarket(Math.Abs(Position));
		}
		else if (crossDown)
		{
			if (SellOpen && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));

			if (BuyClose && Position > 0)
				SellMarket(Math.Abs(Position));
		}

		_prevK = k;
		_prevD = d;
	}
}

