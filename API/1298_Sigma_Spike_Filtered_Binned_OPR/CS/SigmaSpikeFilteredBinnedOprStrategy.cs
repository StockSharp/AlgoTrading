using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Sigma Spike Filtered Binned OPR strategy.
/// Builds an OPR histogram filtered by sigma spike and trades on extremes.
/// </summary>
public class SigmaSpikeFilteredBinnedOprStrategy : Strategy
{
	private readonly StrategyParam<int> _sigmaSpikeLength;
	private readonly StrategyParam<bool> _filterBySigmaSpike;
	private readonly StrategyParam<decimal> _sigmaSpikeThreshold;
	private readonly StrategyParam<int> _oprThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private readonly int[] _oprBins = new int[101];
	private StandardDeviation _returnsStdDev = null!;
	private decimal? _prevClose;
	private decimal? _prevStd;

	/// <summary>
	/// Sigma spike standard deviation length.
	/// </summary>
	public int SigmaSpikeLength
	{
		get => _sigmaSpikeLength.Value;
		set => _sigmaSpikeLength.Value = value;
	}

	/// <summary>
	/// Enable filtering by sigma spike threshold.
	/// </summary>
	public bool FilterBySigmaSpike
	{
		get => _filterBySigmaSpike.Value;
		set => _filterBySigmaSpike.Value = value;
	}

	/// <summary>
	/// Sigma spike threshold.
	/// </summary>
	public decimal SigmaSpikeThreshold
	{
		get => _sigmaSpikeThreshold.Value;
		set => _sigmaSpikeThreshold.Value = value;
	}

	/// <summary>
	/// Upper/lower OPR threshold.
	/// </summary>
	public int OprThreshold
	{
		get => _oprThreshold.Value;
		set => _oprThreshold.Value = value;
	}

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SigmaSpikeFilteredBinnedOprStrategy()
	{
		_sigmaSpikeLength = Param(nameof(SigmaSpikeLength), 20).SetCanOptimize(true).SetDisplay("Sigma spike stdev length");
		_filterBySigmaSpike = Param(nameof(FilterBySigmaSpike), true).SetDisplay("Filter by sigma spike threshold?");
		_sigmaSpikeThreshold = Param(nameof(SigmaSpikeThreshold), 2m).SetCanOptimize(true).SetDisplay("Sigma spike threshold");
		_oprThreshold = Param(nameof(OprThreshold), 10).SetCanOptimize(true).SetDisplay("Upper/lower OPR threshold");
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle type");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_returnsStdDev = new StandardDeviation { Length = SigmaSpikeLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevClose is null)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var ret = candle.ClosePrice / _prevClose.Value - 1m;
		var stdValue = _returnsStdDev.Process(ret);

		if (_prevStd is not null && _prevStd != 0m)
		{
			var ss = ret / _prevStd.Value;

			var opr = (candle.OpenPrice - candle.LowPrice) / (candle.HighPrice - candle.LowPrice) * 100m;
			var bin = (int)Math.Round(opr, MidpointRounding.AwayFromZero);
			bin = Math.Max(0, Math.Min(100, bin));

			if (!FilterBySigmaSpike || Math.Abs(ss) >= SigmaSpikeThreshold)
			{
				_oprBins[bin]++;

				var upper = 100 - OprThreshold;

				if (opr <= OprThreshold && Position <= 0)
					BuyMarket();
				else if (opr >= upper && Position >= 0)
					SellMarket();
			}
		}

		if (stdValue.IsFinal)
			_prevStd = stdValue.GetValue<decimal>();

		_prevClose = candle.ClosePrice;
	}
}
