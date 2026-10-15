using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only bullish body engulfing after optional consecutive bearish candles.
/// Protection sits below the lower of the two pattern lows.
/// </summary>
public class EngulfingBullishStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _requireDowntrend;
	private readonly StrategyParam<int> _downtrendBars;

	private readonly List<ICandleMessage> _recent = new();
	private decimal? _patternStop;
	private Order _entryOrder;
	private Order _exitOrder;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public bool RequireDowntrend { get => _requireDowntrend.Value; set => _requireDowntrend.Value = value; }
	public int DowntrendBars { get => _downtrendBars.Value; set => _downtrendBars.Value = value; }

	public EngulfingBullishStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Engulfing timeframe", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetRange(0m, 99m)
			.SetDisplay("Stop below pattern low (%)", "Buffer below the lower of the two pattern lows; zero places the stop at the low.", "Protection");
		_requireDowntrend = Param(nameof(RequireDowntrend), true)
			.SetDisplay("Require Downtrend", "Require consecutive bearish candles immediately before the engulfing candle.", "Pattern");
		_downtrendBars = Param(nameof(DowntrendBars), 3).SetRange(1, 100)
			.SetDisplay("Downtrend Bars", "Number of prior consecutive bearish candles, including the engulfed candle.", "Pattern");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ClearState();
	}

	private void ClearState()
	{
		_recent.Clear();
		_patternStop = null;
		_entryOrder = _exitOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ClearState();
		var bids = new Subscription(DataType.Level1, Security);
		bids.MarketData.BuildField = Level1Fields.BestBidPrice;
		SubscribeLevel1(bids).Bind(ProcessBid).Start();
		var candles = SubscribeCandles(CandleType);
		candles.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candles);
			DrawOwnTrades(area);
		}
	}

	private static bool IsPending(Order order)
		=> order is not null && order.State is not (OrderStates.Done or OrderStates.Failed);

	private void ProcessBid(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
			CheckStop(bid);
	}

	private void CheckStop(decimal executableBid)
	{
		if (Position > 0m && _patternStop is decimal stop && executableBid <= stop && !IsPending(_exitOrder))
		{
			_exitOrder = SellMarket(Position);
			_recent.Clear();
			_patternStop = null;
		}
	}

	private void Append(ICandleMessage candle)
	{
		_recent.Add(candle);
		if (_recent.Count > DowntrendBars)
			_recent.RemoveAt(0);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		if (Position > 0m)
		{
			// Bar-low fallback covers gaps or missing executable quote updates.
			CheckStop(candle.LowPrice);
			if (Position > 0m && !IsPending(_exitOrder))
				Append(candle);
			return;
		}
		if (IsPending(_entryOrder) || IsPending(_exitOrder))
		{
			Append(candle);
			return;
		}
		var previous = _recent.LastOrDefault();
		var downtrend = !RequireDowntrend ||
			_recent.Count >= DowntrendBars &&
			_recent.TakeLast(DowntrendBars).All(bar => bar.ClosePrice < bar.OpenPrice);
		var engulfing = previous is not null && previous.ClosePrice < previous.OpenPrice &&
			candle.ClosePrice > candle.OpenPrice &&
			candle.OpenPrice <= previous.ClosePrice &&
			candle.ClosePrice >= previous.OpenPrice;
		if (engulfing && downtrend && IsFormedAndOnlineAndAllowTrading())
		{
			_patternStop = Math.Min(previous.LowPrice, candle.LowPrice) * (1m - StopLossPercent / 100m);
			_entryOrder = BuyMarket(Volume);
		}
		Append(candle);
	}
}
