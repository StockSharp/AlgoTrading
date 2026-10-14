using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enhanced BarUpDn strategy.
/// A bullish candle that opens above the previous close goes long when it closes above the trend SMA and above the lower
/// Bollinger band; a bearish candle that opens below the previous close goes short when it closes below the trend SMA and below
/// the upper band. An opposite signal reverses the position. The stop lies AtrMultiplierSl ATR and the target AtrMultiplierTp ATR
/// from the entry.
/// </summary>
public class EnhancedBarUpDnStrategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplierSl;
	private readonly StrategyParam<decimal> _atrMultiplierTp;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BbMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// Trend SMA length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Stop loss distance in ATR.
	/// </summary>
	public decimal AtrMultiplierSl
	{
		get => _atrMultiplierSl.Value;
		set => _atrMultiplierSl.Value = value;
	}

	/// <summary>
	/// Take profit distance in ATR.
	/// </summary>
	public decimal AtrMultiplierTp
	{
		get => _atrMultiplierTp.Value;
		set => _atrMultiplierTp.Value = value;
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
	/// Constructor.
	/// </summary>
	public EnhancedBarUpDnStrategy()
	{
		_bbLength = Param(nameof(BbLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands length", "Indicators");

		_bbMultiplier = Param(nameof(BbMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Multiplier", "Bollinger Bands deviation multiplier", "Indicators");

		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Trend SMA length", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR length", "Risk");

		_atrMultiplierSl = Param(nameof(AtrMultiplierSl), 2m)
			.SetNotNegative()
			.SetDisplay("ATR Stop", "Stop loss distance in ATR", "Risk");

		_atrMultiplierTp = Param(nameof(AtrMultiplierTp), 3m)
			.SetNotNegative()
			.SetDisplay("ATR Target", "Take profit distance in ATR", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_prevClose = null;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;

		var bollinger = new BollingerBands { Length = BbLength, Width = BbMultiplier };
		var ma = new SimpleMovingAverage { Length = MaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue maValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevClose = _prevClose;
		_prevClose = candle.ClosePrice;

		if (!bollingerValue.IsFormed || !maValue.IsFormed || !atrValue.IsFormed)
			return;

		if (bollingerValue is not IBollingerBandsValue { UpBand: decimal upper, LowBand: decimal lower })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0 && ((AtrMultiplierSl > 0m && candle.LowPrice <= _stopPrice) || (AtrMultiplierTp > 0m && candle.HighPrice >= _takePrice)))
		{
			SellMarket(Position);
			return;
		}

		if (Position < 0 && ((AtrMultiplierSl > 0m && candle.HighPrice >= _stopPrice) || (AtrMultiplierTp > 0m && candle.LowPrice <= _takePrice)))
		{
			BuyMarket(-Position);
			return;
		}

		if (prevClose is not decimal pc)
			return;

		var ma = maValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		var longSignal = close > open && open > pc && close > ma && close > lower;
		var shortSignal = close < open && open < pc && close < ma && close < upper;

		if (longSignal && Position <= 0)
		{
			_stopPrice = close - atr * AtrMultiplierSl;
			_takePrice = close + atr * AtrMultiplierTp;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortSignal && Position >= 0)
		{
			_stopPrice = close + atr * AtrMultiplierSl;
			_takePrice = close - atr * AtrMultiplierTp;
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
