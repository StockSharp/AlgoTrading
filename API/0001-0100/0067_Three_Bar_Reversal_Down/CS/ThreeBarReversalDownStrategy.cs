using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Short-only three-bar reversal after an optional net uptrend.
/// A pattern-high stop and an opposite three-bar reversal close the full position.
/// </summary>
public class ThreeBarReversalDownStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _requireUptrend;
	private readonly StrategyParam<int> _uptrendLength;

	private readonly List<ICandleMessage> _recent = new();
	private decimal? _patternStop;
	private Order _entryOrder;
	private Order _exitOrder;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public bool RequireUptrend { get => _requireUptrend.Value; set => _requireUptrend.Value = value; }
	public int UptrendLength { get => _uptrendLength.Value; set => _uptrendLength.Value = value; }

	public ThreeBarReversalDownStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Three-bar pattern timeframe", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetRange(0m, 99m)
			.SetDisplay("Stop above pattern high (%)", "Buffer above the highest of all three pattern highs; zero places the stop at that high.", "Protection");
		_requireUptrend = Param(nameof(RequireUptrend), true)
			.SetDisplay("Require Uptrend", "Require a net Close rise before the signal candle.", "Pattern");
		_uptrendLength = Param(nameof(UptrendLength), 5).SetRange(2, 100)
			.SetDisplay("Uptrend Length", "Number of preceding candles from first Close to last Close, including the two bullish pattern bars.", "Pattern");
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
		var asks = new Subscription(DataType.Level1, Security);
		asks.MarketData.BuildField = Level1Fields.BestAskPrice;
		SubscribeLevel1(asks).Bind(ProcessAsk).Start();
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

	private void ProcessAsk(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
			CheckStop(ask);
	}

	private bool CheckStop(decimal executableAsk)
	{
		if (Position >= 0m || _patternStop is not decimal stop || executableAsk < stop || IsPending(_exitOrder))
			return false;
		_exitOrder = BuyMarket(Math.Abs(Position));
		_recent.Clear();
		_patternStop = null;
		return true;
	}

	private void Append(ICandleMessage candle)
	{
		_recent.Add(candle);
		if (_recent.Count > UptrendLength)
			_recent.RemoveAt(0);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		if (Position < 0m && CheckStop(candle.HighPrice))
			return;
		if (IsPending(_entryOrder) || IsPending(_exitOrder))
		{
			Append(candle);
			return;
		}
		var previous = _recent.Count >= 1 ? _recent[^1] : null;
		var older = _recent.Count >= 2 ? _recent[^2] : null;
		var downPattern = older is not null && previous.ClosePrice > previous.OpenPrice &&
			older.ClosePrice > older.OpenPrice && previous.HighPrice > older.HighPrice &&
			candle.ClosePrice < candle.OpenPrice && candle.ClosePrice < previous.LowPrice;
		var upPattern = older is not null && previous.ClosePrice < previous.OpenPrice &&
			older.ClosePrice < older.OpenPrice && previous.LowPrice < older.LowPrice &&
			candle.ClosePrice > candle.OpenPrice && candle.ClosePrice > previous.HighPrice;

		if (Position < 0m && upPattern && IsFormedAndOnlineAndAllowTrading())
		{
			_exitOrder = BuyMarket(Math.Abs(Position));
			_recent.Clear();
			_patternStop = null;
		}
		else if (Position == 0m && downPattern &&
			(!RequireUptrend || _recent.Count >= UptrendLength &&
				_recent[^1].ClosePrice > _recent[^UptrendLength].ClosePrice) &&
			IsFormedAndOnlineAndAllowTrading())
		{
			_patternStop = Math.Max(Math.Max(older.HighPrice, previous.HighPrice), candle.HighPrice) *
				(1m + StopLossPercent / 100m);
			_entryOrder = SellMarket(Volume);
		}
		Append(candle);
	}
}
