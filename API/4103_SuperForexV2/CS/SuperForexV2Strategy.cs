using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SuperForexV2: RSI threshold reversal with ATR trailing stop.
/// Buys when RSI below lower level, sells when above upper level.
/// </summary>
public class SuperForexV2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _upperLevel;
	private readonly StrategyParam<decimal> _lowerLevel;

	private decimal _entryPrice;
	private decimal _trailStop;

	public SuperForexV2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_rsiLength = Param(nameof(RsiLength), 4)
			.SetDisplay("RSI Length", "RSI period.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period for trailing.", "Indicators");

		_upperLevel = Param(nameof(UpperLevel), 62m)
			.SetDisplay("RSI Upper", "Overbought for shorts.", "Signals");

		_lowerLevel = Param(nameof(LowerLevel), 42m)
			.SetDisplay("RSI Lower", "Oversold for longs.", "Signals");
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

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal UpperLevel
	{
		get => _upperLevel.Value;
		set => _upperLevel.Value = value;
	}

	public decimal LowerLevel
	{
		get => _lowerLevel.Value;
		set => _lowerLevel.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_trailStop = 0;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrVal <= 0)
			return;

		var close = candle.ClosePrice;

		// Trailing stop + opposite RSI exit
		if (Position > 0)
		{
			var newTrail = close - atrVal * 1.5m;
			if (newTrail > _trailStop)
				_trailStop = newTrail;

			if (close <= _trailStop || rsiVal > UpperLevel)
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

			if (close >= _trailStop || rsiVal < LowerLevel)
			{
				BuyMarket();
				_entryPrice = 0;
				_trailStop = 0;
			}
		}

		// Entry on RSI levels
		if (Position == 0)
		{
			if (rsiVal < LowerLevel)
			{
				_entryPrice = close;
				_trailStop = close - atrVal * 2m;
				BuyMarket();
			}
			else if (rsiVal > UpperLevel)
			{
				_entryPrice = close;
				_trailStop = close + atrVal * 2m;
				SellMarket();
			}
		}
	}
}
