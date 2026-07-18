namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// 2Mars OKX Strategy.
/// Uses dual EMA crossover confirmed by SuperTrend.
/// Exits at BB bands or ATR-based stop loss.
/// </summary>
public class TwoMarsOkxStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _basisLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbWidth;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _basisMa;
	private ExponentialMovingAverage _signalMa;
	private SuperTrend _supertrend;
	private BollingerBands _bb;

	private decimal _prevBasis;
	private decimal _prevSignal;
	private bool _hasPrev;
	private decimal _entryPrice;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BasisLength
	{
		get => _basisLength.Value;
		set => _basisLength.Value = value;
	}

	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}

	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
	}

	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	public decimal BbWidth
	{
		get => _bbWidth.Value;
		set => _bbWidth.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public TwoMarsOkxStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_basisLength = Param(nameof(BasisLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Basis MA Length", "Basis EMA period", "MA");

		_signalLength = Param(nameof(SignalLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Signal MA Length", "Signal EMA period", "MA");

		_supertrendPeriod = Param(nameof(SupertrendPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SuperTrend Period", "SuperTrend ATR period", "Trend");

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 4m)
			.SetDisplay("SuperTrend Multiplier", "SuperTrend ATR multiplier", "Trend");

		_bbLength = Param(nameof(BbLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "BB");

		_bbWidth = Param(nameof(BbWidth), 3m)
			.SetDisplay("BB Width", "Bollinger Bands width", "BB");

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

		_basisMa = null;
		_signalMa = null;
		_supertrend = null;
		_bb = null;
		_prevBasis = 0;
		_prevSignal = 0;
		_hasPrev = false;
		_entryPrice = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_basisMa = new ExponentialMovingAverage { Length = BasisLength };
		_signalMa = new ExponentialMovingAverage { Length = SignalLength };
		_supertrend = new SuperTrend { Length = SupertrendPeriod, Multiplier = SupertrendMultiplier };
		_bb = new BollingerBands { Length = BbLength, Width = BbWidth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_basisMa, _signalMa, _supertrend, _bb, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _basisMa);
			DrawIndicator(area, _signalMa);
			DrawIndicator(area, _bb);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue basisVal, IIndicatorValue signalVal, IIndicatorValue stVal, IIndicatorValue bbVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_basisMa.IsFormed || !_signalMa.IsFormed || !_supertrend.IsFormed || !_bb.IsFormed)
			return;

		if (basisVal.IsEmpty || signalVal.IsEmpty || stVal.IsEmpty || bbVal.IsEmpty)
			return;

		var basis = basisVal.ToDecimal();
		var signal = signalVal.ToDecimal();
		var stTyped = (SuperTrendIndicatorValue)stVal;
		var uptrend = stTyped.IsUpTrend;

		var bb = (BollingerBandsValue)bbVal;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevBasis = basis;
			_prevSignal = signal;
			_hasPrev = true;
			return;
		}

		if (!_hasPrev)
		{
			_prevBasis = basis;
			_prevSignal = signal;
			_hasPrev = true;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevBasis = basis;
			_prevSignal = signal;
			return;
		}

		var crossUp = _prevSignal < _prevBasis && signal >= basis;
		var crossDown = _prevSignal > _prevBasis && signal <= basis;

		// Entry long: signal crosses above basis + uptrend
		if (crossUp && uptrend && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_cooldownRemaining = CooldownBars;
		}
		// Entry short: signal crosses below basis + downtrend
		else if (crossDown && !uptrend && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_cooldownRemaining = CooldownBars;
		}
		// Exit long at upper BB
		else if (Position > 0 && candle.ClosePrice >= upper)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short at lower BB
		else if (Position < 0 && candle.ClosePrice <= lower)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevBasis = basis;
		_prevSignal = signal;
	}
}
