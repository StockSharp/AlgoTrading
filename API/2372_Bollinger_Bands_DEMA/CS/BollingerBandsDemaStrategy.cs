using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using Bollinger Bands for entries and DEMA for trend confirmation.
/// Enters long when a bullish candle crosses above the lower band and DEMA is rising.
/// Enters short when a bearish candle crosses below the upper band and DEMA is falling.
/// Exits long on bearish cross of the upper band and exits short on bullish cross of the lower band.
/// </summary>
public class BollingerBandsDemaStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<int> _demaPeriod;
	private readonly StrategyParam<decimal> _deviation;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _dema0;
	private decimal? _dema1;
	private decimal? _dema2;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// DEMA period.
	/// </summary>
	public int DemaPeriod
	{
		get => _demaPeriod.Value;
		set => _demaPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation for Bollinger Bands.
	/// </summary>
	public decimal Deviation
	{
		get => _deviation.Value;
		set => _deviation.Value = value;
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
	/// Initializes a new instance of <see cref="BollingerBandsDemaStrategy"/>.
	/// </summary>
	public BollingerBandsDemaStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Length of Bollinger Bands", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_demaPeriod = Param(nameof(DemaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("DEMA Period", "Length of double EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_deviation = Param(nameof(Deviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation", "Standard deviation for Bollinger Bands", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for Bollinger calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_dema0 = _dema1 = _dema2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = Deviation };
		var dema = new DEMA { Length = DemaPeriod };

		var demaSub = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		demaSub
			.Bind(dema, (candle, value) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!dema.IsFormed)
					return;

				_dema2 = _dema1;
				_dema1 = _dema0;
				_dema0 = value;
			})
			.Start();

		var mainSub = SubscribeCandles(CandleType);
		mainSub
			.Bind(bollinger, ProcessMain)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMain(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_dema0 is null || _dema1 is null || _dema2 is null)
			return;

		var demaUp = _dema0 > _dema1 && _dema1 > _dema2;
		var demaDown = _dema0 < _dema1 && _dema1 < _dema2;

		var buyCondition = candle.ClosePrice > lower && candle.OpenPrice < lower && demaUp;
		var sellCondition = candle.ClosePrice < upper && candle.OpenPrice > upper && demaDown;
		var buyClose = candle.ClosePrice < upper && candle.OpenPrice > upper;
		var sellClose = candle.ClosePrice > lower && candle.OpenPrice < lower;

		if (buyCondition && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (sellCondition && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		if (buyClose && Position > 0)
			SellMarket(Position);

		if (sellClose && Position < 0)
			BuyMarket(-Position);
	}
}
