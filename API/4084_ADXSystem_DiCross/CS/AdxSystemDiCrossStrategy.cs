using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ADX System DI Cross: trend-following using dual EMA crossover
/// (proxy for +DI/-DI cross) with ATR-based volatility filter.
/// </summary>
public class AdxSystemDiCrossStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrThreshold;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;

	public AdxSystemDiCrossStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_fastLength = Param(nameof(FastLength), 10)
			.SetDisplay("Fast EMA", "Fast EMA period (proxy for +DI).", "Indicators");

		_slowLength = Param(nameof(SlowLength), 20)
			.SetDisplay("Slow EMA", "Slow EMA period (proxy for -DI).", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period for trend strength.", "Indicators");

		_atrThreshold = Param(nameof(AtrThreshold), 50m)
			.SetDisplay("ATR Threshold", "Min ATR value for entry.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

	public decimal AtrThreshold
	{
		get => _atrThreshold.Value;
		set => _atrThreshold.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fast = new ExponentialMovingAverage { Length = FastLength };
		var slow = new ExponentialMovingAverage { Length = SlowLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, atr, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0 || _prevSlow == 0)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			return;
		}

		var bullishCross = _prevFast <= _prevSlow && fastVal > slowVal;
		var bearishCross = _prevFast >= _prevSlow && fastVal < slowVal;

		// Exit on opposite cross
		if (Position > 0 && bearishCross)
		{
			SellMarket();
			_entryPrice = 0;
		}
		else if (Position < 0 && bullishCross)
		{
			BuyMarket();
			_entryPrice = 0;
		}

		// Entry on cross + volatility filter
		if (Position == 0 && atrVal >= AtrThreshold)
		{
			if (bullishCross)
			{
				_entryPrice = candle.ClosePrice;
				BuyMarket();
			}
			else if (bearishCross)
			{
				_entryPrice = candle.ClosePrice;
				SellMarket();
			}
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}
