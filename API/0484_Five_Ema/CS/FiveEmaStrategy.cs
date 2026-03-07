namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// 5 EMA Strategy - stores a signal when price closes beyond the EMA
/// and enters on breakout within the next few candles.
/// Uses stop-loss and take-profit based on risk/reward ratio.
/// </summary>
public class FiveEmaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _targetRR;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema;

	private decimal? _signalHigh;
	private decimal? _signalLow;
	private int? _signalIndex;
	private bool _isBuySignal;
	private bool _isSellSignal;

	private int _barIndex;
	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public decimal TargetRR
	{
		get => _targetRR.Value;
		set => _targetRR.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public FiveEmaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_emaLength = Param(nameof(EmaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Length of EMA", "EMA");

		_targetRR = Param(nameof(TargetRR), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Target R:R", "Reward to risk ratio", "Risk Management");

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

		_ema = null;
		_signalHigh = null;
		_signalLow = null;
		_signalIndex = null;
		_isBuySignal = false;
		_isSellSignal = false;
		_barIndex = 0;
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed)
			return;

		_barIndex++;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		// Check stop/target exits first (always)
		if (Position > 0 && _longStop is decimal ls && _longTarget is decimal lt)
		{
			if (low <= ls || high >= lt)
			{
				SellMarket(Math.Abs(Position));
				_longStop = null;
				_longTarget = null;
				_cooldownRemaining = CooldownBars;
			}
		}
		else if (Position < 0 && _shortStop is decimal ss && _shortTarget is decimal st)
		{
			if (high >= ss || low <= st)
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = null;
				_shortTarget = null;
				_cooldownRemaining = CooldownBars;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Signal detection: candle entirely below EMA = buy setup
		if (high < emaValue)
		{
			_signalHigh = high;
			_signalLow = low;
			_signalIndex = _barIndex;
			_isBuySignal = true;
			_isSellSignal = false;
		}
		// Signal detection: candle entirely above EMA = sell setup
		else if (low > emaValue)
		{
			_signalHigh = high;
			_signalLow = low;
			_signalIndex = _barIndex;
			_isBuySignal = false;
			_isSellSignal = true;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var withinWindow = _signalIndex is int idx && _barIndex > idx && _barIndex <= idx + 3;

		// Buy entry: breakout above signal high
		if (_isBuySignal && withinWindow && _signalHigh is decimal sh && high > sh && Position <= 0)
		{
			var sl = _signalLow ?? low;
			var risk = sh - sl;
			if (risk > 0)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				_longStop = sl;
				_longTarget = sh + risk * TargetRR;
				BuyMarket(Volume);
				_isBuySignal = false;
				_signalHigh = _signalLow = null;
				_signalIndex = null;
				_cooldownRemaining = CooldownBars;
			}
		}
		// Sell entry: breakdown below signal low
		else if (_isSellSignal && withinWindow && _signalLow is decimal slw && low < slw && Position >= 0)
		{
			var sl = _signalHigh ?? high;
			var risk = sl - slw;
			if (risk > 0)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				_shortStop = sl;
				_shortTarget = slw - risk * TargetRR;
				SellMarket(Volume);
				_isSellSignal = false;
				_signalHigh = _signalLow = null;
				_signalIndex = null;
				_cooldownRemaining = CooldownBars;
			}
		}
	}
}
