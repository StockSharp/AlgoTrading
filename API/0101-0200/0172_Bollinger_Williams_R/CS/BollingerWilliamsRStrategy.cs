using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Williams %R strategy.
/// A close below the lower band with Williams %R below WilliamsROversold goes long and a close above the upper band with %R above
/// WilliamsROverbought goes short, reversing an opposite position. The position closes when price returns to the middle band.
/// The stop lies AtrMultiplier ATR from the entry close and is checked on candle closes.
/// </summary>
public class BollingerWilliamsRStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _williamsRPeriod;
	private readonly StrategyParam<decimal> _williamsROversold;
	private readonly StrategyParam<decimal> _williamsROverbought;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
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
	/// Period of Williams %R.
	/// </summary>
	public int WilliamsRPeriod
	{
		get => _williamsRPeriod.Value;
		set => _williamsRPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R level for longs.
	/// </summary>
	public decimal WilliamsROversold
	{
		get => _williamsROversold.Value;
		set => _williamsROversold.Value = value;
	}

	/// <summary>
	/// Williams %R level for shorts.
	/// </summary>
	public decimal WilliamsROverbought
	{
		get => _williamsROverbought.Value;
		set => _williamsROverbought.Value = value;
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
	/// Stop distance from the entry in ATRs.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	public BollingerWilliamsRStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Period of the Bollinger Bands", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier of the bands", "Indicators");

		_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Period of Williams %R", "Indicators");

		_williamsROversold = Param(nameof(WilliamsROversold), -80m)
			.SetDisplay("Williams %R Oversold", "Williams %R level for longs", "Indicators");

		_williamsROverbought = Param(nameof(WilliamsROverbought), -20m)
			.SetDisplay("Williams %R Overbought", "Williams %R level for shorts", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of the stop ATR", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "Stop distance from the entry in ATRs", "Risk");

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
		var williams = new WilliamsR { Length = WilliamsRPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, williams, atr, ProcessCandle)
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
				DrawIndicator(oscillators, williams);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue williamsValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed || !williamsValue.IsFormed || !atrValue.IsFormed)
			return;

		var bands = (BollingerBandsValue)bollingerValue;

		if (bands.UpBand is not decimal upper || bands.LowBand is not decimal lower || bands.MovingAverage is not decimal middle)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var williams = williamsValue.GetValue<decimal>();

		if (close < lower && williams < WilliamsROversold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - AtrMultiplier * atr;
		}
		else if (close > upper && williams > WilliamsROverbought && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + AtrMultiplier * atr;
		}
		else if (Position > 0 && (close >= middle || (AtrMultiplier > 0 && close <= _stopPrice)))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (close <= middle || (AtrMultiplier > 0 && close >= _stopPrice)))
		{
			BuyMarket(-Position);
		}
	}
}
