using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume Supertrend strategy.
/// A close above the Supertrend line on volume above the average of the previous VolumeAvgPeriod candles goes long and a close below it
/// on such volume goes short, reversing an opposite position. A long closes once the Supertrend turns down and a short once it turns up,
/// and a percent stop limits the loss.
/// </summary>
public class VolumeSupertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _volumes = [];

	/// <summary>
	/// Previous candles the volume is averaged over.
	/// </summary>
	public int VolumeAvgPeriod
	{
		get => _volumeAvgPeriod.Value;
		set => _volumeAvgPeriod.Value = value;
	}

	/// <summary>
	/// ATR period of Supertrend.
	/// </summary>
	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of Supertrend.
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	public VolumeSupertrendStrategy()
	{
		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Avg Period", "Previous candles the volume is averaged over", "Volume");

		_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Period", "ATR period of Supertrend", "Supertrend");

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Multiplier", "ATR multiplier of Supertrend", "Supertrend");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

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
		_volumes.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_volumes.Clear();

		var supertrend = new SuperTrend { Length = SupertrendPeriod, Multiplier = SupertrendMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(supertrend, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
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
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue supertrendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Volume is compared with the candles before this one.
		var average = _volumes.Count == VolumeAvgPeriod ? _volumes.Average() : (decimal?)null;

		_volumes.Add(candle.TotalVolume);

		if (_volumes.Count > VolumeAvgPeriod)
			_volumes.RemoveAt(0);

		if (!supertrendValue.IsFormed || supertrendValue is not SuperTrendIndicatorValue value || average is not decimal avgVolume)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var line = value.Value;
		var close = candle.ClosePrice;
		var surge = candle.TotalVolume > avgVolume;

		if (surge && close > line && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (surge && close < line && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && !value.IsUpTrend)
			SellMarket(Position);
		else if (Position < 0 && value.IsUpTrend)
			BuyMarket(-Position);
	}
}
