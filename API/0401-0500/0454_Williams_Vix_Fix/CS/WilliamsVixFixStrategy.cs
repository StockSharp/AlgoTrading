namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Williams VIX Fix Strategy.
/// VIX Fix = (highest close of WvfPeriod - low) / highest close * 100; the inverted VIX Fix uses (high - lowest close) / lowest close.
/// Each value is extreme when it reaches its own Bollinger upper band (BbLength, BbMultiplier) or the highest value of the last
/// WvfLookback bars times its percentile (HighestPercentile for the VIX Fix, LowestPercentile for the inverted one).
/// A long opens on an extreme VIX Fix with the close below the lower price Bollinger Band and closes on an extreme inverted
/// VIX Fix with the close above the upper band.
/// </summary>
public class WilliamsVixFixStrategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _wvfPeriod;
	private readonly StrategyParam<int> _wvfLookback;
	private readonly StrategyParam<decimal> _highestPercentile;
	private readonly StrategyParam<decimal> _lowestPercentile;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highestClose;
	private Lowest _lowestClose;
	private SimpleMovingAverage _wvfMean;
	private StandardDeviation _wvfDeviation;
	private Highest _wvfRange;
	private SimpleMovingAverage _invMean;
	private StandardDeviation _invDeviation;
	private Highest _invRange;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BbMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// Lookback of the highest and lowest close in the VIX Fix.
	/// </summary>
	public int WvfPeriod
	{
		get => _wvfPeriod.Value;
		set => _wvfPeriod.Value = value;
	}

	/// <summary>
	/// Lookback of the percentile thresholds.
	/// </summary>
	public int WvfLookback
	{
		get => _wvfLookback.Value;
		set => _wvfLookback.Value = value;
	}

	/// <summary>
	/// Percentile of the highest VIX Fix value that marks fear.
	/// </summary>
	public decimal HighestPercentile
	{
		get => _highestPercentile.Value;
		set => _highestPercentile.Value = value;
	}

	/// <summary>
	/// Percentile of the highest inverted VIX Fix value that marks complacency.
	/// </summary>
	public decimal LowestPercentile
	{
		get => _lowestPercentile.Value;
		set => _lowestPercentile.Value = value;
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
	public WilliamsVixFixStrategy()
	{
		_bbLength = Param(nameof(BbLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger");

		_bbMultiplier = Param(nameof(BbMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("BB Multiplier", "Bollinger Bands standard deviation multiplier", "Bollinger");

		_wvfPeriod = Param(nameof(WvfPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("WVF Period", "Lookback of the highest and lowest close", "VIX Fix");

		_wvfLookback = Param(nameof(WvfLookback), 50)
			.SetGreaterThanZero()
			.SetDisplay("WVF Lookback", "Lookback of the percentile thresholds", "VIX Fix");

		_highestPercentile = Param(nameof(HighestPercentile), 0.85m)
			.SetGreaterThanZero()
			.SetDisplay("Highest Percentile", "Percentile of the highest VIX Fix value", "VIX Fix");

		_lowestPercentile = Param(nameof(LowestPercentile), 0.99m)
			.SetGreaterThanZero()
			.SetDisplay("Lowest Percentile", "Percentile of the highest inverted VIX Fix value", "VIX Fix");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bollinger = new BollingerBands { Length = BbLength, Width = BbMultiplier };

		_highestClose = new Highest { Length = WvfPeriod };
		_lowestClose = new Lowest { Length = WvfPeriod };
		_wvfMean = new SimpleMovingAverage { Length = BbLength };
		_wvfDeviation = new StandardDeviation { Length = BbLength };
		_wvfRange = new Highest { Length = WvfLookback };
		_invMean = new SimpleMovingAverage { Length = BbLength };
		_invDeviation = new StandardDeviation { Length = BbLength };
		_invRange = new Highest { Length = WvfLookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		var highestClose = _highestClose.Process(candle.ClosePrice, time, true).ToDecimal();
		var lowestClose = _lowestClose.Process(candle.ClosePrice, time, true).ToDecimal();

		if (!_highestClose.IsFormed || !_lowestClose.IsFormed || highestClose <= 0m || lowestClose <= 0m)
			return;

		var wvf = (highestClose - candle.LowPrice) / highestClose * 100m;
		var inv = (candle.HighPrice - lowestClose) / lowestClose * 100m;

		var fear = IsExtreme(wvf, time, _wvfMean, _wvfDeviation, _wvfRange, HighestPercentile);
		var complacency = IsExtreme(inv, time, _invMean, _invDeviation, _invRange, LowestPercentile);

		if (fear is null || complacency is null)
			return;

		if (bollingerValue is not IBollingerBandsValue { UpBand: decimal upper, LowBand: decimal lower })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (fear == true && close < lower && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (complacency == true && close > upper && Position > 0)
			SellMarket(Position);
	}

	private bool? IsExtreme(decimal value, DateTime time, SimpleMovingAverage mean, StandardDeviation deviation, Highest range, decimal percentile)
	{
		var middle = mean.Process(value, time, true).ToDecimal();
		var spread = deviation.Process(value, time, true).ToDecimal();
		var highest = range.Process(value, time, true).ToDecimal();

		if (!mean.IsFormed || !deviation.IsFormed || !range.IsFormed)
			return null;

		var upperBand = middle + BbMultiplier * spread;
		return value >= upperBand || value >= highest * percentile;
	}
}
