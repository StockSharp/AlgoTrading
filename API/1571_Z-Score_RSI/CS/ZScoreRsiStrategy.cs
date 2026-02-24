using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI of price Z-Score with EMA smoothing.
/// Computes Z-score of price, feeds it to RSI, smooths with EMA.
/// Buys when RSI crosses above its EMA and sells on opposite cross.
/// </summary>
public class ZScoreRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _zScoreLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = new();
	private decimal _prevRsiZ;
	private decimal _prevRsiMa;
	private bool _hasPrev;

	public int ZScoreLength { get => _zScoreLength.Value; set => _zScoreLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ZScoreRsiStrategy()
	{
		_zScoreLength = Param(nameof(ZScoreLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Z-Score Length", "Length for mean and deviation", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Length for RSI", "Indicators");

		_smoothingLength = Param(nameof(SmoothingLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("RSI EMA Length", "EMA length over RSI", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
		_prevRsiZ = 0;
		_prevRsiMa = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Use SMA + StdDev via Bind to compute Z-score components
		var sma = new SimpleMovingAverage { Length = ZScoreLength };
		var stdDev = new StandardDeviation { Length = ZScoreLength };

		_closes.Clear();
		_prevRsiZ = 0;
		_prevRsiMa = 0;
		_hasPrev = false;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal meanVal, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stdVal <= 0)
			return;

		// Compute Z-score
		var z = (candle.ClosePrice - meanVal) / stdVal;

		// Manual RSI on Z-score values
		_closes.Add(z);
		if (_closes.Count > RsiLength + SmoothingLength + 10)
			_closes.RemoveAt(0);

		if (_closes.Count < RsiLength + 1)
			return;

		// Calculate RSI manually on Z-score series
		decimal avgGain = 0, avgLoss = 0;
		for (int i = _closes.Count - RsiLength; i < _closes.Count; i++)
		{
			var change = _closes[i] - _closes[i - 1];
			if (change > 0) avgGain += change;
			else avgLoss += Math.Abs(change);
		}
		avgGain /= RsiLength;
		avgLoss /= RsiLength;

		decimal rsiZ;
		if (avgLoss == 0)
			rsiZ = 100;
		else
		{
			var rs = avgGain / avgLoss;
			rsiZ = 100 - (100 / (1 + rs));
		}

		// EMA smoothing of RSI
		decimal rsiMa;
		if (!_hasPrev)
		{
			rsiMa = rsiZ;
			_prevRsiZ = rsiZ;
			_prevRsiMa = rsiMa;
			_hasPrev = true;
			return;
		}

		var k = 2m / (SmoothingLength + 1);
		rsiMa = rsiZ * k + _prevRsiMa * (1 - k);

		// Crossover signals
		var crossUp = _prevRsiZ <= _prevRsiMa && rsiZ > rsiMa;
		var crossDown = _prevRsiZ >= _prevRsiMa && rsiZ < rsiMa;

		if (crossUp && Position <= 0)
		{
			BuyMarket();
		}
		else if (crossDown && Position >= 0)
		{
			SellMarket();
		}

		_prevRsiZ = rsiZ;
		_prevRsiMa = rsiMa;
	}
}
