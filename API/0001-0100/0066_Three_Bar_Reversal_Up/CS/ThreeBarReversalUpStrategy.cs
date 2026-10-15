using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only three-bar reversal after an optional net downtrend.
/// A pattern-low stop and an opposite three-bar reversal close the full position.
/// </summary>
public class ThreeBarReversalUpStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _requireDowntrend;
	private readonly StrategyParam<int> _downtrendLength;

	private readonly List<ICandleMessage> _recent = new();
	private decimal? _patternStop;
	private Order _entryOrder;
	private Order _exitOrder;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public bool RequireDowntrend { get => _requireDowntrend.Value; set => _requireDowntrend.Value = value; }
	public int DowntrendLength { get => _downtrendLength.Value; set => _downtrendLength.Value = value; }

	public ThreeBarReversalUpStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Three-bar pattern timeframe", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetRange(0m, 99m)
			.SetDisplay("Stop below pattern low (%)", "Buffer below the lowest of all three pattern lows; zero places the stop at that low.", "Protection");
		_requireDowntrend = Param(nameof(RequireDowntrend), true)
			.SetDisplay("Require Downtrend", "Require a net Close decline before the signal candle.", "Pattern");
		_downtrendLength = Param(nameof(DowntrendLength), 5).SetRange(2, 100)
			.SetDisplay("Downtrend Length", "Number of preceding candles from first Close to last Close, including the two bearish pattern bars.", "Pattern");
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

	private bool CheckStop(decimal executableBid)
	{
		if (Position <= 0m || _patternStop is not decimal stop || executableBid > stop || IsPending(_exitOrder))
			return false;
		_exitOrder = SellMarket(Position);
		_recent.Clear();
		_patternStop = null;
		return true;
	}

	private void Append(ICandleMessage candle)
	{
		_recent.Add(candle);
		if (_recent.Count > DowntrendLength)
			_recent.RemoveAt(0);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		if (Position > 0m && CheckStop(candle.LowPrice))
			return;
		if (IsPending(_entryOrder) || IsPending(_exitOrder))
		{
			Append(candle);
			return;
		}
		var previous = _recent.Count >= 1 ? _recent[^1] : null;
		var older = _recent.Count >= 2 ? _recent[^2] : null;
		var upPattern = older is not null && previous.ClosePrice < previous.OpenPrice &&
			older.ClosePrice < older.OpenPrice && previous.LowPrice < older.LowPrice &&
			candle.ClosePrice > candle.OpenPrice && candle.ClosePrice > previous.HighPrice;
		var downPattern = older is not null && previous.ClosePrice > previous.OpenPrice &&
			older.ClosePrice > older.OpenPrice && previous.HighPrice > older.HighPrice &&
			candle.ClosePrice < candle.OpenPrice && candle.ClosePrice < previous.LowPrice;

		if (Position > 0m && downPattern && IsFormedAndOnlineAndAllowTrading())
		{
			_exitOrder = SellMarket(Position);
			_recent.Clear();
			_patternStop = null;
		}
		else if (Position == 0m && upPattern &&
			(!RequireDowntrend || _recent.Count >= DowntrendLength &&
				_recent[^1].ClosePrice < _recent[^DowntrendLength].ClosePrice) &&
			IsFormedAndOnlineAndAllowTrading())
		{
			_patternStop = Math.Min(Math.Min(older.LowPrice, previous.LowPrice), candle.LowPrice) *
				(1m - StopLossPercent / 100m);
			_entryOrder = BuyMarket(Volume);
		}
		Append(candle);
	}
}
