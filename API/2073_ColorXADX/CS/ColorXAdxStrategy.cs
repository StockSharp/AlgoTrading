using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on crossing of +DI and -DI with ADX confirmation.
/// </summary>
public class ColorXAdxStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevPlusDi;
	private decimal _prevMinusDi;
	private bool _isFirst = true;

	/// <summary>
	/// Initializes a new instance of the <see cref="ColorXAdxStrategy"/> class.
	/// </summary>
	public ColorXAdxStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetRange(5, 50)
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
			.SetCanOptimize(true);

		_adxThreshold = Param(nameof(AdxThreshold), 30m)
			.SetRange(10m, 60m)
			.SetDisplay("ADX Threshold", "Minimum ADX level for trades", "Indicators")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 1000)
			.SetRange(100, 5000)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 2000)
			.SetRange(100, 5000)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold for trade validation.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss in price units.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price units.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type.
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
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(adx, ProcessCandle).Start();

		StartProtection(
			takeProfit: TakeProfit > 0 ? new Unit(TakeProfit, UnitTypes.Absolute) : null,
			stopLoss: StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Absolute) : null,
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
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adx = (AverageDirectionalIndexValue)adxValue;
		var plusDi = adx.Dx.Plus;
		var minusDi = adx.Dx.Minus;
		var adxMain = adx.MovingAverage;

		if (_isFirst)
		{
			_prevPlusDi = plusDi;
			_prevMinusDi = minusDi;
			_isFirst = false;
			return;
		}

		// Detect DI cross with ADX confirmation
		if (plusDi > minusDi && _prevPlusDi <= _prevMinusDi)
		{
			if (adxMain > AdxThreshold && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}

			if (Position < 0)
				BuyMarket(Math.Abs(Position));
		}
		else if (minusDi > plusDi && _prevMinusDi <= _prevPlusDi)
		{
			if (adxMain > AdxThreshold && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}

			if (Position > 0)
				SellMarket(Math.Abs(Position));
		}

		_prevPlusDi = plusDi;
		_prevMinusDi = minusDi;
	}
}

