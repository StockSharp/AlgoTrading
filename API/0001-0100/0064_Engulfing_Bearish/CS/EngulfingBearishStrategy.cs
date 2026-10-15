using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Short-only bearish body engulfing after optional consecutive bullish candles.
/// Protection sits above the higher of the two pattern highs.
/// </summary>
public class EngulfingBearishStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _requireUptrend;
	private readonly StrategyParam<int> _uptrendBars;

	private readonly List<ICandleMessage> _recent = new();
	private decimal? _patternStop;
	private Order _entryOrder;
	private Order _exitOrder;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public bool RequireUptrend { get => _requireUptrend.Value; set => _requireUptrend.Value = value; }
	public int UptrendBars { get => _uptrendBars.Value; set => _uptrendBars.Value = value; }

	public EngulfingBearishStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Engulfing timeframe", "General");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetRange(0m, 99m)
			.SetDisplay("Stop above pattern high (%)", "Buffer above the higher of the two pattern highs; zero places the stop at the high.", "Protection");
		_requireUptrend = Param(nameof(RequireUptrend), true)
			.SetDisplay("Require Uptrend", "Require consecutive bullish candles immediately before the engulfing candle.", "Pattern");
		_uptrendBars = Param(nameof(UptrendBars), 3).SetRange(1, 100)
			.SetDisplay("Uptrend Bars", "Number of prior consecutive bullish candles, including the engulfed candle.", "Pattern");
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

	private void CheckStop(decimal executableAsk)
	{
		if (Position < 0m && _patternStop is decimal stop && executableAsk >= stop && !IsPending(_exitOrder))
		{
			_exitOrder = BuyMarket(Math.Abs(Position));
			_recent.Clear();
			_patternStop = null;
		}
	}

	private void Append(ICandleMessage candle)
	{
		_recent.Add(candle);
		if (_recent.Count > UptrendBars)
			_recent.RemoveAt(0);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		if (Position < 0m)
		{
			// Bar-high fallback covers gaps or missing executable quote updates.
			CheckStop(candle.HighPrice);
			if (Position < 0m && !IsPending(_exitOrder))
				Append(candle);
			return;
		}
		if (IsPending(_entryOrder) || IsPending(_exitOrder))
		{
			Append(candle);
			return;
		}
		var previous = _recent.LastOrDefault();
		var uptrend = !RequireUptrend ||
			_recent.Count >= UptrendBars &&
			_recent.TakeLast(UptrendBars).All(bar => bar.ClosePrice > bar.OpenPrice);
		var engulfing = previous is not null && previous.ClosePrice > previous.OpenPrice &&
			candle.ClosePrice < candle.OpenPrice &&
			candle.OpenPrice >= previous.ClosePrice &&
			candle.ClosePrice <= previous.OpenPrice;
		if (engulfing && uptrend && IsFormedAndOnlineAndAllowTrading())
		{
			_patternStop = Math.Max(previous.HighPrice, candle.HighPrice) * (1m + StopLossPercent / 100m);
			_entryOrder = SellMarket(Volume);
		}
		Append(candle);
	}
}
