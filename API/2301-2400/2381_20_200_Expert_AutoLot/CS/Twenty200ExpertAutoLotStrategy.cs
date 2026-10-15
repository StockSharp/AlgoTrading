using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// "20/200 expert v4.2 AntS" strategy.
/// Opens trades based on the difference between two past open prices.
/// Closes positions by stop-loss, take-profit or time.
/// </summary>
public class Twenty200ExpertAutoLotStrategy : Strategy
{
	private readonly StrategyParam<int> _takeProfitLong;
	private readonly StrategyParam<int> _stopLossLong;
	private readonly StrategyParam<int> _takeProfitShort;
	private readonly StrategyParam<int> _stopLossShort;
	private readonly StrategyParam<int> _t1;
	private readonly StrategyParam<int> _t2;
	private readonly StrategyParam<int> _deltaLong;
	private readonly StrategyParam<int> _deltaShort;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _opens = new();

	private decimal _stopPrice;
	private decimal _takePrice;
	private DateTimeOffset _entryTime;
	private bool _isLong;

	public int TakeProfitLong { get => _takeProfitLong.Value; set => _takeProfitLong.Value = value; }
	public int StopLossLong { get => _stopLossLong.Value; set => _stopLossLong.Value = value; }
	public int TakeProfitShort { get => _takeProfitShort.Value; set => _takeProfitShort.Value = value; }
	public int StopLossShort { get => _stopLossShort.Value; set => _stopLossShort.Value = value; }
	public int T1 { get => _t1.Value; set => _t1.Value = value; }
	public int T2 { get => _t2.Value; set => _t2.Value = value; }
	public int DeltaLong { get => _deltaLong.Value; set => _deltaLong.Value = value; }
	public int DeltaShort { get => _deltaShort.Value; set => _deltaShort.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Twenty200ExpertAutoLotStrategy()
	{
		_takeProfitLong = Param(nameof(TakeProfitLong), 39)
			.SetDisplay("TP Long (pips)", "Take profit for long", "Risk");
		_stopLossLong = Param(nameof(StopLossLong), 147)
			.SetDisplay("SL Long (pips)", "Stop loss for long", "Risk");
		_takeProfitShort = Param(nameof(TakeProfitShort), 32)
			.SetDisplay("TP Short (pips)", "Take profit for short", "Risk");
		_stopLossShort = Param(nameof(StopLossShort), 267)
			.SetDisplay("SL Short (pips)", "Stop loss for short", "Risk");
		_t1 = Param(nameof(T1), 6)
			.SetDisplay("T1", "First bar shift", "Logic");
		_t2 = Param(nameof(T2), 2)
			.SetDisplay("T2", "Second bar shift", "Logic");
		_deltaLong = Param(nameof(DeltaLong), 1)
			.SetDisplay("Delta Long", "Min rise in pips", "Logic");
		_deltaShort = Param(nameof(DeltaShort), 1)
			.SetDisplay("Delta Short", "Min fall in pips", "Logic");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_opens.Clear();
		_stopPrice = 0m;
		_takePrice = 0m;
		_entryTime = default;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_opens.Clear();
		_stopPrice = 0m;
		_takePrice = 0m;
		_entryTime = default;
		_isLong = false;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_opens.Add(candle.OpenPrice);

		var maxShift = Math.Max(T1, T2);
		if (_opens.Count <= maxShift)
			return;

		var pip = Security?.PriceStep ?? 1m;
		var openT1 = _opens[_opens.Count - 1 - T1];
		var openT2 = _opens[_opens.Count - 1 - T2];

		// Position management: check SL/TP
		if (Position != 0)
		{
			if (_isLong)
			{
				if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				{
					SellMarket();
					return;
				}
			}
			else
			{
				if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				{
					BuyMarket();
					return;
				}
			}
			return;
		}

		// Entry logic
		var diffShort = openT1 - openT2;
		var diffLong = openT2 - openT1;

		if (diffShort > DeltaShort * pip && Position >= 0)
		{
			SellMarket();
			_isLong = false;
			_entryTime = candle.OpenTime;
			_stopPrice = candle.OpenPrice + StopLossShort * pip;
			_takePrice = candle.OpenPrice - TakeProfitShort * pip;
		}
		else if (diffLong > DeltaLong * pip && Position <= 0)
		{
			BuyMarket();
			_isLong = true;
			_entryTime = candle.OpenTime;
			_stopPrice = candle.OpenPrice - StopLossLong * pip;
			_takePrice = candle.OpenPrice + TakeProfitLong * pip;
		}
	}
}
