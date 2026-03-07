using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean reversion strategy based on the Bands Price indicator.
/// Computes price position within Bollinger Bands as an oscillator.
/// Buys when leaving overbought zone, sells when leaving oversold zone.
/// </summary>
public class BandsPriceStrategy : Strategy
{
	private readonly StrategyParam<int> _bandsPeriod;
	private readonly StrategyParam<decimal> _bandsDeviation;
	private readonly StrategyParam<int> _smooth;
	private readonly StrategyParam<int> _upLevel;
	private readonly StrategyParam<int> _dnLevel;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _smoother;
	private int _prevColor;
	private int _prevPrevColor;

	public int BandsPeriod { get => _bandsPeriod.Value; set => _bandsPeriod.Value = value; }
	public decimal BandsDeviation { get => _bandsDeviation.Value; set => _bandsDeviation.Value = value; }
	public int Smooth { get => _smooth.Value; set => _smooth.Value = value; }
	public int UpLevel { get => _upLevel.Value; set => _upLevel.Value = value; }
	public int DnLevel { get => _dnLevel.Value; set => _dnLevel.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BandsPriceStrategy()
	{
		_bandsPeriod = Param(nameof(BandsPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Bands Period", "Bollinger Bands period", "Indicator");

		_bandsDeviation = Param(nameof(BandsDeviation), 2m)
			.SetDisplay("Bands Deviation", "Width of Bollinger Bands", "Indicator");

		_smooth = Param(nameof(Smooth), 5)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing", "Length of smoothing EMA", "Indicator");

		_upLevel = Param(nameof(UpLevel), 25)
			.SetDisplay("Upper Level", "Threshold for overbought zone", "Indicator");

		_dnLevel = Param(nameof(DnLevel), -25)
			.SetDisplay("Lower Level", "Threshold for oversold zone", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
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
		_smoother = default;
		_prevColor = -1;
		_prevPrevColor = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevColor = -1;
		_prevPrevColor = -1;
		_smoother = new ExponentialMovingAverage { Length = Smooth };

		Indicators.Add(_smoother);

		var bands = new BollingerBands { Length = BandsPeriod, Width = BandsDeviation };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bands, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bbValue.IsFormed)
			return;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		var width = upper - lower;
		if (width == 0)
			return;

		// Normalize price position within bands: -50 to +50
		var res = 100m * (candle.ClosePrice - lower) / width - 50m;

		var smoothResult = _smoother.Process(res, candle.OpenTime, true);
		if (!smoothResult.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var jres = smoothResult.ToDecimal();

		// Determine color zone
		int color;
		if (jres > UpLevel)
			color = 4; // overbought
		else if (jres > 0)
			color = 3; // above zero
		else if (jres < DnLevel)
			color = 0; // oversold
		else if (jres < 0)
			color = 1; // below zero
		else
			color = 2; // neutral

		if (_prevPrevColor != -1 && _prevColor != -1)
		{
			// Buy: was overbought (4), now leaving -> mean reversion buy
			if (_prevPrevColor == 4 && _prevColor < 4 && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			// Sell: was oversold (0), now leaving -> mean reversion sell
			else if (_prevPrevColor == 0 && _prevColor > 0 && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevPrevColor = _prevColor;
		_prevColor = color;
	}
}
