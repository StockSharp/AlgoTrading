using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fifty Five Median Slope: EMA slope direction with ATR stops.
/// </summary>
public class FiftyFiveMedianSlopeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _slopeShift;

	private decimal _entryPrice;
	private decimal _prevEma;
	private int _barCount;
	private readonly decimal[] _emaHistory = new decimal[20];

	public FiftyFiveMedianSlopeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_emaLength = Param(nameof(EmaLength), 55)
			.SetDisplay("EMA Length", "Moving average period.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");

		_slopeShift = Param(nameof(SlopeShift), 13)
			.SetDisplay("Slope Shift", "Bars between slope comparison.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

	public int SlopeShift
	{
		get => _slopeShift.Value;
		set => _slopeShift.Value = value;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_prevEma = 0;
		_barCount = 0;

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var len = Math.Min(SlopeShift + 1, _emaHistory.Length);
		var idx = _barCount % len;
		_emaHistory[idx] = emaVal;
		_barCount++;

		if (_barCount < len || atrVal <= 0)
			return;

		var shiftIdx = (_barCount - SlopeShift) % len;
		if (shiftIdx < 0) shiftIdx += len;
		var shiftedEma = _emaHistory[shiftIdx];

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (close >= _entryPrice + atrVal * 3m || close <= _entryPrice - atrVal * 1.5m || emaVal < shiftedEma)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (close <= _entryPrice - atrVal * 3m || close >= _entryPrice + atrVal * 1.5m || emaVal > shiftedEma)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		if (Position == 0)
		{
			if (emaVal > shiftedEma && _prevEma <= shiftedEma)
			{
				_entryPrice = close;
				BuyMarket();
			}
			else if (emaVal < shiftedEma && _prevEma >= shiftedEma)
			{
				_entryPrice = close;
				SellMarket();
			}
		}

		_prevEma = emaVal;
	}
}
