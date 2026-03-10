using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// JMaster RSX: RSI-based momentum with EMA trend filter.
/// Buys when RSI exits oversold and price above EMA.
/// Sells when RSI exits overbought and price below EMA.
/// </summary>
public class JmasterRsxStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _overbought;
	private readonly StrategyParam<decimal> _oversold;

	private decimal _prevRsi;
	private decimal _entryPrice;

	public JmasterRsxStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period.", "Indicators");

		_emaLength = Param(nameof(EmaLength), 30)
			.SetDisplay("EMA Length", "EMA trend filter.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period for stops.", "Indicators");

		_overbought = Param(nameof(Overbought), 75m)
			.SetDisplay("Overbought", "RSI overbought level.", "Signals");

		_oversold = Param(nameof(Oversold), 25m)
			.SetDisplay("Oversold", "RSI oversold level.", "Signals");
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

	public decimal Overbought
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevRsi = 0;
		_entryPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRsi = 0;
		_entryPrice = 0;

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

		// Exit management
		if (Position > 0)
		{
			if (close <= _entryPrice - atrVal * 2m || close >= _entryPrice + atrVal * 3m || rsiVal > Overbought)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close >= _entryPrice + atrVal * 2m || close <= _entryPrice - atrVal * 3m || rsiVal < Oversold)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entry
		if (Position == 0)
		{
			if (_prevRsi < Oversold && rsiVal >= Oversold && close > emaVal)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (_prevRsi > Overbought && rsiVal <= Overbought && close < emaVal)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevRsi = rsiVal;
	}
}
