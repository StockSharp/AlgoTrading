using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Amstell SL: Grid averaging strategy with ATR-based take profit and stop loss.
/// Adds positions on adverse moves and exits on profit/stop targets.
/// </summary>
public class AmstellSlStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _emaLength;

	private decimal _entryPrice;
	private decimal _prevEma;
	private int _gridCount;

	public AmstellSlStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetDisplay("EMA Length", "EMA trend filter.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_prevEma = 0;
		_gridCount = 0;

		var atr = new AverageTrueRange { Length = AtrLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrVal, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrVal <= 0 || _prevEma == 0)
		{
			_prevEma = emaVal;
			return;
		}

		var close = candle.ClosePrice;

		// Position management with grid and stops
		if (Position > 0)
		{
			if (close >= _entryPrice + atrVal * 1.5m)
			{
				SellMarket();
				_entryPrice = 0;
				_gridCount = 0;
			}
			else if (close <= _entryPrice - atrVal * 3m)
			{
				SellMarket();
				_entryPrice = 0;
				_gridCount = 0;
			}
			else if (_gridCount < 2 && close <= _entryPrice - atrVal * 1.2m)
			{
				_entryPrice = (_entryPrice + close) / 2m;
				_gridCount++;
				BuyMarket();
			}
		}
		else if (Position < 0)
		{
			if (close <= _entryPrice - atrVal * 1.5m)
			{
				BuyMarket();
				_entryPrice = 0;
				_gridCount = 0;
			}
			else if (close >= _entryPrice + atrVal * 3m)
			{
				BuyMarket();
				_entryPrice = 0;
				_gridCount = 0;
			}
			else if (_gridCount < 2 && close >= _entryPrice + atrVal * 1.2m)
			{
				_entryPrice = (_entryPrice + close) / 2m;
				_gridCount++;
				SellMarket();
			}
		}

		// Entry on EMA trend
		if (Position == 0)
		{
			if (close > emaVal && emaVal > _prevEma)
			{
				_entryPrice = close;
				_gridCount = 0;
				BuyMarket();
			}
			else if (close < emaVal && emaVal < _prevEma)
			{
				_entryPrice = close;
				_gridCount = 0;
				SellMarket();
			}
		}

		_prevEma = emaVal;
	}
}
