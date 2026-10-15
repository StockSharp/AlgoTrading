using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive KDJ (MTF) strategy.
/// KDJ is calculated on TimeFrame1, TimeFrame2 and TimeFrame3, each line is smoothed by an EMA of SmoothingLength and the three
/// timeframes are blended with the weights selected by WeightOption. An SMA of the blended J over TrendLength bars measures the trend:
/// above 50 the oversold/overbought levels are 40/80, otherwise 20/60. A long opens when J is below the oversold level and K crosses
/// above D, a short when J is above the overbought level and K crosses below D; the opposite signal reverses the position.
/// </summary>
public class AdaptiveKdjMtfStrategy : Strategy
{
	private sealed class KdjState
	{
		public readonly List<decimal> Highs = [];
		public readonly List<decimal> Lows = [];
		public decimal? K;
		public decimal? D;
		public decimal? SmoothK;
		public decimal? SmoothD;
		public decimal? SmoothJ;
		public int Count;
	}

	private readonly StrategyParam<DataType> _timeFrame1;
	private readonly StrategyParam<DataType> _timeFrame2;
	private readonly StrategyParam<DataType> _timeFrame3;
	private readonly StrategyParam<int> _kdjLength;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _weightOption;

	private KdjState[] _states;
	private SimpleMovingAverage _trend;
	private decimal? _prevK;
	private decimal? _prevD;

	/// <summary>
	/// Shortest timeframe, also used for signals.
	/// </summary>
	public DataType TimeFrame1
	{
		get => _timeFrame1.Value;
		set => _timeFrame1.Value = value;
	}

	/// <summary>
	/// Middle timeframe.
	/// </summary>
	public DataType TimeFrame2
	{
		get => _timeFrame2.Value;
		set => _timeFrame2.Value = value;
	}

	/// <summary>
	/// Longest timeframe.
	/// </summary>
	public DataType TimeFrame3
	{
		get => _timeFrame3.Value;
		set => _timeFrame3.Value = value;
	}

	/// <summary>
	/// KDJ lookback.
	/// </summary>
	public int KdjLength
	{
		get => _kdjLength.Value;
		set => _kdjLength.Value = value;
	}

	/// <summary>
	/// EMA length that smooths each timeframe's KDJ.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// SMA length of the trend strength.
	/// </summary>
	public int TrendLength
	{
		get => _trendLength.Value;
		set => _trendLength.Value = value;
	}

	/// <summary>
	/// Weighting of the timeframes: 1 favours the shortest (0.5/0.3/0.2), 2 is equal, 3 favours the longest (0.2/0.3/0.5).
	/// </summary>
	public int WeightOption
	{
		get => _weightOption.Value;
		set => _weightOption.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AdaptiveKdjMtfStrategy()
	{
		_timeFrame1 = Param(nameof(TimeFrame1), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Time Frame 1", "Shortest timeframe, also used for signals", "General");

		_timeFrame2 = Param(nameof(TimeFrame2), TimeSpan.FromMinutes(3).TimeFrame())
			.SetDisplay("Time Frame 2", "Middle timeframe", "General");

		_timeFrame3 = Param(nameof(TimeFrame3), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Time Frame 3", "Longest timeframe", "General");

		_kdjLength = Param(nameof(KdjLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("KDJ Length", "KDJ lookback", "KDJ");

		_smoothingLength = Param(nameof(SmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "EMA length that smooths each timeframe's KDJ", "KDJ");

		_trendLength = Param(nameof(TrendLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "SMA length of the trend strength", "KDJ");

		_weightOption = Param(nameof(WeightOption), 1)
			.SetRange(1, 3)
			.SetDisplay("Weight Option", "1 favours the shortest timeframe, 2 is equal, 3 favours the longest", "KDJ");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { TimeFrame1, TimeFrame2, TimeFrame3 }.Distinct().Select(dt => (Security, dt));
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_states = null;
		_trend = null;
		_prevK = null;
		_prevD = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_states = [new(), new(), new()];
		_trend = new SimpleMovingAverage { Length = TrendLength };
		_prevK = null;
		_prevD = null;

		var main = SubscribeCandles(TimeFrame1);
		main.Bind(ProcessMain).Start();
		SubscribeCandles(TimeFrame2).Bind(c => UpdateKdj(_states[1], c)).Start();
		SubscribeCandles(TimeFrame3).Bind(c => UpdateKdj(_states[2], c)).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, main);
			DrawOwnTrades(area);
		}
	}

	private void UpdateKdj(KdjState state, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		state.Highs.Add(candle.HighPrice);
		state.Lows.Add(candle.LowPrice);

		if (state.Highs.Count > KdjLength)
		{
			state.Highs.RemoveAt(0);
			state.Lows.RemoveAt(0);
		}

		if (state.Highs.Count < KdjLength)
			return;

		var highest = state.Highs.Max();
		var lowest = state.Lows.Min();
		var rsv = highest > lowest ? 100m * (candle.ClosePrice - lowest) / (highest - lowest) : 50m;

		// Classic KDJ: K and D are 1/3 smoothings of RSV and K.
		var k = ((state.K ?? 50m) * 2m + rsv) / 3m;
		var d = ((state.D ?? 50m) * 2m + k) / 3m;
		var j = 3m * k - 2m * d;
		state.K = k;
		state.D = d;

		var alpha = 2m / (SmoothingLength + 1m);
		state.SmoothK = state.SmoothK is decimal sk ? sk + alpha * (k - sk) : k;
		state.SmoothD = state.SmoothD is decimal sd ? sd + alpha * (d - sd) : d;
		state.SmoothJ = state.SmoothJ is decimal sj ? sj + alpha * (j - sj) : j;
		state.Count++;
	}

	private bool IsReady(KdjState state) => state.Count >= SmoothingLength;

	private void ProcessMain(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateKdj(_states[0], candle);

		if (!_states.All(IsReady))
			return;

		var (w1, w2, w3) = WeightOption switch
		{
			2 => (1m / 3m, 1m / 3m, 1m / 3m),
			3 => (0.2m, 0.3m, 0.5m),
			_ => (0.5m, 0.3m, 0.2m),
		};

		var k = w1 * _states[0].SmoothK.Value + w2 * _states[1].SmoothK.Value + w3 * _states[2].SmoothK.Value;
		var d = w1 * _states[0].SmoothD.Value + w2 * _states[1].SmoothD.Value + w3 * _states[2].SmoothD.Value;
		var j = w1 * _states[0].SmoothJ.Value + w2 * _states[1].SmoothJ.Value + w3 * _states[2].SmoothJ.Value;

		var trend = _trend.Process(j, candle.ServerTime, true).ToDecimal();

		var prevK = _prevK;
		var prevD = _prevD;
		_prevK = k;
		_prevD = d;

		if (!_trend.IsFormed || prevK is not decimal pk || prevD is not decimal pd)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bullish = trend > 50m;
		var buyLevel = bullish ? 40m : 20m;
		var sellLevel = bullish ? 80m : 60m;

		var crossUp = pk <= pd && k > d;
		var crossDown = pk >= pd && k < d;

		if (j < buyLevel && crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (j > sellLevel && crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
