namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Adaptive SMI Ergodic Strategy - uses True Strength Index crossovers with signal line confirmation.
/// </summary>
public class AdaptiveSmiErgodicStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _firstLength;
	private readonly StrategyParam<int> _secondLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _oversoldThreshold;
	private readonly StrategyParam<decimal> _overboughtThreshold;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _previousTsi;
	private int _cooldownRemaining;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FirstLength { get => _firstLength.Value; set => _firstLength.Value = value; }
	public int SecondLength { get => _secondLength.Value; set => _secondLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public decimal OversoldThreshold { get => _oversoldThreshold.Value; set => _oversoldThreshold.Value = value; }
	public decimal OverboughtThreshold { get => _overboughtThreshold.Value; set => _overboughtThreshold.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AdaptiveSmiErgodicStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_firstLength = Param(nameof(FirstLength), 25)
			.SetGreaterThanZero()
			.SetDisplay("First Length", "First smoothing length for TSI", "TSI")
			.SetOptimize(10, 30, 5);

		_secondLength = Param(nameof(SecondLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Second Length", "Second smoothing length for TSI", "TSI")
			.SetOptimize(5, 20, 3);

		_signalLength = Param(nameof(SignalLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Signal EMA length", "TSI")
			.SetOptimize(3, 15, 2);

		_oversoldThreshold = Param(nameof(OversoldThreshold), -10m)
			.SetDisplay("Oversold Threshold", "Oversold level for TSI", "TSI");

		_overboughtThreshold = Param(nameof(OverboughtThreshold), 10m)
			.SetDisplay("Overbought Threshold", "Overbought level for TSI", "TSI");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_previousTsi = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousTsi = 0;

		var tsi = new TrueStrengthIndex
		{
			FirstLength = FirstLength,
			SecondLength = SecondLength,
			SignalLength = SignalLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(tsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, tsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue tsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var tv = (ITrueStrengthIndexValue)tsiValue;

		if (tv.Tsi is not decimal tsiVal || tv.Signal is not decimal signalVal)
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_previousTsi = tsiVal;
			return;
		}

		var crossAboveOversold = _previousTsi <= OversoldThreshold && tsiVal > OversoldThreshold;
		var crossBelowOverbought = _previousTsi >= OverboughtThreshold && tsiVal < OverboughtThreshold;

		if (crossAboveOversold && tsiVal > signalVal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (crossBelowOverbought && tsiVal < signalVal && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}

		_previousTsi = tsiVal;
	}
}
