using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// i_Trend strategy built on Bollinger Bands and Moving Average.
/// Generates buy/sell signals when the iTrend value crosses the signal line.
/// </summary>
public class ITrendStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbDeviation;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevInd;
	private decimal _prevSign;
	private bool _isInitialized;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int BbPeriod { get => _bbPeriod.Value; set => _bbPeriod.Value = value; }
	public decimal BbDeviation { get => _bbDeviation.Value; set => _bbDeviation.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ITrendStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average length", "Indicator");

		_bbPeriod = Param(nameof(BbPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicator");

		_bbDeviation = Param(nameof(BbDeviation), 2.0m)
			.SetDisplay("BB Deviation", "Standard deviation for Bollinger Bands", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used", "General");
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
		_prevInd = 0m;
		_prevSign = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ma = new ExponentialMovingAverage { Length = MaPeriod };
		var bb = new BollingerBands { Length = BbPeriod, Width = BbDeviation };

		Indicators.Add(ma);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(bb, (candle, bbValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!bbValue.IsFormed)
					return;

				var maResult = ma.Process(candle.ClosePrice, candle.OpenTime, true);
				if (!maResult.IsFormed)
					return;

				var maVal = maResult.ToDecimal();
				var bbVal = (BollingerBandsValue)bbValue;
				if (bbVal.UpBand is not decimal upperBand)
					return;

				ProcessCandle(candle, maVal, upperBand);
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal band)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;

		var ind = price - band;
		var sign = 2m * maValue - (candle.LowPrice + candle.HighPrice);

		if (!_isInitialized)
		{
			_prevInd = ind;
			_prevSign = sign;
			_isInitialized = true;
			return;
		}

		var crossUp = _prevInd <= _prevSign && ind > sign;
		var crossDown = _prevInd >= _prevSign && ind < sign;

		if (crossUp && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevInd = ind;
		_prevSign = sign;
	}
}
