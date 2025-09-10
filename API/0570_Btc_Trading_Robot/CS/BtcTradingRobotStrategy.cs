
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy using Donchian channels and trailing stop.
/// </summary>
public class BtcTradingRobotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _barsCount;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _trailingPercent;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;

	private decimal _prevClose;
	private decimal _entryPrice;
	private decimal _peakPrice;
	private decimal _valleyPrice;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Bars lookback for channel calculation.
	/// </summary>
	public int BarsCount
	{
		get => _barsCount.Value;
		set => _barsCount.Value = value;
	}

	/// <summary>
	/// Take profit as percent of price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss as percent of price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Trailing percentage of take profit.
	/// </summary>
	public decimal TrailingPercent
	{
		get => _trailingPercent.Value;
		set => _trailingPercent.Value = value;
	}

	/// <summary>
	/// Session start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Session end hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Initializes the strategy.
	/// </summary>
	public BtcTradingRobotStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_barsCount = Param(nameof(BarsCount), 5)
			.SetGreaterThanZero()
			.SetDisplay("Bars N", "Bars lookback", "General")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.05m);

		_stopLossPercent = Param(nameof(StopLossPercent), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("SL %", "Stop loss percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.5m, 0.05m);

		_trailingPercent = Param(nameof(TrailingPercent), 7m)
			.SetGreaterThanZero()
			.SetDisplay("Trail %", "Trailing percent of TP", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 15m, 1m);

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Session start hour", "Session");

		_endHour = Param(nameof(EndHour), 0)
			.SetDisplay("End Hour", "Session end hour", "Session");
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

		_prevClose = 0;
		_entryPrice = 0;
		_peakPrice = 0;
		_valleyPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var channel = new DonchianChannels { Length = BarsCount * 2 + 1 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(channel, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, channel);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue channelValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var dc = (DonchianChannelsValue)channelValue;

		if (dc.UpperBand is not decimal upper || dc.LowerBand is not decimal lower)
			return;

		var price = candle.ClosePrice;
		var tick = Security?.PriceStep ?? 1m;

		var tpPoints = price * TakeProfitPercent;
		var slPoints = price * StopLossPercent;
		var orderDist = tpPoints / 2m;
		var trailPoints = tpPoints * TrailingPercent / 100m;
		var trailTrigger = tpPoints * TrailingPercent / 100m;

		var hour = candle.CloseTime.LocalDateTime.Hour;
		var inSession = (StartHour == 0 || hour >= StartHour) && (EndHour == 0 || hour < EndHour);

		if (Position == 0 && inSession)
		{
			if (_prevClose < upper - orderDist * tick && candle.HighPrice >= upper && Position <= 0)
			{
				RegisterBuy();
				_entryPrice = price;
				_peakPrice = price;
				return;
			}

			if (_prevClose > lower + orderDist * tick && candle.LowPrice <= lower && Position >= 0)
			{
				RegisterSell();
				_entryPrice = price;
				_valleyPrice = price;
				return;
			}
		}

		if (Position > 0)
		{
			if (price > _peakPrice)
				_peakPrice = price;

			if (price >= _entryPrice + tpPoints || price <= _entryPrice - slPoints)
			{
				RegisterSell();
				ResetPosition();
				return;
			}

			var profit = _peakPrice - _entryPrice;
			if (profit >= trailTrigger && price <= _peakPrice - trailPoints)
			{
				RegisterSell();
				ResetPosition();
			}
		}
		else if (Position < 0)
		{
			if (price < _valleyPrice)
				_valleyPrice = price;

			if (price <= _entryPrice - tpPoints || price >= _entryPrice + slPoints)
			{
				RegisterBuy();
				ResetPosition();
				return;
			}

			var profit = _entryPrice - _valleyPrice;
			if (profit >= trailTrigger && price >= _valleyPrice + trailPoints)
			{
				RegisterBuy();
				ResetPosition();
			}
		}

		_prevClose = price;
	}

	private void ResetPosition()
	{
		_entryPrice = 0;
		_peakPrice = 0;
		_valleyPrice = 0;
	}
}
