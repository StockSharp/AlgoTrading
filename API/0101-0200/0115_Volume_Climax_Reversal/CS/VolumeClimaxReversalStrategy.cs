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
/// Volume Climax Reversal strategy.
/// A climax is a candle closing in the direction of the trend (a bullish candle above the SMA or a bearish one below it)
/// with volume above VolumeMultiplier times the average of the previous MaPeriod candles. When the next candle retraces, the strategy
/// enters against the move while flat. It exits when price closes beyond the climax extreme, when volume spikes again, or at the percent stop.
/// </summary>
public class VolumeClimaxReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _volumes = [];
	// The climax of the previous candle: 1 up, -1 down, 0 none, and its extreme.
	private int _climax;
	private decimal _climaxExtreme;
	private decimal _exitLevel;

	/// <summary>
	/// Period of the trend SMA and of the volume average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// How many times the average volume a climax must exceed.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
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
	public VolumeClimaxReversalStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period of the trend SMA and of the volume average", "Indicators");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "How many times the average volume a climax must exceed", "Indicators");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_climax = 0;
		_climaxExtreme = default;
		_exitLevel = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_volumes.Clear();
		_climax = 0;
		_climaxExtreme = default;
		_exitLevel = default;

		var sma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, ProcessCandle)
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
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The spike is measured against the candles before this one.
		var average = _volumes.Count == MaPeriod ? _volumes.Average() : (decimal?)null;

		_volumes.Add(candle.TotalVolume);

		if (_volumes.Count > MaPeriod)
			_volumes.RemoveAt(0);

		var climax = _climax;
		var climaxExtreme = _climaxExtreme;
		_climax = 0;

		if (average is not decimal avgVolume || !smaValue.IsFormed)
			return;

		var ma = smaValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var spike = candle.TotalVolume > avgVolume * VolumeMultiplier;

		if (spike && close > candle.OpenPrice && close > ma)
		{
			_climax = 1;
			_climaxExtreme = candle.HighPrice;
		}
		else if (spike && close < candle.OpenPrice && close < ma)
		{
			_climax = -1;
			_climaxExtreme = candle.LowPrice;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position < 0)
		{
			if (close > _exitLevel || spike)
				BuyMarket(-Position);
		}
		else if (Position > 0)
		{
			if (close < _exitLevel || spike)
				SellMarket(Position);
		}
		else if (climax == 1 && close < candle.OpenPrice)
		{
			SellMarket(Volume);
			_exitLevel = climaxExtreme;
		}
		else if (climax == -1 && close > candle.OpenPrice)
		{
			BuyMarket(Volume);
			_exitLevel = climaxExtreme;
		}
	}
}
