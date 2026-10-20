namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Trades lower-timeframe closes outside the preceding complete higher-timeframe range.</summary>
public class Bitcoin1H15MBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TimeSpan> _higherTimeFrame;
	private readonly StrategyParam<decimal> _stopLossBuffer;
	private readonly StrategyParam<decimal> _riskRewardRatio;
	private DateTime _bucket, _lastTime;
	private decimal _high, _low;
	private decimal? _previousHigh, _previousLow;
	private int _count;
	private Order _pending;

	/// <summary>Finished lower timeframe candles.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Completed higher timeframe range.</summary>
	public TimeSpan HigherTimeFrame
	{
		get => _higherTimeFrame.Value;
		set => _higherTimeFrame.Value = value;
	}

	/// <summary>Absolute stop distance from actual entry fills.</summary>
	public decimal StopLossBuffer
	{
		get => _stopLossBuffer.Value;
		set => _stopLossBuffer.Value = value;
	}

	/// <summary>Profit distance divided by stop distance.</summary>
	public decimal RiskRewardRatio
	{
		get => _riskRewardRatio.Value;
		set => _riskRewardRatio.Value = value;
	}

	/// <summary>Initializes the fifteen-minute/hourly breakout and fixed protection.</summary>
	public Bitcoin1H15MBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("CandleType", "Finished lower timeframe candles", "Parameters");
		_higherTimeFrame = Param(nameof(HigherTimeFrame), TimeSpan.FromHours(1))
			.SetDisplay("HigherTimeFrame", "Completed higher timeframe range", "Parameters");
		_stopLossBuffer = Param(nameof(StopLossBuffer), 50m).SetGreaterThanZero()
			.SetDisplay("StopLossBuffer", "Absolute stop distance from actual entry fills", "Parameters");
		_riskRewardRatio = Param(nameof(RiskRewardRatio), 2m).SetGreaterThanZero()
			.SetDisplay("RiskRewardRatio", "Profit distance divided by stop distance", "Parameters");
		OrderRegistering += order => _pending = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_bucket = _lastTime = default;
		_high = _low = 0m;
		_previousHigh = _previousLow = null;
		_count = 0;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		var frame = CandleType.Arg is TimeSpan interval ? interval : TimeSpan.Zero;
		if (frame <= TimeSpan.Zero || HigherTimeFrame <= frame || HigherTimeFrame.Ticks % frame.Ticks != 0)
			throw new InvalidOperationException("HigherTimeFrame must be an integer multiple of the lower time frame.");
		base.OnStarted2(time);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quote = new Subscription(DataType.Level1, Security);
			quote.MarketData.BuildField = field;
			SubscribeLevel1(quote).Start();
		}
		var candles = SubscribeCandles(CandleType);
		candles.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, candles);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || candle.OpenTime <= _lastTime)
			return;
		var frame = (TimeSpan)CandleType.Arg;
		var bucket = new DateTime(candle.OpenTime.Ticks / HigherTimeFrame.Ticks * HigherTimeFrame.Ticks, DateTimeKind.Utc);
		if (bucket != _bucket)
		{
			var complete = _bucket != default && bucket == _bucket + HigherTimeFrame
				&& _count == HigherTimeFrame.Ticks / frame.Ticks && _lastTime + frame == bucket;
			_previousHigh = complete ? _high : null;
			_previousLow = complete ? _low : null;
			_bucket = bucket;
			_high = candle.HighPrice;
			_low = candle.LowPrice;
			_count = candle.OpenTime == bucket ? 1 : 0;
		}
		else
		{
			_count = _count > 0 && candle.OpenTime == _lastTime + frame ? _count + 1 : 0;
			_high = Math.Max(_high, candle.HighPrice);
			_low = Math.Min(_low, candle.LowPrice);
		}
		_lastTime = candle.OpenTime;
		if (_previousHigh is null || _previousLow is null || Position != 0m || !IsFormedAndOnlineAndAllowTrading()
			|| _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		var side = candle.ClosePrice > _previousHigh ? Sides.Buy : candle.ClosePrice < _previousLow ? Sides.Sell : (Sides?)null;
		if (side is null) return;
		StartProtection(new Unit(StopLossBuffer * RiskRewardRatio, UnitTypes.Absolute),
			new Unit(StopLossBuffer, UnitTypes.Absolute), useMarketOrders: true, isLocalStop: true);
		if (side == Sides.Buy) BuyMarket(Volume);
		else SellMarket(Volume);
	}
}
