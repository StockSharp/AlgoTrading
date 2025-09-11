namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on Distance to Demand Vector indicator.
/// Enters long when distance to long vector exceeds distance to short vector and vice versa.
/// </summary>
public class DistanceToDemandVectorStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _highs = new();
	private readonly Queue<decimal> _lows = new();

	private decimal _prevLongDist;
	private decimal _prevShortDist;
	private bool _isPrevSet;

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DistanceToDemandVectorStrategy()
	{
		_length = Param(nameof(Length), 100)
			.SetDisplay("Length", "Lookback period", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_highs.Clear();
		_lows.Clear();
		_prevLongDist = default;
		_prevShortDist = default;
		_isPrevSet = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_highs.Enqueue(candle.HighPrice);
		_lows.Enqueue(candle.LowPrice);

		if (_highs.Count > Length)
			_highs.Dequeue();
		if (_lows.Count > Length)
			_lows.Dequeue();

		if (_highs.Count < Length || _lows.Count < Length)
			return;

		var hv = decimal.MinValue;
		var hvIndex = 0;
		var idx = 0;
		foreach (var h in _highs)
		{
			if (h >= hv)
			{
				hv = h;
				hvIndex = idx;
			}
			idx++;
		}
		var hb = _highs.Count - 1 - hvIndex;

		var lv = decimal.MaxValue;
		var lvIndex = 0;
		idx = 0;
		foreach (var l in _lows)
		{
			if (l <= lv)
			{
				lv = l;
				lvIndex = idx;
			}
			idx++;
		}
		var lb = _lows.Count - 1 - lvIndex;

		var denom = Math.Abs(hb - lb);
		if (denom == 0)
			return;

		var demandVector = (hv - lv) / denom;

		var distanceLong = candle.ClosePrice - (lv + demandVector * lb);
		var distanceShort = candle.ClosePrice - (hv - demandVector * hb);

		if (!_isPrevSet)
		{
			_prevLongDist = distanceLong;
			_prevShortDist = distanceShort;
			_isPrevSet = true;
			return;
		}

		var prevLongGreater = _prevLongDist > _prevShortDist;
		var currLongGreater = distanceLong > distanceShort;

		if (prevLongGreater != currLongGreater)
		{
			if (currLongGreater)
			{
				if (Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
			}
			else
			{
				if (Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			}
		}

		_prevLongDist = distanceLong;
		_prevShortDist = distanceShort;
	}
}
