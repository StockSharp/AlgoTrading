using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Heikin Ashi trend reversals with Donchian Channel breakouts and MACD confirmation.
/// </summary>
public class ParallelStrategiesStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevUpper;
	private decimal? _prevLower;
	private int? _prevTrend;

	// Heikin Ashi state
	private decimal _haOpen;
	private decimal _haClose;
	private bool _haInitialized;

	public int DonchianPeriod { get => _donchianPeriod.Value; set => _donchianPeriod.Value = value; }
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ParallelStrategiesStrategy()
	{
		_donchianPeriod = Param(nameof(DonchianPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Lookback for breakout calculation", "Indicators");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period", "Indicators");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevUpper = null;
		_prevLower = null;
		_prevTrend = null;
		_haOpen = 0;
		_haClose = 0;
		_haInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var donchian = new DonchianChannels { Length = DonchianPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal(
			new MovingAverageConvergenceDivergence(
				new ExponentialMovingAverage { Length = MacdSlow },
				new ExponentialMovingAverage { Length = MacdFast }),
			new ExponentialMovingAverage { Length = MacdSignal });

		SubscribeCandles(CandleType)
			.BindEx(donchian, macd, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var dc = (IDonchianChannelsValue)donchianValue;
		var macdV = (IMovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (dc.UpperBand is not decimal upper || dc.LowerBand is not decimal lower)
			return;

		if (macdV.Macd is not decimal macdLine || macdV.Signal is not decimal signalLine)
			return;

		// Compute Heikin Ashi manually
		var haCloseNew = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		decimal haOpenNew;
		if (!_haInitialized)
		{
			haOpenNew = (candle.OpenPrice + candle.ClosePrice) / 2m;
			_haInitialized = true;
		}
		else
		{
			haOpenNew = (_haOpen + _haClose) / 2m;
		}

		_haOpen = haOpenNew;
		_haClose = haCloseNew;

		var trend = haOpenNew < haCloseNew ? 1 : -1;

		if (_prevTrend is int prevTrend)
		{
			if (trend > 0 && prevTrend < 0 && macdLine > signalLine && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (trend < 0 && prevTrend > 0 && macdLine < signalLine && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevUpper = upper;
		_prevLower = lower;
		_prevTrend = trend;
	}
}
