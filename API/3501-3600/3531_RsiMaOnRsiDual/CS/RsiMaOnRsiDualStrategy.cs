using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual moving averages calculated on top of RSI values.
/// Fast RSI MA crossing slow RSI MA generates entry signals.
/// </summary>
public class RsiMaOnRsiDualStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastRsiPeriod;
	private readonly StrategyParam<int> _slowRsiPeriod;
	private readonly StrategyParam<int> _maPeriod;

	private RelativeStrengthIndex _fastRsi;
	private RelativeStrengthIndex _slowRsi;
	private readonly Queue<decimal> _fastRsiHistory = new();
	private readonly Queue<decimal> _slowRsiHistory = new();

	private decimal? _previousFastMa;
	private decimal? _previousSlowMa;

	public RsiMaOnRsiDualStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle type", "Candles processed by the strategy.", "General");

		_fastRsiPeriod = Param(nameof(FastRsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast RSI period", "Length of the fast RSI smoothing window.", "Indicators");

		_slowRsiPeriod = Param(nameof(SlowRsiPeriod), 28)
			.SetGreaterThanZero()
			.SetDisplay("Slow RSI period", "Length of the slow RSI smoothing window.", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MA period", "Number of RSI values averaged by the smoothing moving average.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastRsiPeriod
	{
		get => _fastRsiPeriod.Value;
		set => _fastRsiPeriod.Value = value;
	}

	public int SlowRsiPeriod
	{
		get => _slowRsiPeriod.Value;
		set => _slowRsiPeriod.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_previousFastMa = null;
		_previousSlowMa = null;
		_fastRsiHistory.Clear();
		_slowRsiHistory.Clear();
		_fastRsi = null!;
		_slowRsi = null!;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousFastMa = null;
		_previousSlowMa = null;
		_fastRsiHistory.Clear();
		_slowRsiHistory.Clear();

		_fastRsi = new RelativeStrengthIndex { Length = FastRsiPeriod };
		_slowRsi = new RelativeStrengthIndex { Length = SlowRsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastRsi, _slowRsi, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawOwnTrades(priceArea);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastRsiValue, decimal slowRsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_fastRsiHistory.Enqueue(fastRsiValue);
		_slowRsiHistory.Enqueue(slowRsiValue);
		while (_fastRsiHistory.Count > MaPeriod)
			_fastRsiHistory.Dequeue();
		while (_slowRsiHistory.Count > MaPeriod)
			_slowRsiHistory.Dequeue();

		if (!_fastRsi.IsFormed || !_slowRsi.IsFormed)
			return;

		if (_fastRsiHistory.Count < MaPeriod || _slowRsiHistory.Count < MaPeriod)
			return;

		// Calculate SMA of each RSI
		var fastSum = 0m;
		var fastHistory = _fastRsiHistory.ToArray();
		foreach (var v in fastHistory)
			fastSum += v;
		var fastMa = fastSum / MaPeriod;

		var slowSum = 0m;
		var slowHistory = _slowRsiHistory.ToArray();
		foreach (var v in slowHistory)
			slowSum += v;
		var slowMa = slowSum / MaPeriod;

		if (_previousFastMa is null || _previousSlowMa is null)
		{
			_previousFastMa = fastMa;
			_previousSlowMa = slowMa;
			return;
		}

		var crossUp = _previousFastMa < _previousSlowMa && fastMa > slowMa;
		var crossDown = _previousFastMa > _previousSlowMa && fastMa < slowMa;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		if (crossUp)
		{
			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (crossDown)
		{
			if (Position >= 0)
				SellMarket(volume);
		}

		_previousFastMa = fastMa;
		_previousSlowMa = slowMa;
	}
}
