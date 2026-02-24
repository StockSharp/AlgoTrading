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
/// EMA crossover scalping strategy with ATR-based exits.
/// </summary>
public class UltimateScalpingV2Strategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<decimal> _slPct;
	private readonly StrategyParam<decimal> _tpPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;

	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public decimal SlPct { get => _slPct.Value; set => _slPct.Value = value; }
	public decimal TpPct { get => _tpPct.Value; set => _tpPct.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public UltimateScalpingV2Strategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators");

		_slPct = Param(nameof(SlPct), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("SL %", "Stop loss percent", "Risk");

		_tpPct = Param(nameof(TpPct), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");

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
		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var emaFast = new ExponentialMovingAverage { Length = FastEmaLength };
		var emaSlow = new ExponentialMovingAverage { Length = SlowEmaLength };

		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(emaFast, emaSlow, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var longCross = _prevFast <= _prevSlow && fast > slow;
		var shortCross = _prevFast >= _prevSlow && fast < slow;

		// Check SL/TP for existing positions
		if (Position > 0 && _entryPrice > 0)
		{
			var sl = _entryPrice * (1m - SlPct / 100m);
			var tp = _entryPrice * (1m + TpPct / 100m);
			if (candle.ClosePrice <= sl || candle.ClosePrice >= tp || shortCross)
			{
				SellMarket();
				_entryPrice = 0;
				_prevFast = fast;
				_prevSlow = slow;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var sl = _entryPrice * (1m + SlPct / 100m);
			var tp = _entryPrice * (1m - TpPct / 100m);
			if (candle.ClosePrice >= sl || candle.ClosePrice <= tp || longCross)
			{
				BuyMarket();
				_entryPrice = 0;
				_prevFast = fast;
				_prevSlow = slow;
				return;
			}
		}

		// Entry signals
		if (longCross && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (shortCross && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
