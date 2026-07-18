using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Majors Volume Sum Strategy - trades based on volume momentum using EMA smoothing.
/// </summary>
public class MajorsVolumeSumStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;

	private decimal _prevClose;
	private decimal _volumeEma;
	private decimal _maxAbs;
	private bool _isReady;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	public MajorsVolumeSumStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for processing", "General");

		_emaLength = Param(nameof(EmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Smoothing period for volume", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_volumeEma = 0;
		_maxAbs = 0;
		_isReady = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isReady)
		{
			_prevClose = candle.ClosePrice;
			_isReady = true;
			return;
		}

		// Volume direction based on price change
		var direction = candle.ClosePrice > _prevClose ? 1m : -1m;
		var signedVol = direction * candle.TotalVolume;

		// Simple EMA of signed volume
		var k = 2m / (EmaLength + 1m);
		_volumeEma = _volumeEma * (1m - k) + signedVol * k;

		var absEma = Math.Abs(_volumeEma);
		if (absEma > _maxAbs)
			_maxAbs = absEma;

		// Trade when volume momentum is strong relative to its history
		var threshold = _maxAbs * 0.5m;

		if (_volumeEma > threshold && candle.ClosePrice > emaVal && Position <= 0)
			BuyMarket();
		else if (_volumeEma < -threshold && candle.ClosePrice < emaVal && Position >= 0)
			SellMarket();

		_prevClose = candle.ClosePrice;
	}
}
