namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Moving Average Crossover confirmed by SuperTrend with Bollinger Bands exits.
/// Buys on MA cross up + SuperTrend uptrend.
/// Sells on MA cross down + SuperTrend downtrend.
/// Exits at BB bands.
/// </summary>
public class MaBbSupertrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendFactor;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _fastMa;
	private SimpleMovingAverage _slowMa;
	private BollingerBands _bb;
	private SuperTrend _supertrend;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}

	public decimal SupertrendFactor
	{
		get => _supertrendFactor.Value;
		set => _supertrendFactor.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public MaBbSupertrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fastMaLength = Param(nameof(FastMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Fast SMA period", "MA");

		_slowMaLength = Param(nameof(SlowMaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Slow SMA period", "MA");

		_bbLength = Param(nameof(BbLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Bollinger");

		_supertrendPeriod = Param(nameof(SupertrendPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("SuperTrend Period", "ATR period for SuperTrend", "SuperTrend");

		_supertrendFactor = Param(nameof(SupertrendFactor), 4m)
			.SetDisplay("SuperTrend Factor", "ATR multiplier for SuperTrend", "SuperTrend");

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

		_fastMa = null;
		_slowMa = null;
		_bb = null;
		_supertrend = null;
		_prevFast = 0;
		_prevSlow = 0;
		_hasPrev = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastMa = new SimpleMovingAverage { Length = FastMaLength };
		_slowMa = new SimpleMovingAverage { Length = SlowMaLength };
		_bb = new BollingerBands { Length = BbLength, Width = 2m };
		_supertrend = new SuperTrend { Length = SupertrendPeriod, Multiplier = SupertrendFactor };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_fastMa, _slowMa, _bb, _supertrend, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _bb);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue fastVal, IIndicatorValue slowVal, IIndicatorValue bbVal, IIndicatorValue stVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_bb.IsFormed || !_supertrend.IsFormed)
			return;

		if (fastVal.IsEmpty || slowVal.IsEmpty || bbVal.IsEmpty || stVal.IsEmpty)
			return;

		var fast = fastVal.ToDecimal();
		var slow = slowVal.ToDecimal();

		var bb = (BollingerBandsValue)bbVal;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		var stTyped = (SuperTrendIndicatorValue)stVal;
		var uptrend = stTyped.IsUpTrend;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		// Entry long: MA cross up + SuperTrend uptrend
		if (crossUp && uptrend && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Entry short: MA cross down + SuperTrend downtrend
		else if (crossDown && !uptrend && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: price reaches upper BB or SuperTrend flips
		else if (Position > 0 && (candle.ClosePrice >= upper || !uptrend))
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: price reaches lower BB or SuperTrend flips
		else if (Position < 0 && (candle.ClosePrice <= lower || uptrend))
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
