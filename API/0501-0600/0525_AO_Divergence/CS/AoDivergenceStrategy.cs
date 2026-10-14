using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AO divergence strategy.
/// The Awesome Oscillator is the difference of a FastLength and a SlowLength moving average (SMA, or EMA when UseEma is set) of
/// the median price. Oscillator swing lows and highs are confirmed as pivots with Lookback bars on each side. A pivot low where
/// price made a lower low than at the previous pivot low while AO made a higher low is a bullish divergence and goes long; a
/// pivot high where price made a higher high while AO made a lower high is a bearish divergence and goes short. The opposite
/// divergence reverses the position.
/// </summary>
public class AoDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<bool> _useEma;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<(decimal ao, decimal low, decimal high)> _bars = [];
	private IIndicator _fastMa;
	private IIndicator _slowMa;
	private (decimal ao, decimal price)? _lastPivotLow;
	private (decimal ao, decimal price)? _lastPivotHigh;

	/// <summary>
	/// Fast moving average period of AO.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow moving average period of AO.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Bars on each side that confirm an AO pivot.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Use EMA instead of SMA for AO.
	/// </summary>
	public bool UseEma
	{
		get => _useEma.Value;
		set => _useEma.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AoDivergenceStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast moving average period of AO", "Indicator");

		_slowLength = Param(nameof(SlowLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow moving average period of AO", "Indicator");

		_lookback = Param(nameof(Lookback), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Bars on each side that confirm an AO pivot", "Divergence");

		_useEma = Param(nameof(UseEma), false)
			.SetDisplay("Use EMA", "Use EMA instead of SMA for AO", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_fastMa = null;
		_slowMa = null;
		ResetState();
	}

	private void ResetState()
	{
		_bars.Clear();
		_lastPivotLow = null;
		_lastPivotHigh = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		_fastMa = UseEma ? new ExponentialMovingAverage { Length = FastLength } : new SimpleMovingAverage { Length = FastLength };
		_slowMa = UseEma ? new ExponentialMovingAverage { Length = SlowLength } : new SimpleMovingAverage { Length = SlowLength };

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

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var fast = _fastMa.Process(median, candle.ServerTime, true).ToDecimal();
		var slow = _slowMa.Process(median, candle.ServerTime, true).ToDecimal();

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
			return;

		_bars.Add((fast - slow, candle.LowPrice, candle.HighPrice));

		var window = 2 * Lookback + 1;
		if (_bars.Count > window)
			_bars.RemoveAt(0);

		if (_bars.Count < window)
			return;

		// The middle bar is an AO pivot once Lookback bars on each side have finished.
		var (pivotAo, pivotLow, pivotHigh) = _bars[Lookback];
		var isPivotLow = true;
		var isPivotHigh = true;

		for (var i = 0; i < window; i++)
		{
			if (i == Lookback)
				continue;

			if (_bars[i].ao <= pivotAo)
				isPivotLow = false;

			if (_bars[i].ao >= pivotAo)
				isPivotHigh = false;
		}

		var bullish = false;
		var bearish = false;

		if (isPivotLow)
		{
			if (_lastPivotLow is (decimal prevAo, decimal prevLow))
				bullish = pivotLow < prevLow && pivotAo > prevAo;

			_lastPivotLow = (pivotAo, pivotLow);
		}

		if (isPivotHigh)
		{
			if (_lastPivotHigh is (decimal prevAo, decimal prevHigh))
				bearish = pivotHigh > prevHigh && pivotAo < prevAo;

			_lastPivotHigh = (pivotAo, pivotHigh);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (bullish && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (bearish && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
