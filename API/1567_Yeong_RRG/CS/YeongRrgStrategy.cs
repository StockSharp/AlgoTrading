using System;
using System.Collections.Generic;
using Ecng.ComponentModel;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Yeong Relative Rotation Graph strategy.
/// Uses normalized relative strength and momentum to trade.
/// </summary>
public class YeongRrgStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _indexSecurity;

	private SimpleMovingAverage _rsSma;
	private RateOfChange _rsRoc;
	private SimpleMovingAverage _rsRatioMean;
	private StandardDeviation _rsRatioStd;
	private SimpleMovingAverage _rmRatioMean;
	private StandardDeviation _rmRatioStd;

	private decimal _lastIndexClose;
	private State _state = State.None;

	/// <summary>
	/// Period used for calculations.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
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
	/// Index security for relative strength.
	/// </summary>
	public Security IndexSecurity
	{
		get => _indexSecurity.Value;
		set => _indexSecurity.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="YeongRrgStrategy"/>.
	/// </summary>
	public YeongRrgStrategy()
	{
		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Period for SMA and ROC calculations", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_indexSecurity = Param<Security>(nameof(IndexSecurity))
			.SetDisplay("Index Security", "Security used as benchmark", "General")
			.SetRequired();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, CandleType),
			(IndexSecurity, CandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsSma = null;
		_rsRoc = null;
		_rsRatioMean = null;
		_rsRatioStd = null;
		_rmRatioMean = null;
		_rmRatioStd = null;
		_lastIndexClose = 0m;
		_state = State.None;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (IndexSecurity == null)
			throw new InvalidOperationException("Index security is not specified.");

		_rsSma = new SimpleMovingAverage { Length = Length };
		_rsRoc = new RateOfChange { Length = Length };
		_rsRatioMean = new SimpleMovingAverage { Length = Length };
		_rsRatioStd = new StandardDeviation { Length = Length };
		_rmRatioMean = new SimpleMovingAverage { Length = Length };
		_rmRatioStd = new StandardDeviation { Length = Length };

		var mainSubscription = SubscribeCandles(CandleType);
		var indexSubscription = SubscribeCandles(CandleType, security: IndexSecurity);

		mainSubscription
			.Bind(ProcessMainCandle)
			.Start();

		indexSubscription
			.Bind(ProcessIndexCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessIndexCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastIndexClose = candle.ClosePrice;
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_lastIndexClose == 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stockClose = candle.ClosePrice;
		var rs = (stockClose / _lastIndexClose) * 100m;

		var rsRatioValue = _rsSma.Process(rs, candle.ServerTime, true);
		if (!_rsSma.IsFormed)
			return;
		var rsRatio = rsRatioValue.ToDecimal();

		var rmRatioValue = _rsRoc.Process(rsRatio, candle.ServerTime, true);
		if (!_rsRoc.IsFormed)
			return;
		var rmRatio = rmRatioValue.ToDecimal();

		var meanRsRatioValue = _rsRatioMean.Process(rsRatio, candle.ServerTime, true);
		var stdRsRatioValue = _rsRatioStd.Process(rsRatio, candle.ServerTime, true);
		var meanRmRatioValue = _rmRatioMean.Process(rmRatio, candle.ServerTime, true);
		var stdRmRatioValue = _rmRatioStd.Process(rmRatio, candle.ServerTime, true);

		if (!_rsRatioMean.IsFormed || !_rsRatioStd.IsFormed || !_rmRatioMean.IsFormed || !_rmRatioStd.IsFormed)
			return;

		var meanRsRatio = meanRsRatioValue.ToDecimal();
		var stdRsRatio = stdRsRatioValue.ToDecimal();
		var meanRmRatio = meanRmRatioValue.ToDecimal();
		var stdRmRatio = stdRmRatioValue.ToDecimal();

		if (stdRsRatio == 0m || stdRmRatio == 0m)
			return;

		var jdkRs = 100m + ((rsRatio - meanRsRatio) / stdRsRatio + 1m);
		var jdkRm = 100m + ((rmRatio - meanRmRatio) / stdRmRatio + 1m);

		if (jdkRs > 100m && jdkRm > 100m)
			_state = State.Green;
		else if (jdkRs > 100m && jdkRm < 100m)
			_state = State.Yellow;
		else if (jdkRs < 100m && jdkRm < 100m)
			_state = State.Red;
		else if (jdkRs < 100m && jdkRm > 100m)
			_state = State.Blue;

		var buySignal = _state == State.Green && candle.ServerTime.Year >= 2010;
		var sellSignal = _state == State.Red;

		if (buySignal && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (sellSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}

	private enum State
	{
		None,
		Green,
		Yellow,
		Red,
		Blue
	}
}

