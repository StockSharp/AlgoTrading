using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from _HPCS_Inter7_MT4_EA_V01_We.mq4.
/// Sells when price crosses below the lower Bollinger band and buys when price crosses above the upper band.
/// </summary>
public class HpcsInter7Strategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _bandPercent;

	private decimal? _prevClose;
	private decimal? _prevLower;
	private decimal? _prevUpper;

	/// <summary>
	/// Initializes a new instance of the <see cref="HpcsInter7Strategy"/> class.
	/// </summary>
	public HpcsInter7Strategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Number of candles included in the Bollinger Bands calculation", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for the Bollinger Bands", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for Bollinger Bands", "General");

		_bandPercent = Param(nameof(BandPercent), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Band Percent", "MA percentage band width", "Indicators");
	}

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal BandPercent
	{
		get => _bandPercent.Value;
		set => _bandPercent.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = null;
		_prevLower = null;
		_prevUpper = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevClose = null;
		_prevLower = null;
		_prevUpper = null;

		var bollinger = new ExponentialMovingAverage { Length = BollingerLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var upper = middle * (1 + BandPercent);
		var lower = middle * (1 - BandPercent);

		if (_prevClose.HasValue && _prevLower.HasValue && _prevUpper.HasValue)
		{
			// Downward cross through the lower band - open short
			if (_prevClose.Value > _prevLower.Value && candle.ClosePrice < lower && Position >= 0)
			{
				SellMarket();
			}
			// Upward cross through the upper band - open long
			else if (_prevClose.Value < _prevUpper.Value && candle.ClosePrice > upper && Position <= 0)
			{
				BuyMarket();
			}
		}

		_prevClose = candle.ClosePrice;
		_prevLower = lower;
		_prevUpper = upper;
	}
}
