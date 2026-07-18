using System;
using System.Collections.Generic;

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

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _initialized;
	private decimal _stopPrice;
	private int _cooldown;

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
		_atrMultiplier = Param(nameof(AtrMultiplier), 3.0m).SetDisplay("ATR Mult", "ATR stop mult", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(25).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = default;
		_prevSlow = default;
		_initialized = false;
		_stopPrice = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_emaFast = new ExponentialMovingAverage { Length = EmaFastLen };
		_emaSlow = new ExponentialMovingAverage { Length = EmaSlowLen };
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

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		// Entry with RSI confirmation
		if (crossUp && rsi > 25 && rsi < 80 && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_stopPrice = candle.ClosePrice - atr * AtrMultiplier;
			_cooldown = 8;
		}
		else if (crossDown && rsi > 20 && rsi < 75 && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_stopPrice = candle.ClosePrice + atr * AtrMultiplier;
			_cooldown = 8;
		}

		// ATR stop exit
		if (Position > 0 && _stopPrice > 0 && candle.ClosePrice <= _stopPrice)
		{
			SellMarket();
			_stopPrice = 0;
			_cooldown = 10;
		}
		else if (Position < 0 && _stopPrice > 0 && candle.ClosePrice >= _stopPrice)
		{
			BuyMarket();
			_stopPrice = 0;
			_cooldown = 10;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
