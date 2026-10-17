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
	private readonly StrategyParam<int> _priceType, _bbMode;
	private Order _pending;

	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public int BbPeriod { get => _bbPeriod.Value; set => _bbPeriod.Value = value; }
	public decimal BbDeviation { get => _bbDeviation.Value; set => _bbDeviation.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public int PriceType { get => _priceType.Value; set => _priceType.Value = value; }
	public int BbMode { get => _bbMode.Value; set => _bbMode.Value = value; }
	public ITrendStrategy()
	{
		_priceType = Param(nameof(PriceType), 0).SetRange(0, 5).SetDisplay("Price Type", "0 Close, 1 Open, 2 High, 3 Low, 4 Median, 5 Typical", "Indicator");
		_bbMode = Param(nameof(BbMode), 0).SetRange(0, 2).SetDisplay("BB Mode", "0 Upper, 1 Lower, 2 Middle", "Indicator");
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
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_pending = null;

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
				if (bbVal.UpBand is not decimal upperBand || bbVal.LowBand is not decimal lowerBand)
					return;
				var band = BbMode switch { 1 => lowerBand, 2 => (upperBand + lowerBand) / 2m, _ => upperBand };
				ProcessCandle(candle, maVal, band);
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
		var price = PriceType switch { 1 => candle.OpenPrice, 2 => candle.HighPrice, 3 => candle.LowPrice,
			4 => (candle.HighPrice + candle.LowPrice) / 2m,
			5 => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			_ => candle.ClosePrice };

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

		if (IsFormedAndOnlineAndAllowTrading() && (_pending is null || _pending.State is OrderStates.Done or OrderStates.Failed))
		{
			if (crossUp && Position <= 0m) _pending = BuyMarket(Volume - Math.Min(0m, Position));
			else if (crossDown && Position >= 0m) _pending = SellMarket(Volume + Math.Max(0m, Position));
		}

		_prevInd = ind;
		_prevSign = sign;
	}
}
