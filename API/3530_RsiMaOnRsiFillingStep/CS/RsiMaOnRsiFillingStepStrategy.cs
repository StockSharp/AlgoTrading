namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// RSI crossing its own moving average with trade signals.
/// Converted from the MetaTrader expert advisor "RSI_MAonRSI_Filling Step EA.mq5".
/// </summary>
public class RsiMaOnRsiFillingStepStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _maPeriod;

	private RelativeStrengthIndex _rsi;
	private readonly Queue<decimal> _rsiHistory = new();
	private decimal? _previousRsi;
	private decimal? _previousSignal;

	/// <summary>
	/// Initializes a new instance of <see cref="RsiMaOnRsiFillingStepStrategy"/>.
	/// </summary>
	public RsiMaOnRsiFillingStepStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for RSI calculations.", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Number of bars for the RSI smoothing window.", "Indicators");

		_maPeriod = Param(nameof(MaPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Length of the moving average applied to the RSI.", "Indicators");
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI averaging period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Moving average period applied to the RSI output.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousRsi = null;
		_previousSignal = null;
		_rsiHistory.Clear();

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_rsi, ProcessCandle)
		.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawOwnTrades(priceArea);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Accumulate RSI values for moving average calculation
		_rsiHistory.Enqueue(rsiValue);
		while (_rsiHistory.Count > MaPeriod)
			_rsiHistory.Dequeue();

		if (!_rsi.IsFormed)
			return;

		// Need enough RSI values to compute the MA
		if (_rsiHistory.Count < MaPeriod)
		{
			_previousRsi = rsiValue;
			return;
		}

		// Calculate simple moving average of RSI
		var sum = 0m;
		foreach (var v in _rsiHistory)
			sum += v;
		var signalValue = sum / MaPeriod;

		if (_previousRsi is null || _previousSignal is null)
		{
			_previousRsi = rsiValue;
			_previousSignal = signalValue;
			return;
		}

		var crossUp = _previousRsi < _previousSignal && rsiValue > signalValue;
		var crossDown = _previousRsi > _previousSignal && rsiValue < signalValue;

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		if (crossUp)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		else if (crossDown)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			if (Position >= 0)
				SellMarket(volume);
		}

		_previousRsi = rsiValue;
		_previousSignal = signalValue;
	}
}
