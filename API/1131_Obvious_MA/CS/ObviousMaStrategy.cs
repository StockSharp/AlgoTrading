using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ObviousMaStrategy : Strategy
{
	private readonly StrategyParam<int> _longEntryLength;
	private readonly StrategyParam<int> _shortEntryLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _obv;
	private readonly List<decimal> _obvHistory = new();

	public int LongEntryLength { get => _longEntryLength.Value; set => _longEntryLength.Value = value; }
	public int ShortEntryLength { get => _shortEntryLength.Value; set => _shortEntryLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ObviousMaStrategy()
	{
		_longEntryLength = Param(nameof(LongEntryLength), 50).SetGreaterThanZero();
		_shortEntryLength = Param(nameof(ShortEntryLength), 100).SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = 0;
		_obv = 0;
		_obvHistory.Clear();

		var sma = new SimpleMovingAverage { Length = Math.Max(LongEntryLength, ShortEntryLength) };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// OBV calculation
		if (_prevClose != 0)
		{
			if (candle.ClosePrice > _prevClose)
				_obv += candle.TotalVolume;
			else if (candle.ClosePrice < _prevClose)
				_obv -= candle.TotalVolume;
		}
		_prevClose = candle.ClosePrice;

		_obvHistory.Add(_obv);

		if (_obvHistory.Count < ShortEntryLength)
			return;

		// Calculate OBV MAs
		var longMa = _obvHistory.Skip(_obvHistory.Count - LongEntryLength).Take(LongEntryLength).Average();
		var shortMa = _obvHistory.Skip(_obvHistory.Count - ShortEntryLength).Take(ShortEntryLength).Average();

		var prevObv = _obvHistory.Count >= 2 ? _obvHistory[_obvHistory.Count - 2] : _obv;

		// Long: OBV crosses above long MA
		if (_obv > longMa && prevObv <= longMa && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
		}
		// Short: OBV crosses below short MA
		else if (_obv < shortMa && prevObv >= shortMa && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
		}
		// Exit long when OBV below long MA
		else if (Position > 0 && _obv < longMa)
		{
			SellMarket(Math.Abs(Position));
		}
		// Exit short when OBV above short MA
		else if (Position < 0 && _obv > shortMa)
		{
			BuyMarket(Math.Abs(Position));
		}

		// Keep history manageable
		if (_obvHistory.Count > ShortEntryLength * 2)
			_obvHistory.RemoveRange(0, _obvHistory.Count - ShortEntryLength * 2);
	}
}
