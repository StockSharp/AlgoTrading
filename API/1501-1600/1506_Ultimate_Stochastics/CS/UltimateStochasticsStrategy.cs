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
/// Ultimate Stochastics strategy using RSI momentum with EMA trend filter.
/// </summary>
public class UltimateStochasticsStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private decimal _prevFast;
	private decimal _prevSlow;
	private int _cooldown;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int SmoothLength { get => _smoothLength.Value; set => _smoothLength.Value = value; }
	public decimal Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public decimal Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public UltimateStochasticsStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_smoothLength = Param(nameof(SmoothLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Smooth Length", "Smoothing for D line", "Indicators");

		_overbought = Param(nameof(Overbought), 70m)
			.SetDisplay("Overbought", "Overbought level", "Levels");

		_oversold = Param(nameof(Oversold), 30m)
			.SetDisplay("Oversold", "Oversold level", "Levels");

		_takeProfitPct = Param(nameof(TakeProfitPct), 2m)
			.SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");

		_stopLossPct = Param(nameof(StopLossPct), 1m)
			.SetGreaterThanZero()
			.SetDisplay("SL %", "Stop loss percent", "Risk");

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

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
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
