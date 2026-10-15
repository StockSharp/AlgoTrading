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
/// Ichimoku Volume strategy.
/// A close above the cloud with Tenkan-sen above Kijun-sen on volume above the average of the previous VolumeAvgPeriod candles goes long;
/// a close below the cloud with Tenkan-sen below Kijun-sen on such volume goes short, reversing an opposite position.
/// A long closes when price closes below the cloud and a short when it closes above it, and a percent stop limits the loss.
/// </summary>
public class IchimokuVolumeStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanPeriod;
	private readonly StrategyParam<int> _volumeAvgPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _volumes = [];

	/// <summary>
	/// Period of Tenkan-sen.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Period of Kijun-sen.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Period of Senkou Span B.
	/// </summary>
	public int SenkouSpanPeriod
	{
		get => _senkouSpanPeriod.Value;
		set => _senkouSpanPeriod.Value = value;
	}

	/// <summary>
	/// Previous candles the volume is averaged over.
	/// </summary>
	public int VolumeAvgPeriod
	{
		get => _volumeAvgPeriod.Value;
		set => _volumeAvgPeriod.Value = value;
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
	public IchimokuVolumeStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Period of Tenkan-sen", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Period of Kijun-sen", "Ichimoku");

		_senkouSpanPeriod = Param(nameof(SenkouSpanPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span Period", "Period of Senkou Span B", "Ichimoku");

		_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Average Period", "Previous candles the volume is averaged over", "Volume");

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

		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ichimoku, ProcessCandle)
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
			DrawIndicator(area, ichimoku);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Volume is compared with the candles before this one.
		var average = _volumes.Count == VolumeAvgPeriod ? _volumes.Average() : (decimal?)null;

		_volumes.Add(candle.TotalVolume);

		if (_volumes.Count > VolumeAvgPeriod)
			_volumes.RemoveAt(0);

		if (ichimokuValue is not IIchimokuValue { Tenkan: decimal tenkan, Kijun: decimal kijun, SenkouA: decimal senkouA, SenkouB: decimal senkouB })
			return;

		if (average is not decimal avgVolume || !IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var cloudTop = Math.Max(senkouA, senkouB);
		var cloudBottom = Math.Min(senkouA, senkouB);
		var surge = candle.TotalVolume > avgVolume;

		if (close > cloudTop && tenkan > kijun && surge && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < cloudBottom && tenkan < kijun && surge && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close < cloudBottom)
			SellMarket(Position);
		else if (Position < 0 && close > cloudTop)
			BuyMarket(-Position);
	}
}
