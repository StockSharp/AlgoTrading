using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on crossing of +DI and -DI from the ADX indicator.
/// </summary>
public class FractalAdxCloudStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevPlusDi;
	private decimal? _prevMinusDi;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss level in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit level in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Allow opening of long positions.
	/// </summary>
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	/// <summary>
	/// Allow opening of short positions.
	/// </summary>
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	/// <summary>
	/// Allow closing of long positions on opposite signal.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	/// <summary>
	/// Allow closing of short positions on opposite signal.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FractalAdxCloudStrategy"/>.
	/// </summary>
	public FractalAdxCloudStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 30)
			.SetRange(5, 60)
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetRange(100m, 5000m)
			.SetDisplay("Stop Loss", "Stop loss level", "Risk Management")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetRange(100m, 10000m)
			.SetDisplay("Take Profit", "Take profit level", "Risk Management")
			.SetCanOptimize(true);

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Buy Close", "Allow closing long positions", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Sell Close", "Allow closing short positions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
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

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
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

		if (_prevPlusDi is decimal prevPlus && _prevMinusDi is decimal prevMinus)
		{
			if (plusDi > minusDi)
			{
				if (SellPosClose && Position < 0)
					BuyMarket(Math.Abs(Position));

				if (BuyPosOpen && prevPlus <= prevMinus && Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
			}
			else if (minusDi > plusDi)
			{
				if (BuyPosClose && Position > 0)
					SellMarket(Math.Abs(Position));

				if (SellPosOpen && prevPlus >= prevMinus && Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			}
		}

		_prevPlusDi = plusDi;
		_prevMinusDi = minusDi;
	}
}
