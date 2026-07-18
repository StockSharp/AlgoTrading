using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ComFracti: Fractal detection with RSI confirmation and ATR stops.
/// </summary>
public class ComFractiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _entryPrice;
	private decimal _h1, _h2, _h3, _h4, _h5;
	private decimal _l1, _l2, _l3, _l4, _l5;
	private int _barCount;

	public ComFractiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period.", "Indicators");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetDisplay("EMA Length", "Trend filter.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryPrice = 0;
		_barCount = 0;
		_h1 = _h2 = _h3 = _h4 = _h5 = 0;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_barCount = 0;
		_h1 = _h2 = _h3 = _h4 = _h5 = 0;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal emaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_h5 = _h4; _h4 = _h3; _h3 = _h2; _h2 = _h1;
		_h1 = candle.HighPrice;
		_l5 = _l4; _l4 = _l3; _l3 = _l2; _l2 = _l1;
		_l1 = candle.LowPrice;
		_barCount++;

		if (_barCount < 5 || atrVal <= 0)
			return;

		var close = candle.ClosePrice;

		var fractalUp = _h3 > _h1 && _h3 > _h2 && _h3 > _h4 && _h3 > _h5;
		var fractalDown = _l3 < _l1 && _l3 < _l2 && _l3 < _l4 && _l3 < _l5;

		if (Position > 0)
		{
			if (close >= _entryPrice + atrVal * 3m || close <= _entryPrice - atrVal * 2m || (fractalUp && rsiVal > 65))
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close <= _entryPrice - atrVal * 3m || close >= _entryPrice + atrVal * 2m || (fractalDown && rsiVal < 35))
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		if (Position == 0)
		{
			if (fractalDown && rsiVal < 55)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (fractalUp && rsiVal > 45)
			{
				_entryPrice = close;
				SellMarket();
			}
		}
	}
}
