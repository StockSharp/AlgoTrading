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
/// Simple XAUUSD strategy with fixed profit and loss targets.
/// Buys and holds until hitting percent-based TP or SL, then waits cooldown.
/// </summary>
public class XauusdSimple20Profit100LossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tpPct;
	private readonly StrategyParam<decimal> _slPct;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private int _barsSinceExit;
	private decimal _entryPrice;

	public decimal TpPct { get => _tpPct.Value; set => _tpPct.Value = value; }
	public decimal SlPct { get => _slPct.Value; set => _slPct.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public XauusdSimple20Profit100LossStrategy()
	{
		_tpPct = Param(nameof(TpPct), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");

		_slPct = Param(nameof(SlPct), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("SL %", "Stop loss percent", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown", "Bars between trades", "Risk");

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
		_barsSinceExit = 100;
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 2 };

		_barsSinceExit = 100;
		_entryPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal _dummy)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceExit++;

		if (Position == 0 && _barsSinceExit >= CooldownBars)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			return;
		}

		if (Position > 0 && _entryPrice > 0)
		{
			var tp = _entryPrice * (1 + TpPct / 100m);
			var sl = _entryPrice * (1 - SlPct / 100m);

			if (candle.ClosePrice >= tp || candle.ClosePrice <= sl)
			{
				SellMarket();
				_barsSinceExit = 0;
				_entryPrice = 0;
			}
		}
	}
}
