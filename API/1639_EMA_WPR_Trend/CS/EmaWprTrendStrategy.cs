using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining EMA trend filter with Williams %R signals.
/// Buys at oversold levels and sells at overbought levels.
/// </summary>
public class EmaWprTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _wprRetracement;
	private readonly StrategyParam<DataType> _candleType;

	private bool _buyAllowed;
	private bool _sellAllowed;
	private decimal _entryPrice;
	private decimal _prevEma;
	private bool _hasPrevEma;
	private int _trendCounter;

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	public decimal WprRetracement
	{
		get => _wprRetracement.Value;
		set => _wprRetracement.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public EmaWprTrendStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA", "Indicators");

		_wprPeriod = Param(nameof(WprPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period", "%R length", "Indicators");

		_wprRetracement = Param(nameof(WprRetracement), 30m)
			.SetNotNegative()
			.SetDisplay("WPR Retracement", "Retracement for next trade", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_buyAllowed = true;
		_sellAllowed = true;
		_entryPrice = 0;
		_prevEma = 0;
		_hasPrevEma = false;
		_trendCounter = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var wpr = new WilliamsR { Length = WprPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, wpr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update trend
		if (_hasPrevEma)
		{
			if (emaValue > _prevEma)
				_trendCounter = Math.Min(_trendCounter + 1, 1);
			else if (emaValue < _prevEma)
				_trendCounter = Math.Max(_trendCounter - 1, -1);
			else
				_trendCounter = 0;
		}
		_prevEma = emaValue;
		_hasPrevEma = true;

		var price = candle.ClosePrice;

		// Exit: WPR at opposite extreme
		if (Position > 0 && wprValue >= 0)
		{
			SellMarket();
			_entryPrice = 0;
		}
		else if (Position < 0 && wprValue <= -100)
		{
			BuyMarket();
			_entryPrice = 0;
		}

		// Retracement flags
		if (wprValue > -100 + WprRetracement)
			_buyAllowed = true;

		if (wprValue < 0 - WprRetracement)
			_sellAllowed = true;

		var trendUp = _trendCounter >= 1;
		var trendDown = _trendCounter <= -1;

		if (Position <= 0 && _buyAllowed && wprValue <= -100 && trendUp)
		{
			BuyMarket();
			_entryPrice = price;
			_buyAllowed = false;
		}
		else if (Position >= 0 && _sellAllowed && wprValue >= 0 && trendDown)
		{
			SellMarket();
			_entryPrice = price;
			_sellAllowed = false;
		}
	}
}
