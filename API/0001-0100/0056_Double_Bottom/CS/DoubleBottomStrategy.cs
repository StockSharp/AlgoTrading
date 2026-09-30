using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only double bottom: two confirmed pivot lows followed by a bullish candle.
/// A pattern-low stop is checked against executable best bids and finished-bar lows.
/// </summary>
public class DoubleBottomStrategy : Strategy
{
	private readonly StrategyParam<int> _distance;
	private readonly StrategyParam<decimal> _similarityPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _twoBack;
	private ICandleMessage _previous;
	private (int Bar, decimal Low)? _lastPivot;
	private decimal? _candidateLow;
	private int _candidateExpires;
	private decimal? _patternStop;
	private int _bar;
	private Order _entryOrder;
	private Order _exitOrder;

	public int Distance { get => _distance.Value; set => _distance.Value = value; }
	public decimal SimilarityPercent { get => _similarityPercent.Value; set => _similarityPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DoubleBottomStrategy()
	{
		_distance = Param(nameof(Distance), 5).SetRange(3, 100)
			.SetDisplay("Distance", "Minimum bars between confirmed pivot lows", "Pattern");
		_similarityPercent = Param(nameof(SimilarityPercent), 2m).SetRange(0.1m, 5m)
			.SetDisplay("Similarity %", "Maximum relative difference between pivot lows", "Pattern");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetRange(0m, 99m)
			.SetDisplay("Stop below lows (%)", "Percentage buffer below the lower pattern low; zero places it at the low.", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for pivot lows and bullish confirmation", "General");
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
		_twoBack = _previous = null;
		_lastPivot = null;
		_candidateLow = null;
		_candidateExpires = 0;
		_patternStop = null;
		_bar = 0;
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
			// An exited pattern cannot be reused to open another position.
			_twoBack = _previous = null;
			_lastPivot = null;
			_candidateLow = null;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		_bar++;
		var older = _twoBack;
		var middle = _previous;
		_twoBack = middle;
		_previous = candle;
		if (Position > 0m)
		{
			// Covers missing/stale quotes or a gap; the next market order may fill beyond the stop.
			CheckStop(candle.LowPrice);
			return;
		}
		if (IsPending(_entryOrder) || IsPending(_exitOrder))
			return;
		if (older is not null && middle is not null &&
			middle.LowPrice < older.LowPrice && middle.LowPrice <= candle.LowPrice)
		{
			var pivot = (Bar: _bar - 1, Low: middle.LowPrice);
			if (_lastPivot is { } first && pivot.Bar - first.Bar >= Distance &&
				Math.Abs(pivot.Low - first.Low) * 100m <= first.Low * SimilarityPercent)
			{
				_candidateLow = Math.Min(first.Low, pivot.Low);
				_candidateExpires = _bar + Distance;
			}
			_lastPivot = pivot;
		}
		if (_candidateLow is not decimal low)
			return;
		if (_bar > _candidateExpires || candle.LowPrice < low)
		{
			_candidateLow = null;
			return;
		}
		if (candle.ClosePrice <= candle.OpenPrice || !IsFormedAndOnlineAndAllowTrading())
			return;
		_patternStop = low * (1m - StopLossPercent / 100m);
		_entryOrder = BuyMarket(Volume);
		_candidateLow = null;
		_lastPivot = null;
	}
}
