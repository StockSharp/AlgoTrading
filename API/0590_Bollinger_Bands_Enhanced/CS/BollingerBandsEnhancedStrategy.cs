using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands Enhanced strategy.
/// </summary>
public class BollingerBandsEnhancedStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbWidth;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopAtr;
	private readonly StrategyParam<decimal> _trailAtr;

	private decimal _entry;
	private bool _trail;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BbPeriod { get => _bbPeriod.Value; set => _bbPeriod.Value = value; }
	public decimal BbWidth { get => _bbWidth.Value; set => _bbWidth.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal StopAtr { get => _stopAtr.Value; set => _stopAtr.Value = value; }
	public decimal TrailAtr { get => _trailAtr.Value; set => _trailAtr.Value = value; }

	public BollingerBandsEnhancedStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
		_bbPeriod = Param(nameof(BbPeriod), 20).SetGreaterThanZero().SetDisplay("BB Period", "Bollinger period", "Bollinger");
		_bbWidth = Param(nameof(BbWidth), 2m).SetGreaterThanZero().SetDisplay("StdDev", "Deviation multiplier", "Bollinger");
		_emaPeriod = Param(nameof(EmaPeriod), 200).SetGreaterThanZero().SetDisplay("EMA Period", "EMA length", "Filters");
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR length", "Risk");
		_stopAtr = Param(nameof(StopAtr), 1.75m).SetGreaterThanZero().SetDisplay("Stop ATR", "ATR stop loss", "Risk");
		_trailAtr = Param(nameof(TrailAtr), 2.25m).SetGreaterThanZero().SetDisplay("Trail ATR", "ATR trailing activation", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entry = 0m;
		_trail = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bb = new BollingerBands { Length = BbPeriod, Width = BbWidth };
		var ema = new EMA { Length = EmaPeriod };
		var atr = new ATR { Length = AtrPeriod };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(bb, ema, atr, Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, bb);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage c, decimal mid, decimal up, decimal low, decimal emaVal, decimal atrVal)
	{
		if (c.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			var stop = _entry - StopAtr * atrVal;
			var act = _entry + TrailAtr * atrVal;
			if (!_trail && c.HighPrice >= act)
				_trail = true;

			var tp = _trail ? mid : (decimal?)null;
			if (c.LowPrice <= stop || (tp != null && c.ClosePrice < tp.Value))
			{
				RegisterSell();
				_entry = 0m;
				_trail = false;
			}
		}
		else if (c.LowPrice > emaVal && c.LowPrice <= low)
		{
			RegisterBuy();
			_entry = c.ClosePrice;
		}
	}
}

