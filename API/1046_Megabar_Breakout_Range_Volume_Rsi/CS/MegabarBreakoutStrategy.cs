using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MegabarBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _avgPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private readonly List<decimal> _volumes = new();
	private readonly List<decimal> _ranges = new();

	public int AvgPeriod { get => _avgPeriod.Value; set => _avgPeriod.Value = value; }
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MegabarBreakoutStrategy()
	{
		_avgPeriod = Param(nameof(AvgPeriod), 20);
		_multiplier = Param(nameof(Multiplier), 1.5m);
		_rsiPeriod = Param(nameof(RsiPeriod), 14);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_volumes.Clear();
		_ranges.Clear();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var vol = candle.TotalVolume;
		var range = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		_volumes.Add(vol);
		_ranges.Add(range);
		if (_volumes.Count > AvgPeriod) _volumes.RemoveAt(0);
		if (_ranges.Count > AvgPeriod) _ranges.RemoveAt(0);

		if (!_rsi.IsFormed || _volumes.Count < AvgPeriod)
			return;

		var avgVol = _volumes.Average();
		var avgRange = _ranges.Average();

		var volumeOk = vol > avgVol * Multiplier;
		var rangeOk = range > avgRange * Multiplier;

		if (candle.ClosePrice > candle.OpenPrice && volumeOk && rangeOk && rsiValue > 50 && Position <= 0)
			BuyMarket();
		else if (candle.ClosePrice < candle.OpenPrice && volumeOk && rangeOk && rsiValue < 50 && Position >= 0)
			SellMarket();
	}
}
