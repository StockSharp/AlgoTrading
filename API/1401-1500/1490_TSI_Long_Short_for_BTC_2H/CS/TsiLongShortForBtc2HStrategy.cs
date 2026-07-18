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
/// True Strength Index-inspired breakout strategy.
/// Uses RSI as momentum oscillator with EMA trend filter.
/// </summary>
public class TsiLongShortForBtc2HStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _lookback;

	private decimal _prevRsi;
	private decimal _prevEmaFast;
	private decimal _prevEmaSlow;
	private int _cooldown;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }

	public TsiLongShortForBtc2HStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period", "Indicators");
		_lookback = Param(nameof(Lookback), 21)
			.SetDisplay("Lookback", "EMA slow period", "Indicators");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0;
		_prevEmaFast = 0;
		_prevEmaSlow = 0;
		_cooldown = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var emaFast = new ExponentialMovingAverage { Length = 10 };
		var emaSlow = new ExponentialMovingAverage { Length = Lookback };

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

		if (_prevRsi == 0 || _prevEmaFast == 0 || _prevEmaSlow == 0)
		{
			_prevRsi = rsiVal;
			_prevEmaFast = emaFast;
			_prevEmaSlow = emaSlow;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevRsi = rsiVal;
			_prevEmaFast = emaFast;
			_prevEmaSlow = emaSlow;
			return;
		}

		// RSI cross 50
		var rsiCrossUp = _prevRsi <= 50m && rsiVal > 50m;
		var rsiCrossDown = _prevRsi >= 50m && rsiVal < 50m;

		// EMA trend
		var trendUp = emaFast > emaSlow;
		var trendDown = emaFast < emaSlow;

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
			if (rsiCrossUp && trendUp)
			{
				BuyMarket();
				_cooldown = 80;
			}
			else if (rsiCrossDown && trendDown)
			{
				SellMarket();
				_cooldown = 80;
			}
		}

		_prevRsi = rsiVal;
		_prevEmaFast = emaFast;
		_prevEmaSlow = emaSlow;
	}
}
