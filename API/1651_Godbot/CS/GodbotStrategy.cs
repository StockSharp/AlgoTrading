
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Bollinger Bands, EMA and DEMA trend confirmation.
/// Closes positions on band reversals and opens new ones when price
/// crosses bands with trend alignment.
/// </summary>
public class GodbotStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _demaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _demaCandleType;

	private decimal _emaPrev1;
	private decimal _emaPrev2;
	private int _maCount;

	private decimal _dema0;
	private decimal _dema1;
	private decimal _dema2;
	private int _demaCount;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// EMA period for trend filter.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// DEMA period for higher timeframe trend.
	/// </summary>
	public int DemaPeriod
	{
		get => _demaPeriod.Value;
		set => _demaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for Bollinger Bands and EMA.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type used for DEMA calculation.
	/// </summary>
	public DataType DemaCandleType
	{
		get => _demaCandleType.Value;
		set => _demaCandleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public GodbotStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 23)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Bollinger Bands period", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Bollinger Bands deviation", "General");

		_maPeriod = Param(nameof(MaPeriod), 178)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period for trend", "General");

		_demaPeriod = Param(nameof(DemaPeriod), 56)
			.SetGreaterThanZero()
			.SetDisplay("DEMA Period", "DEMA period for higher timeframe", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Main candle timeframe", "General");

		_demaCandleType = Param(nameof(DemaCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("DEMA Candle Type", "Candle timeframe for DEMA", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DemaCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_emaPrev1 = 0;
		_emaPrev2 = 0;
		_maCount = 0;
		_dema0 = 0;
		_dema1 = 0;
		_dema2 = 0;
		_demaCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var ema = new ExponentialMovingAverage
		{
			Length = MaPeriod
		};

		var dema = new DEMA
		{
			Length = DemaPeriod
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(bollinger, ema, ProcessCandle)
			.Start();

		var demaSubscription = SubscribeCandles(DemaCandleType);
		demaSubscription
			.Bind(dema, ProcessDema)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDema(ICandleMessage candle, decimal demaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_dema2 = _dema1;
		_dema1 = _dema0;
		_dema0 = demaValue;
		if (_demaCount < 3)
			_demaCount++;
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_demaCount < 3)
		{
			// Not enough DEMA data yet
			_emaPrev2 = _emaPrev1;
			_emaPrev1 = emaValue;
			if (_maCount < 2)
				_maCount++;
			return;
		}

		if (_maCount < 2)
		{
			_emaPrev2 = _emaPrev1;
			_emaPrev1 = emaValue;
			_maCount++;
			return;
		}

		var ma1 = _emaPrev1;
		var ma2 = _emaPrev2;

		var buyClose = candle.ClosePrice < upperBand && candle.OpenPrice > upperBand;
		var sellClose = candle.ClosePrice > lowerBand && candle.OpenPrice < lowerBand;
		var buy = candle.ClosePrice > lowerBand && candle.OpenPrice < lowerBand &&
			_dema0 > _dema1 && _dema1 > _dema2 && ma2 < ma1;
		var sell = candle.ClosePrice < upperBand && candle.OpenPrice > upperBand &&
			_dema0 < _dema1 && _dema1 < _dema2 && ma2 > ma1;

		if (sellClose && Position < 0)
			BuyMarket(Math.Abs(Position));

		if (buyClose && Position > 0)
			SellMarket(Math.Abs(Position));

		if (buy && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (sell && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		// update EMA history
		_emaPrev2 = _emaPrev1;
		_emaPrev1 = emaValue;
	}
}
