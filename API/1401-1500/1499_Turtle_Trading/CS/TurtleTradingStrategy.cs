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
/// Turtle Trading strategy using RSI momentum with EMA trend filter.
/// </summary>
public class TurtleTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _entryLength;
	private readonly StrategyParam<int> _exitLength;
	private readonly StrategyParam<decimal> _stopMultiple;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private decimal _prevFast;
	private decimal _prevSlow;
	private int _cooldown;

	public int EntryLength { get => _entryLength.Value; set => _entryLength.Value = value; }
	public int ExitLength { get => _exitLength.Value; set => _exitLength.Value = value; }
	public decimal StopMultiple { get => _stopMultiple.Value; set => _stopMultiple.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TurtleTradingStrategy()
	{
		_entryLength = Param(nameof(EntryLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Entry Length", "Entry channel length", "General");

		_exitLength = Param(nameof(ExitLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Exit Length", "Exit channel length", "General");

		_stopMultiple = Param(nameof(StopMultiple), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Multiple", "StdDev multiple for stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0;
		_prevFast = 0;
		_prevSlow = 0;
		_cooldown = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = 14 };
		var emaFast = new ExponentialMovingAverage { Length = 8 };
		var emaSlow = new ExponentialMovingAverage { Length = 21 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, emaFast, emaSlow, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal emaFast, decimal emaSlow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevRsi == 0 || _prevFast == 0 || _prevSlow == 0)
		{
			_prevRsi = rsiVal;
			_prevFast = emaFast;
			_prevSlow = emaSlow;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevRsi = rsiVal;
			_prevFast = emaFast;
			_prevSlow = emaSlow;
			return;
		}

		var hist = emaFast - emaSlow;
		var histUp = hist > 0m;
		var histDown = hist < 0m;

		var rsiCrossUp = _prevRsi <= 50m && rsiVal > 50m;
		var rsiCrossDown = _prevRsi >= 50m && rsiVal < 50m;

		// Exit
		if (Position > 0 && rsiCrossDown)
		{
			SellMarket();
			_cooldown = 80;
		}
		else if (Position < 0 && rsiCrossUp)
		{
			BuyMarket();
			_cooldown = 80;
		}

		// Entry
		if (Position == 0)
		{
			if (rsiCrossUp && histUp)
			{
				BuyMarket();
				_cooldown = 80;
			}
			else if (rsiCrossDown && histDown)
			{
				SellMarket();
				_cooldown = 80;
			}
		}

		_prevRsi = rsiVal;
		_prevFast = emaFast;
		_prevSlow = emaSlow;
	}
}
