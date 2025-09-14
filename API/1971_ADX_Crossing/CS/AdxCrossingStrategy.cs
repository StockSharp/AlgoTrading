using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on crossovers of the +DI and -DI lines of the ADX indicator.
/// It opens or closes positions when directional lines cross each other.
/// </summary>
public class AdxCrossingStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private decimal _prevPlusDi;
	private decimal _prevMinusDi;
	private bool _isInitialized;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Permission to open long positions.
	/// </summary>
	public bool AllowBuyOpen
	{
		get => _allowBuyOpen.Value;
		set => _allowBuyOpen.Value = value;
	}

	/// <summary>
	/// Permission to open short positions.
	/// </summary>
	public bool AllowSellOpen
	{
		get => _allowSellOpen.Value;
		set => _allowSellOpen.Value = value;
	}

	/// <summary>
	/// Permission to close long positions.
	/// </summary>
	public bool AllowBuyClose
	{
		get => _allowBuyClose.Value;
		set => _allowBuyClose.Value = value;
	}

	/// <summary>
	/// Permission to close short positions.
	/// </summary>
	public bool AllowSellClose
	{
		get => _allowSellClose.Value;
		set => _allowSellClose.Value = value;
	}

	/// <summary>
	/// Stop-loss in absolute price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take-profit in absolute price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public AdxCrossingStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 50)
			.SetDisplay("ADX Period", "Period of ADX indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");

		_allowBuyOpen = Param(nameof(AllowBuyOpen), true)
			.SetDisplay("Allow Buy Open", "Enable opening long positions", "Permissions");

		_allowSellOpen = Param(nameof(AllowSellOpen), true)
			.SetDisplay("Allow Sell Open", "Enable opening short positions", "Permissions");

		_allowBuyClose = Param(nameof(AllowBuyClose), true)
			.SetDisplay("Allow Buy Close", "Enable closing long positions", "Permissions");

		_allowSellClose = Param(nameof(AllowSellClose), true)
			.SetDisplay("Allow Sell Close", "Enable closing short positions", "Permissions");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Absolute stop-loss in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Absolute take-profit in price units", "Risk");
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
		_prevPlusDi = default;
		_prevMinusDi = default;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}

		StartProtection(
			stopLoss: StopLoss > 0m ? new Unit(StopLoss, UnitTypes.Absolute) : null,
			takeProfit: TakeProfit > 0m ? new Unit(TakeProfit, UnitTypes.Absolute) : null);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adx = (AverageDirectionalIndexValue)adxValue;

		if (adx.Dx.Plus is not decimal plusDi || adx.Dx.Minus is not decimal minusDi)
			return;

		if (!_isInitialized)
		{
			_prevPlusDi = plusDi;
			_prevMinusDi = minusDi;
			_isInitialized = true;
			return;
		}

		var buySignal = plusDi > minusDi && _prevPlusDi <= _prevMinusDi;
		var sellSignal = plusDi < minusDi && _prevPlusDi >= _prevMinusDi;

		if (buySignal)
		{
			if (AllowSellClose && Position < 0)
				BuyMarket(Math.Abs(Position));

			if (AllowBuyOpen && Position <= 0)
				BuyMarket();
		}

		if (sellSignal)
		{
			if (AllowBuyClose && Position > 0)
				SellMarket(Math.Abs(Position));

			if (AllowSellOpen && Position >= 0)
				SellMarket();
		}

		_prevPlusDi = plusDi;
		_prevMinusDi = minusDi;
	}
}

