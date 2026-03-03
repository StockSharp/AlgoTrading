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
/// Correlation-based TSI with SuperTrend direction.
/// Uses RSI momentum with EMA trend for entry/exit decisions.
/// </summary>
public class TsiSuperTrendDecisionStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _tsiLength;
	private readonly StrategyParam<int> _stLength;
	private readonly StrategyParam<decimal> _stMultiplier;
	private readonly StrategyParam<decimal> _threshold;

	private decimal _prevRsi;
	private decimal _prevFast;
	private decimal _prevSlow;
	private int _cooldown;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int TsiLength { get => _tsiLength.Value; set => _tsiLength.Value = value; }
	public int StLength { get => _stLength.Value; set => _stLength.Value = value; }
	public decimal StMultiplier { get => _stMultiplier.Value; set => _stMultiplier.Value = value; }
	public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }

	public TsiSuperTrendDecisionStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_tsiLength = Param(nameof(TsiLength), 14)
			.SetDisplay("TSI Length", "RSI period", "Indicators");
		_stLength = Param(nameof(StLength), 8)
			.SetDisplay("ST Length", "Fast EMA length", "Indicators");
		_stMultiplier = Param(nameof(StMultiplier), 3m)
			.SetDisplay("ST Mult", "SuperTrend factor", "Indicators");
		_threshold = Param(nameof(Threshold), 21m)
			.SetDisplay("TSI Threshold", "Slow EMA length", "Trading");
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

		var rsi = new RelativeStrengthIndex { Length = TsiLength };
		var emaFast = new ExponentialMovingAverage { Length = StLength };
		var emaSlow = new ExponentialMovingAverage { Length = (int)Threshold };

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

		// EMA histogram (like MACD)
		var hist = emaFast - emaSlow;
		var histUp = hist > 0m;
		var histDown = hist < 0m;

		// RSI cross 50 as momentum trigger
		var rsiCrossUp = _prevRsi <= 50m && rsiVal > 50m;
		var rsiCrossDown = _prevRsi >= 50m && rsiVal < 50m;

		// Exit on opposite RSI cross
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

		// Entry: RSI cross + histogram confirms
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
