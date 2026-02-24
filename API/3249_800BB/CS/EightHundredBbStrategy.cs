using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 800BB strategy: trades Bollinger Band re-entries.
/// When price crosses back inside from below lower band - buy.
/// When price crosses back inside from above upper band - sell.
/// Uses ATR for volatility confirmation.
/// </summary>
public class EightHundredBbStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _atrPeriod;

	private bool _wasBelowLower;
	private bool _wasAboveUpper;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public EightHundredBbStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Band MA period", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB Deviation", "Standard deviation multiplier", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR lookback period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_wasBelowLower = false;
		_wasAboveUpper = false;

		var bb = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bb, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bbTyped = (BollingerBandsValue)bbValue;
		if (bbTyped.UpBand is not decimal upper || bbTyped.LowBand is not decimal lower)
			return;

		var atr = atrValue.ToDecimal();
		if (atr <= 0)
			return;

		var close = candle.ClosePrice;

		// Track when price goes outside bands
		if (close < lower)
			_wasBelowLower = true;
		if (close > upper)
			_wasAboveUpper = true;

		// Buy: was below lower band, now crossed back inside
		if (_wasBelowLower && close > lower && close < upper && Position <= 0)
		{
			BuyMarket();
			_wasBelowLower = false;
		}
		// Sell: was above upper band, now crossed back inside
		else if (_wasAboveUpper && close < upper && close > lower && Position >= 0)
		{
			SellMarket();
			_wasAboveUpper = false;
		}

		// Reset flags when inside bands
		if (close > lower && close < upper)
		{
			_wasBelowLower = false;
			_wasAboveUpper = false;
		}
	}
}
