using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMA momentum strategy with shifted MA comparison.
/// Buys when current SMA is above shifted SMA, sells when below.
/// </summary>
public class MarneniMoneyTreeStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _smaBuffer = new decimal[31];
	private int _bufferIndex;
	private int _valuesCount;

	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MarneniMoneyTreeStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "SMA length", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR for stops", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		Array.Clear(_smaBuffer, 0, _smaBuffer.Length);
		_bufferIndex = 0;
		_valuesCount = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = SmaPeriod };
		var atr = new StandardDeviation { Length = AtrPeriod };

		SubscribeCandles(CandleType).Bind(sma, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished) return;

		_smaBuffer[_bufferIndex] = smaValue;
		_bufferIndex = (_bufferIndex + 1) % _smaBuffer.Length;
		if (_valuesCount < _smaBuffer.Length) _valuesCount++;

		if (_valuesCount < _smaBuffer.Length) return;
		if (atrValue <= 0) return;

		var idxCurrent = (_bufferIndex - 1 + _smaBuffer.Length) % _smaBuffer.Length;
		var idxShift4 = (_bufferIndex - 5 + _smaBuffer.Length) % _smaBuffer.Length;
		var idxShift30 = _bufferIndex % _smaBuffer.Length;

		var ma = _smaBuffer[idxShift4];
		var ma1 = _smaBuffer[idxCurrent];
		var ma2 = _smaBuffer[idxShift30];

		if (Position == 0)
		{
			if (ma > ma1 && ma < ma2)
				SellMarket();
			else if (ma < ma1 && ma > ma2)
				BuyMarket();
		}
		else if (Position > 0 && ma > ma1)
			SellMarket();
		else if (Position < 0 && ma < ma1)
			BuyMarket();
	}
}
