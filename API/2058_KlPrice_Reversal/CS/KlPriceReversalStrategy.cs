using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// KlPrice reversal strategy.
/// Calculates a normalized oscillator based on price position relative to EMA and ATR.
/// Buys when leaving overbought zone, sells when leaving oversold zone.
/// </summary>
public class KlPriceReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _priceMaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _downLevel;

	private decimal _prevColor;
	private bool _isFirst;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int PriceMaLength { get => _priceMaLength.Value; set => _priceMaLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal UpLevel { get => _upLevel.Value; set => _upLevel.Value = value; }
	public decimal DownLevel { get => _downLevel.Value; set => _downLevel.Value = value; }

	public KlPriceReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for calculations", "General");

		_priceMaLength = Param(nameof(PriceMaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Price MA Length", "EMA period for price smoothing", "Parameters");

		_atrLength = Param(nameof(AtrLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for range estimation", "Parameters");

		_upLevel = Param(nameof(UpLevel), 50m)
			.SetDisplay("Upper Level", "Upper threshold for signals", "Parameters");

		_downLevel = Param(nameof(DownLevel), -50m)
			.SetDisplay("Lower Level", "Lower threshold for signals", "Parameters");
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
		_prevColor = 2m;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_isFirst = true;
		_prevColor = 2m;

		var priceMa = new ExponentialMovingAverage { Length = PriceMaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(priceMa, atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!maValue.IsFormed || !atrValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ma = maValue.ToDecimal();
		var tr = atrValue.ToDecimal();
		if (tr == 0m)
			return;

		var dwband = ma - tr;
		var jres = 100m * (candle.ClosePrice - dwband) / (2m * tr) - 50m;

		var color = 2m;
		if (jres > UpLevel)
			color = 4m;
		else if (jres > 0m)
			color = 3m;

		if (jres < DownLevel)
			color = 0m;
		else if (jres < 0m)
			color = 1m;

		if (!_isFirst)
		{
			// Buy: leaving overbought (was 4, now < 4)
			if (_prevColor == 4m && color < 4m && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			// Sell: leaving oversold (was 0, now > 0)
			else if (_prevColor == 0m && color > 0m && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevColor = color;
		_isFirst = false;
	}
}
