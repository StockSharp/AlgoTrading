using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Well Martin mean reversion strategy using Bollinger Bands and ADX.
/// </summary>
public class WellMartinStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxLevel;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	private decimal _entryPrice;
	private int _lastDealType; // 0 - none, 1 - sell, 2 - buy

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
	/// Bollinger Bands width (deviation).
	/// </summary>
	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
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
	/// ADX threshold level.
	/// </summary>
	public decimal AdxLevel
	{
		get => _adxLevel.Value;
		set => _adxLevel.Value = value;
	}

	/// <summary>
	/// Trade volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
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
	/// Stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="WellMartinStrategy"/>.
	/// </summary>
	public WellMartinStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 84)
			.SetDisplay("Bollinger Period", "Bollinger Bands period", "Indicators");

		_bollingerWidth = Param<decimal>(nameof(BollingerWidth), 1.8m)
			.SetDisplay("Bollinger Width", "Bollinger Bands width", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 40)
			.SetDisplay("ADX Period", "ADX period", "Indicators");

		_adxLevel = Param<decimal>(nameof(AdxLevel), 45m)
			.SetDisplay("ADX Level", "ADX threshold", "Parameters");

		_volume = Param<decimal>(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Trade volume", "Parameters");

		_takeProfit = Param<decimal>(nameof(TakeProfit), 1200m)
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_stopLoss = Param<decimal>(nameof(StopLoss), 1400m)
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");
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

		_entryPrice = 0m;
		_lastDealType = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerWidth
		};

		var adx = new ADX
		{
			Length = AdxPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(bollinger, adx, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal adx)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (Position == 0)
		{
		if (candle.ClosePrice < lower && adx < AdxLevel && (_lastDealType == 0 || _lastDealType == 2))
		{
		BuyMarket(Volume);
		_entryPrice = candle.ClosePrice;
		}
		else if (candle.ClosePrice > upper && adx < AdxLevel && (_lastDealType == 0 || _lastDealType == 1))
		{
		SellMarket(Volume);
		_entryPrice = candle.ClosePrice;
		}
		}
		else if (Position > 0)
		{
		var profit = candle.ClosePrice - _entryPrice;

		if (candle.ClosePrice >= upper || (TakeProfit > 0 && profit >= TakeProfit) || (StopLoss > 0 && -profit >= StopLoss))
		{
		SellMarket(Position);
		_lastDealType = 2;
		}
		}
		else if (Position < 0)
		{
		var profit = _entryPrice - candle.ClosePrice;

		if (candle.ClosePrice <= lower || (TakeProfit > 0 && profit >= TakeProfit) || (StopLoss > 0 && -profit >= StopLoss))
		{
		BuyMarket(Math.Abs(Position));
		_lastDealType = 1;
		}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0 && delta != 0)
		{
		_entryPrice = 0m;
		}
	}
}
