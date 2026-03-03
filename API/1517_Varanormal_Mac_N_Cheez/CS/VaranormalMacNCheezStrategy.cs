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
/// Varanormal Mac N Cheez strategy using RSI momentum with EMA trend filter.
/// </summary>
public class VaranormalMacNCheezStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _dailyTarget;
	private readonly StrategyParam<decimal> _stopLossAmount;
	private readonly StrategyParam<decimal> _trailOffset;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private decimal _prevFast;
	private decimal _prevSlow;
	private int _cooldown;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public decimal DailyTarget { get => _dailyTarget.Value; set => _dailyTarget.Value = value; }
	public decimal StopLossAmount { get => _stopLossAmount.Value; set => _stopLossAmount.Value = value; }
	public decimal TrailOffset { get => _trailOffset.Value; set => _trailOffset.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VaranormalMacNCheezStrategy()
	{
		_fastLength = Param(nameof(FastLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast MA period", "General");

		_slowLength = Param(nameof(SlowLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow MA period", "General");

		_dailyTarget = Param(nameof(DailyTarget), 500m)
			.SetDisplay("Daily Target", "Daily profit target", "Risk");

		_stopLossAmount = Param(nameof(StopLossAmount), 200m)
			.SetDisplay("Stop Loss", "Stop loss amount", "Risk");

		_trailOffset = Param(nameof(TrailOffset), 100m)
			.SetDisplay("Trail Offset", "Trailing stop offset", "Risk");

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
