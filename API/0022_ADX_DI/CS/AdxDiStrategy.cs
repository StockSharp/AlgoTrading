using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on ADX and Directional Movement indicators.
/// Buys when +DI crosses above -DI with strong ADX.
/// Sells when -DI crosses above +DI with strong ADX.
/// </summary>
public class AdxDiStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevPlusDiAbove;
	private bool _hasPrevValues;
	private int _cooldown;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold for trend confirmation.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
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
	/// Initializes a new instance of the <see cref="AdxDiStrategy"/>.
	/// </summary>
	public AdxDiStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
			.SetOptimize(10, 20, 2);

		_adxThreshold = Param(nameof(AdxThreshold), 15m)
			.SetDisplay("ADX Threshold", "ADX level to confirm trend", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevPlusDiAbove = default;
		_hasPrevValues = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (adxValue.IsEmpty)
			return;

		decimal adxMain, plusDi, minusDi;
		try
		{
			var adx = (AverageDirectionalIndexValue)adxValue;
			if (adx.MovingAverage is not decimal ma)
				return;
			if (adx.Dx.Plus is not decimal pDi)
				return;
			if (adx.Dx.Minus is not decimal mDi)
				return;
			adxMain = ma;
			plusDi = pDi;
			minusDi = mDi;
		}
		catch (IndexOutOfRangeException)
		{
			return;
		}

		var plusDiAbove = plusDi > minusDi;

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevPlusDiAbove = plusDiAbove;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevPlusDiAbove = plusDiAbove;
			return;
		}

		// +DI crosses above -DI with strong trend = buy
		if (plusDiAbove && !_prevPlusDiAbove && adxMain >= AdxThreshold && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_cooldown = 5;
		}
		// -DI crosses above +DI with strong trend = sell
		else if (!plusDiAbove && _prevPlusDiAbove && adxMain >= AdxThreshold && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_cooldown = 5;
		}

		_prevPlusDiAbove = plusDiAbove;
	}
}
