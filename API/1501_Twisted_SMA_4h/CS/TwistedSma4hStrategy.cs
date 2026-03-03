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
/// Twisted SMA strategy using RSI momentum with EMA trend filter.
/// </summary>
public class TwistedSma4hStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _midLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _mainSmaLength;
	private readonly StrategyParam<int> _kamaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private decimal _prevFast;
	private decimal _prevSlow;
	private int _cooldown;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int MidLength { get => _midLength.Value; set => _midLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int MainSmaLength { get => _mainSmaLength.Value; set => _mainSmaLength.Value = value; }
	public int KamaLength { get => _kamaLength.Value; set => _kamaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TwistedSma4hStrategy()
	{
		_fastLength = Param(nameof(FastLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA Length", "Length of the fastest SMA", "SMA");

		_midLength = Param(nameof(MidLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Middle SMA Length", "Length of the middle SMA", "SMA");

		_slowLength = Param(nameof(SlowLength), 18)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA Length", "Length of the slow SMA", "SMA");

		_mainSmaLength = Param(nameof(MainSmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Main SMA Length", "Length of the main SMA", "SMA");

		_kamaLength = Param(nameof(KamaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("KAMA Length", "Length of KAMA", "KAMA");

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
