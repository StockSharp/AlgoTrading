using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Day trading strategy using EMA crossover with RSI and ATR stops.
/// </summary>
public class MacdRsiEmaBbAtrDayTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _emaFastLen;
	private readonly StrategyParam<int> _emaSlowLen;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _emaFast;
	private EMA _emaSlow;
	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;
	private decimal _entryPrice;
	private decimal _stopPrice;

	public int EmaFastLen { get => _emaFastLen.Value; set => _emaFastLen.Value = value; }
	public int EmaSlowLen { get => _emaSlowLen.Value; set => _emaSlowLen.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdRsiEmaBbAtrDayTradingStrategy()
	{
		_emaFastLen = Param(nameof(EmaFastLen), 9).SetDisplay("Fast EMA", "Fast EMA", "Indicators");
		_emaSlowLen = Param(nameof(EmaSlowLen), 21).SetDisplay("Slow EMA", "Slow EMA", "Indicators");
		_rsiLength = Param(nameof(RsiLength), 14).SetDisplay("RSI", "RSI period", "Indicators");
		_atrLength = Param(nameof(AtrLength), 14).SetDisplay("ATR", "ATR period", "Indicators");
		_atrMultiplier = Param(nameof(AtrMultiplier), 2m).SetDisplay("ATR Mult", "ATR stop mult", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevFast = 0; _prevSlow = 0; _initialized = false;
		_entryPrice = 0; _stopPrice = 0;

		_emaFast = new EMA { Length = EmaFastLen };
		_emaSlow = new EMA { Length = EmaSlowLen };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_emaFast, _emaSlow, _rsi, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_emaFast.IsFormed || !_emaSlow.IsFormed || !_rsi.IsFormed || !_atr.IsFormed)
			return;

		if (!_initialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_initialized = true;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		// Entry with RSI confirmation
		if (crossUp && rsi > 40 && rsi < 70 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_stopPrice = candle.ClosePrice - atr * AtrMultiplier;
		}
		else if (crossDown && rsi > 30 && rsi < 60 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_stopPrice = candle.ClosePrice + atr * AtrMultiplier;
		}

		// ATR stop exit
		if (Position > 0 && candle.ClosePrice <= _stopPrice)
			SellMarket(Math.Abs(Position));
		else if (Position < 0 && candle.ClosePrice >= _stopPrice)
			BuyMarket(Math.Abs(Position));

		_prevFast = fast;
		_prevSlow = slow;
	}
}
