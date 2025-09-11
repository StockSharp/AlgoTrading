using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades on Bollinger Bands breakouts.
/// Buys when price crosses above the upper band and sells when price crosses below the lower band.
/// </summary>
public class FancyBollingerBandsStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _deviation;
	private readonly StrategyParam<DataType> _candleType;

	private BollingerBands _bands;
	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private bool _initialized;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Deviation multiplier.
	/// </summary>
	public decimal Deviation
	{
		get => _deviation.Value;
		set => _deviation.Value = value;
	}

	/// <summary>
	/// The type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FancyBollingerBandsStrategy"/> class.
	/// </summary>
	public FancyBollingerBandsStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Bollinger Bands period", "Bollinger")
			.SetCanOptimize(true);

		_deviation = Param(nameof(Deviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation", "Standard deviation multiplier", "Bollinger")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_initialized = false;
		_prevClose = 0m;
		_prevUpper = 0m;
		_prevLower = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		_bands = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = Deviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bands, OnProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bands);
			DrawOwnTrades(area);
		}
	}

	private void OnProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bands.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upper;
			_prevLower = lower;
			return;
		}

		if (!_initialized)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upper;
			_prevLower = lower;
			_initialized = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upper;
			_prevLower = lower;
			return;
		}

		var crossAbove = _prevClose <= _prevUpper && candle.ClosePrice > upper;
		var crossBelow = _prevClose >= _prevLower && candle.ClosePrice < lower;

		if (crossAbove && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossBelow && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevClose = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;
	}
}
