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
/// VoVix experiment strategy using volatility ratio z-score.
/// Uses fast/slow StdDev ratio to detect volatility spikes, then trades in candle direction.
/// </summary>
public class VoVixExperimentStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _zWindow;
	private readonly StrategyParam<decimal> _entryZ;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _ratioBuffer = new();

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int ZWindow { get => _zWindow.Value; set => _zWindow.Value = value; }
	public decimal EntryZ { get => _entryZ.Value; set => _entryZ.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VoVixExperimentStrategy()
	{
		_fastLength = Param(nameof(FastLength), 13)
			.SetDisplay("Fast Length", "Period for fast StdDev", "Indicators")
			.SetGreaterThanZero();

		_slowLength = Param(nameof(SlowLength), 26)
			.SetDisplay("Slow Length", "Period for slow StdDev", "Indicators")
			.SetGreaterThanZero();

		_zWindow = Param(nameof(ZWindow), 50)
			.SetDisplay("Z-Score Window", "Lookback for z-score", "Indicators")
			.SetGreaterThanZero();

		_entryZ = Param(nameof(EntryZ), 1.0m)
			.SetDisplay("Entry Z", "Minimum z-score to enter", "Strategy");

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
		_ratioBuffer.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastSd = new StandardDeviation { Length = FastLength };
		var slowSd = new StandardDeviation { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSd, slowSd, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastSd, decimal slowSd)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (slowSd <= 0)
			return;

		var voVix = fastSd / slowSd;

		_ratioBuffer.Add(voVix);
		if (_ratioBuffer.Count > ZWindow)
			_ratioBuffer.RemoveAt(0);

		if (_ratioBuffer.Count < ZWindow)
			return;

		// Compute z-score of voVix
		var mean = _ratioBuffer.Average();
		var variance = _ratioBuffer.Sum(x => (x - mean) * (x - mean)) / _ratioBuffer.Count;
		var sd = (decimal)Math.Sqrt((double)variance);

		if (sd <= 0)
			return;

		var z = (voVix - mean) / sd;

		// High z-score = volatility spike
		var isSpike = z > EntryZ;
		var exit = z < 0;

		// Check exits first
		if (Position > 0 && exit)
		{
			SellMarket();
			return;
		}
		else if (Position < 0 && exit)
		{
			BuyMarket();
			return;
		}

		// Entries: on spike, trade in candle direction
		if (Position == 0 && isSpike)
		{
			if (candle.ClosePrice > candle.OpenPrice)
				BuyMarket();
			else if (candle.ClosePrice < candle.OpenPrice)
				SellMarket();
		}
	}
}
