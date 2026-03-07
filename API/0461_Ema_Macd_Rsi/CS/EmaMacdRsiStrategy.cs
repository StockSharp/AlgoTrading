namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy combining EMA trend, MACD crossovers, and RSI levels.
/// Buys when fast EMA > slow EMA + MACD bullish cross + RSI in buy zone.
/// Sells when fast EMA < slow EMA + MACD bearish cross + RSI in sell zone.
/// </summary>
public class EmaMacdRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private MovingAverageConvergenceDivergenceSignal _macdSignal;
	private RelativeStrengthIndex _rsi;

	private decimal _prevMacd;
	private decimal _prevSignal;
	private bool _isFirst = true;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public decimal RsiBuyLevel
	{
		get => _rsiBuyLevel.Value;
		set => _rsiBuyLevel.Value = value;
	}

	public decimal RsiSellLevel
	{
		get => _rsiSellLevel.Value;
		set => _rsiSellLevel.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public EmaMacdRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_fastEmaLength = Param(nameof(FastEmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 40m)
			.SetDisplay("RSI Buy Level", "Min RSI for buy", "Trading");

		_rsiSellLevel = Param(nameof(RsiSellLevel), 60m)
			.SetDisplay("RSI Sell Level", "Max RSI for sell", "Trading");

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

		_fastEma = null;
		_slowEma = null;
		_macdSignal = null;
		_rsi = null;
		_prevMacd = 0;
		_prevSignal = 0;
		_isFirst = true;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		_macdSignal = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = 12 }, LongMa = { Length = 26 } },
			SignalMa = { Length = 9 }
		};
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_fastEma, _slowEma, _macdSignal, _rsi, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue fastEmaVal, IIndicatorValue slowEmaVal, IIndicatorValue macdVal, IIndicatorValue rsiVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastEma.IsFormed || !_slowEma.IsFormed || !_macdSignal.IsFormed || !_rsi.IsFormed)
			return;

		if (fastEmaVal.IsEmpty || slowEmaVal.IsEmpty || macdVal.IsEmpty || rsiVal.IsEmpty)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdVal;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		var fastEma = fastEmaVal.ToDecimal();
		var slowEma = slowEmaVal.ToDecimal();
		var rsi = rsiVal.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevMacd = macd;
			_prevSignal = signal;
			_isFirst = false;
			return;
		}

		if (_isFirst)
		{
			_prevMacd = macd;
			_prevSignal = signal;
			_isFirst = false;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevMacd = macd;
			_prevSignal = signal;
			return;
		}

		var isBullish = fastEma > slowEma;
		var isBearish = fastEma < slowEma;
		var macdBullCross = _prevMacd <= _prevSignal && macd > signal;
		var macdBearCross = _prevMacd >= _prevSignal && macd < signal;
		var rsiBullish = rsi > RsiBuyLevel && rsi < 70m;
		var rsiBearish = rsi < RsiSellLevel && rsi > 30m;

		if (isBullish && macdBullCross && rsiBullish && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (isBearish && macdBearCross && rsiBearish && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}

		_prevMacd = macd;
		_prevSignal = signal;
	}
}
