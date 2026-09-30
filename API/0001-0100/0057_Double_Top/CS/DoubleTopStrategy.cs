using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Short-only double top: two confirmed pivot highs followed by a bearish candle.
/// A pattern-high stop is checked against executable best asks and finished-bar highs.
/// </summary>
public class DoubleTopStrategy : Strategy
{
	private readonly StrategyParam<int> _distance;
	private readonly StrategyParam<decimal> _similarityPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _twoBack;
	private ICandleMessage _previous;
	private (int Bar, decimal High)? _lastPivot;
	private decimal? _candidateHigh;
	private int _candidateExpires;
	private decimal? _patternStop;
	private int _bar;
	private Order _entryOrder;
	private Order _exitOrder;

	public int Distance { get => _distance.Value; set => _distance.Value = value; }
	public decimal SimilarityPercent { get => _similarityPercent.Value; set => _similarityPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DoubleTopStrategy()
	{
		_distance = Param(nameof(Distance), 5).SetRange(3, 100)
			.SetDisplay("Distance", "Minimum bars between confirmed pivot highs", "Pattern");
		_similarityPercent = Param(nameof(SimilarityPercent), 2m).SetRange(0.1m, 5m)
			.SetDisplay("Similarity %", "Maximum relative difference between pivot highs", "Pattern");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetRange(0m, 99m)
			.SetDisplay("Stop above highs (%)", "Percentage buffer above the higher pattern high; zero places it at the high.", "Protection");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for pivot highs and bearish confirmation", "General");
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
		_candidateHigh = null;
		_candidateExpires = 0;
		_patternStop = null;
		_bar = 0;
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
			// An exited pattern cannot be reused to open another position.
			_twoBack = _previous = null;
			_lastPivot = null;
			_candidateHigh = null;
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
		if (Position < 0m)
		{
			// Covers missing/stale quotes or a gap; the next market order may fill beyond the stop.
			CheckStop(candle.HighPrice);
			return;
		}
		if (IsPending(_entryOrder) || IsPending(_exitOrder))
			return;
		if (older is not null && middle is not null &&
			middle.HighPrice > older.HighPrice && middle.HighPrice >= candle.HighPrice)
		{
			var pivot = (Bar: _bar - 1, High: middle.HighPrice);
			if (_lastPivot is { } first && pivot.Bar - first.Bar >= Distance &&
				Math.Abs(pivot.High - first.High) * 100m <= first.High * SimilarityPercent)
			{
				_candidateHigh = Math.Max(first.High, pivot.High);
				_candidateExpires = _bar + Distance;
			}
			_lastPivot = pivot;
		}
		if (_candidateHigh is not decimal high)
			return;
		if (_bar > _candidateExpires || candle.HighPrice > high)
		{
			_candidateHigh = null;
			return;
		}
		if (candle.ClosePrice >= candle.OpenPrice || !IsFormedAndOnlineAndAllowTrading())
			return;
		_patternStop = high * (1m + StopLossPercent / 100m);
		_entryOrder = SellMarket(Volume);
		_candidateHigh = null;
		_lastPivot = null;
	}
}
