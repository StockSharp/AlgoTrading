using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Megabar Breakout (Range and Volume and RSI) strategy.
/// A megabar has a body larger than RangeMultiplier times the average body over RangeAveragePeriod candles and a volume
/// larger than VolumeMultiplier times the average volume over VolumeAveragePeriod candles. A bullish megabar buys when the
/// RsiMaPeriod moving average of RSI is above LongRsiThreshold, a bearish one sells when it is below ShortRsiThreshold,
/// reversing an opposite position. Exits are a take profit and stop loss in price steps.
/// </summary>
public class MegabarBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _volumeAveragePeriod;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _rangeAveragePeriod;
	private readonly StrategyParam<decimal> _rangeMultiplier;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiMaPeriod;
	private readonly StrategyParam<decimal> _longRsiThreshold;
	private readonly StrategyParam<decimal> _shortRsiThreshold;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<bool> _filterTradeHours;

	private SimpleMovingAverage _volumeAverage;
	private SimpleMovingAverage _rangeAverage;
	private SimpleMovingAverage _rsiAverage;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candles in the volume average.
	/// </summary>
	public int VolumeAveragePeriod
	{
		get => _volumeAveragePeriod.Value;
		set => _volumeAveragePeriod.Value = value;
	}

	/// <summary>
	/// Multiple of the average volume a megabar needs.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Candles in the body average.
	/// </summary>
	public int RangeAveragePeriod
	{
		get => _rangeAveragePeriod.Value;
		set => _rangeAveragePeriod.Value = value;
	}

	/// <summary>
	/// Multiple of the average body a megabar needs.
	/// </summary>
	public decimal RangeMultiplier
	{
		get => _rangeMultiplier.Value;
		set => _rangeMultiplier.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Period of the RSI moving average.
	/// </summary>
	public int RsiMaPeriod
	{
		get => _rsiMaPeriod.Value;
		set => _rsiMaPeriod.Value = value;
	}

	/// <summary>
	/// RSI average must be above this level to buy.
	/// </summary>
	public decimal LongRsiThreshold
	{
		get => _longRsiThreshold.Value;
		set => _longRsiThreshold.Value = value;
	}

	/// <summary>
	/// RSI average must be below this level to sell.
	/// </summary>
	public decimal ShortRsiThreshold
	{
		get => _shortRsiThreshold.Value;
		set => _shortRsiThreshold.Value = value;
	}

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Trade hours filter switch of the original script; the README documents no hours, so it has no effect.
	/// </summary>
	public bool FilterTradeHours
	{
		get => _filterTradeHours.Value;
		set => _filterTradeHours.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MegabarBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_volumeAveragePeriod = Param(nameof(VolumeAveragePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Average Period", "Candles in the volume average", "Megabar");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 3m)
			.SetNotNegative()
			.SetDisplay("Volume Multiplier", "Multiple of the average volume", "Megabar");

		_rangeAveragePeriod = Param(nameof(RangeAveragePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Range Average Period", "Candles in the body average", "Megabar");

		_rangeMultiplier = Param(nameof(RangeMultiplier), 4m)
			.SetNotNegative()
			.SetDisplay("Range Multiplier", "Multiple of the average body", "Megabar");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period", "RSI");

		_rsiMaPeriod = Param(nameof(RsiMaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI MA Period", "Period of the RSI moving average", "RSI");

		_longRsiThreshold = Param(nameof(LongRsiThreshold), 50m)
			.SetDisplay("Long RSI Threshold", "RSI average must be above to buy", "RSI");

		_shortRsiThreshold = Param(nameof(ShortRsiThreshold), 70m)
			.SetDisplay("Short RSI Threshold", "RSI average must be below to sell", "RSI");

		_takeProfit = Param(nameof(TakeProfit), 400)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit in price steps", "Risk");

		_stopLoss = Param(nameof(StopLoss), 300)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss in price steps", "Risk");

		_filterTradeHours = Param(nameof(FilterTradeHours), false)
			.SetDisplay("Filter Trade Hours", "Trade hours filter switch of the original script", "General");
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
		_volumeAverage = null;
		_rangeAverage = null;
		_rsiAverage = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_volumeAverage = new SimpleMovingAverage { Length = VolumeAveragePeriod };
		_rangeAverage = new SimpleMovingAverage { Length = RangeAveragePeriod };
		_rsiAverage = new SimpleMovingAverage { Length = RsiMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
			TakeProfit > 0 ? new Unit(TakeProfit * step, UnitTypes.Absolute) : new Unit(),
			StopLoss > 0 ? new Unit(StopLoss * step, UnitTypes.Absolute) : new Unit(),
			useMarketOrders: true, isLocalStop: true);

		// The take profit and stop loss have to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private static decimal Feed(SimpleMovingAverage average, decimal value, DateTime time)
		=> average.Process(new DecimalIndicatorValue(average, value, time) { IsFinal = true }).GetValue<decimal>();

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var averageVolume = Feed(_volumeAverage, candle.TotalVolume, candle.OpenTime);
		var averageBody = Feed(_rangeAverage, body, candle.OpenTime);

		if (!rsiValue.IsFormed)
			return;

		var rsiMa = Feed(_rsiAverage, rsiValue.GetValue<decimal>(), candle.OpenTime);

		if (!_volumeAverage.IsFormed || !_rangeAverage.IsFormed || !_rsiAverage.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var megabar = body > averageBody * RangeMultiplier && candle.TotalVolume > averageVolume * VolumeMultiplier;
		if (!megabar)
			return;

		if (candle.ClosePrice > candle.OpenPrice && rsiMa > LongRsiThreshold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (candle.ClosePrice < candle.OpenPrice && rsiMa < ShortRsiThreshold && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
