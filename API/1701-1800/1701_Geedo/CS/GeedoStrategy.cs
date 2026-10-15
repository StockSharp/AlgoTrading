using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time-based strategy comparing open prices with ATR-based stop/take.
/// </summary>
public class GeedoStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _openHistory = new();
	private decimal _entryPrice;

	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public GeedoStrategy()
	{
		_lookback = Param(nameof(Lookback), 6)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Open price lookback bars", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for stops", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_openHistory.Clear();
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new StandardDeviation { Length = AtrPeriod };
		SubscribeCandles(CandleType).Bind(atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished) return;

		_openHistory.Add(candle.OpenPrice);
		if (_openHistory.Count > Lookback + 1)
			_openHistory.RemoveAt(0);

		if (_openHistory.Count <= Lookback) return;
		if (atrVal <= 0) return;

		var close = candle.ClosePrice;

		// Exit check
		if (Position > 0 && _entryPrice > 0)
		{
			if (close <= _entryPrice - atrVal * 2 || close >= _entryPrice + atrVal * 1.5m)
			{
				SellMarket();
				_entryPrice = 0;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (close >= _entryPrice + atrVal * 2 || close <= _entryPrice - atrVal * 1.5m)
			{
				BuyMarket();
				_entryPrice = 0;
				return;
			}
		}

		var pastOpen = _openHistory[0];
		var currentOpen = _openHistory[^1];
		var diff = currentOpen - pastOpen;

		// Price rising => long
		if (diff > atrVal * 0.5m && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_entryPrice = close;
		}
		// Price falling => short
		else if (diff < -atrVal * 0.5m && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_entryPrice = close;
		}
	}
}
