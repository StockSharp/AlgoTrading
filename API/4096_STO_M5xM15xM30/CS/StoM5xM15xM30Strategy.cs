using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// STO M5xM15xM30: RSI momentum with dual EMA confirmation.
/// Uses RSI crossover of 50 level with fast/slow EMA alignment.
/// </summary>
public class StoM5xM15xM30Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _prevRsi;
	private decimal _entryPrice;

	public StoM5xM15xM30Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period.", "Indicators");

		_fastLength = Param(nameof(FastLength), 10)
			.SetDisplay("Fast EMA", "Fast EMA period.", "Indicators");

		_slowLength = Param(nameof(SlowLength), 30)
			.SetDisplay("Slow EMA", "Slow EMA period.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period for stops.", "Indicators");
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

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
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
		var fast = new ExponentialMovingAverage { Length = FastLength };
		var slow = new ExponentialMovingAverage { Length = SlowLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, fast, slow, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal fastVal, decimal slowVal, decimal atrVal)
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
			if (close <= _entryPrice - atrVal * 2m || close >= _entryPrice + atrVal * 3m)
			{
				SellMarket();
				_entryPrice = 0;
			}
			else if (rsiVal < 40 && fastVal < slowVal)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close >= _entryPrice + atrVal * 2m || close <= _entryPrice - atrVal * 3m)
			{
				BuyMarket();
				_entryPrice = 0;
			}
			else if (rsiVal > 60 && fastVal > slowVal)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Entry: RSI crosses 50 with EMA alignment
		if (Position == 0)
		{
			if (_prevRsi <= 50 && rsiVal > 50 && fastVal > slowVal)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (_prevRsi >= 50 && rsiVal < 50 && fastVal < slowVal)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevRsi = rsiVal;
	}
}
