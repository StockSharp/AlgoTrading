using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NtoQf: RSI + EMA multi-filter strategy with ATR trailing.
/// Combines RSI overbought/oversold with EMA trend confirmation.
/// </summary>
public class NtoQfStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _rsiLower;

	private decimal _prevRsi;
	private decimal _entryPrice;
	private decimal _trailStop;

	public NtoQfStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period.", "Indicators");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetDisplay("EMA Length", "EMA trend filter.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period for trailing.", "Indicators");

		_rsiUpper = Param(nameof(RsiUpper), 70m)
			.SetDisplay("RSI Upper", "Overbought level.", "Signals");

		_rsiLower = Param(nameof(RsiLower), 30m)
			.SetDisplay("RSI Lower", "Oversold level.", "Signals");
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

	public decimal RsiUpper
	{
		get => _rsiUpper.Value;
		set => _rsiUpper.Value = value;
	}

	public decimal RsiLower
	{
		get => _rsiLower.Value;
		set => _rsiLower.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevRsi = 0;
		_entryPrice = 0;
		_trailStop = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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

		if (_prevRsi == 0 || atrVal <= 0)
		{
			_prevRsi = rsiVal;
			return;
		}

		var close = candle.ClosePrice;

		// Trailing stop management
		if (Position > 0)
		{
			var newTrail = close - atrVal * 1.5m;
			if (newTrail > _trailStop)
				_trailStop = newTrail;

			if (close <= _trailStop || rsiVal > RsiUpper)
			{
				SellMarket();
				_entryPrice = 0;
				_trailStop = 0;
			}
		}
		else if (Position < 0)
		{
			var newTrail = close + atrVal * 1.5m;
			if (_trailStop == 0 || newTrail < _trailStop)
				_trailStop = newTrail;

			if (close >= _trailStop || rsiVal < RsiLower)
			{
				BuyMarket();
				_entryPrice = 0;
				_trailStop = 0;
			}
		}

		// Entry: RSI exits extreme zone, confirmed by EMA trend
		if (Position == 0)
		{
			if (_prevRsi < RsiLower && rsiVal >= RsiLower && close > emaVal)
			{
				_entryPrice = close;
				_trailStop = close - atrVal * 2m;
				BuyMarket();
			}
			else if (_prevRsi > RsiUpper && rsiVal <= RsiUpper && close < emaVal)
			{
				_entryPrice = close;
				_trailStop = close + atrVal * 2m;
				SellMarket();
			}
		}

		_prevRsi = rsiVal;
	}
}
