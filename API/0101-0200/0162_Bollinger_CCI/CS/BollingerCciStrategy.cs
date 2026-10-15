using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger CCI strategy.
/// A close below the lower band with CCI below CciOversold goes long and a close above the upper band with CCI above CciOverbought goes short,
/// reversing an opposite position. The position closes when price returns to the middle band. The stop lies StopLossAtr ATR from the entry
/// close and is checked on candle closes.
/// </summary>
public class BollingerCciStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciOversold;
	private readonly StrategyParam<decimal> _cciOverbought;
	private readonly StrategyParam<decimal> _stopLossAtr;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;

	/// <summary>
	/// Period of the Bollinger Bands.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier of the bands.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Period of CCI.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// CCI level for longs.
	/// </summary>
	public decimal CciOversold
	{
		get => _cciOversold.Value;
		set => _cciOversold.Value = value;
	}

	/// <summary>
	/// CCI level for shorts.
	/// </summary>
	public decimal CciOverbought
	{
		get => _cciOverbought.Value;
		set => _cciOverbought.Value = value;
	}

	/// <summary>
	/// Stop distance from the entry in ATRs.
	/// </summary>
	public decimal StopLossAtr
	{
		get => _stopLossAtr.Value;
		set => _stopLossAtr.Value = value;
	}

	/// <summary>
	/// Period of the stop ATR.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
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
	public BollingerCciStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Period of the Bollinger Bands", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier of the bands", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period of CCI", "Indicators");

		_cciOversold = Param(nameof(CciOversold), -100m)
			.SetDisplay("CCI Oversold", "CCI level for longs", "Indicators");

		_cciOverbought = Param(nameof(CciOverbought), 100m)
			.SetDisplay("CCI Overbought", "CCI level for shorts", "Indicators");

		_stopLossAtr = Param(nameof(StopLossAtr), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss ATR", "Stop distance from the entry in ATRs", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of the stop ATR", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stopPrice = default;

		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerDeviation };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, cci, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, cci);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue cciValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed || !cciValue.IsFormed || !atrValue.IsFormed)
			return;

		var bands = (BollingerBandsValue)bollingerValue;

		if (bands.UpBand is not decimal upper || bands.LowBand is not decimal lower || bands.MovingAverage is not decimal middle)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var cci = cciValue.GetValue<decimal>();

		if (close < lower && cci < CciOversold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - StopLossAtr * atr;
		}
		else if (close > upper && cci > CciOverbought && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + StopLossAtr * atr;
		}
		else if (Position > 0 && (close >= middle || (StopLossAtr > 0 && close <= _stopPrice)))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (close <= middle || (StopLossAtr > 0 && close >= _stopPrice)))
		{
			BuyMarket(-Position);
		}
	}
}
