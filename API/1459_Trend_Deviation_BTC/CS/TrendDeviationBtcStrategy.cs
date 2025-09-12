using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend deviation strategy combining DMI crosses with Bollinger Bands and multiple confirmations.
/// </summary>
public class TrendDeviationBtcStrategy : Strategy
{
	private readonly StrategyParam<int> _dmiPeriod;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _aroonLength;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _superTrendFactor;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevDiPlus;
	private decimal _prevDiMinus;
	private decimal _prevMomentum;
	private bool _prevTrendUp;
	private decimal _prevAroonUp;
	private decimal _prevAroonDown;

	/// <summary>
	/// DMI period.
	/// </summary>
	public int DmiPeriod
	{
		get => _dmiPeriod.Value;
		set => _dmiPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands width multiplier.
	/// </summary>
	public decimal BbMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// Momentum length.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Aroon length.
	/// </summary>
	public int AroonLength
	{
		get => _aroonLength.Value;
		set => _aroonLength.Value = value;
	}

	/// <summary>
	/// Fast MACD period.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow MACD period.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// ATR period for SuperTrend.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// SuperTrend factor.
	/// </summary>
	public decimal SuperTrendFactor
	{
		get => _superTrendFactor.Value;
		set => _superTrendFactor.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TrendDeviationBtcStrategy()
	{
		_dmiPeriod = Param(nameof(DmiPeriod), 15)
			.SetDisplay("DI Length", "Directional Index length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_bbLength = Param(nameof(BbLength), 13)
			.SetDisplay("BB Length", "Bollinger period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_bbMultiplier = Param(nameof(BbMultiplier), 2.3m)
			.SetDisplay("BB Multiplier", "Bollinger width", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.1m);

		_momentumLength = Param(nameof(MomentumLength), 10)
			.SetDisplay("Momentum Length", "Momentum period", "Parameters");

		_aroonLength = Param(nameof(AroonLength), 5)
			.SetDisplay("Aroon Length", "Aroon period", "Parameters");

		_macdFast = Param(nameof(MacdFast), 15)
			.SetDisplay("MACD Fast", "Fast EMA length", "Parameters");

		_macdSlow = Param(nameof(MacdSlow), 200)
			.SetDisplay("MACD Slow", "Slow EMA length", "Parameters");

		_macdSignal = Param(nameof(MacdSignal), 25)
			.SetDisplay("MACD Signal", "Signal length", "Parameters");

		_atrPeriod = Param(nameof(AtrPeriod), 200)
			.SetDisplay("ATR Period", "ATR period for SuperTrend", "Parameters");

		_superTrendFactor = Param(nameof(SuperTrendFactor), 2m)
			.SetDisplay("SuperTrend Factor", "ATR multiplier", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevDiPlus = 0m;
		_prevDiMinus = 0m;
		_prevMomentum = 0m;
		_prevTrendUp = false;
		_prevAroonUp = 0m;
		_prevAroonDown = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var adx = new AverageDirectionalIndex { Length = DmiPeriod };
		var bollinger = new BollingerBands { Length = BbLength, Width = BbMultiplier };
		var momentum = new Momentum { Length = MomentumLength };
		var aroon = new Aroon { Length = AroonLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = MacdFast }, LongMa = { Length = MacdSlow } },
			SignalMa = { Length = MacdSignal }
		};
		var supertrend = new SuperTrend { Length = AtrPeriod, Multiplier = SuperTrendFactor };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(bollinger, adx, momentum, aroon, macd, supertrend, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, macd);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle,
		IIndicatorValue bollingerValue,
		IIndicatorValue adxValue,
		IIndicatorValue momentumValue,
		IIndicatorValue aroonValue,
		IIndicatorValue macdValue,
		IIndicatorValue superTrendValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
		return;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.Dx.Plus is not decimal diPlus || adxTyped.Dx.Minus is not decimal diMinus)
		return;

		var momentum = momentumValue.ToDecimal();
		var aroonTyped = (AroonValue)aroonValue;
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var st = (SuperTrendIndicatorValue)superTrendValue;

		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
		return;

		var source = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var bblong = source < upper;
		var bbshort = source > lower;

		var dmiCrossUp = diPlus > diMinus && _prevDiPlus <= _prevDiMinus;
		var dmiCrossDown = diMinus > diPlus && _prevDiMinus <= _prevDiPlus;

		var momLong = momentum > 0m && momentum > _prevMomentum;
		var momShort = momentum < 0m && momentum < _prevMomentum;

		var macdLong = macd > signal;
		var macdShort = macd < signal;

		var trendLong = st.IsUpTrend && !_prevTrendUp;
		var trendShort = !st.IsUpTrend && _prevTrendUp;

		var aroonUp = aroonTyped.Up;
		var aroonDown = aroonTyped.Down;
		var aroonLong = aroonUp > aroonDown;
		var aroonShort = aroonDown > aroonUp;
		var aroonBull = aroonUp > aroonDown && _prevAroonUp <= _prevAroonDown;
		var aroonBear = aroonDown > aroonUp && _prevAroonDown <= _prevAroonUp;

		var longCondition = dmiCrossUp && bblong && (momLong || macdLong || trendLong || aroonLong || aroonBull);
		var shortCondition = dmiCrossDown && bbshort && (momShort || macdShort || trendShort || aroonShort || aroonBear);

		if (longCondition && Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortCondition && Position >= 0)
		{
		SellMarket(Volume + Math.Abs(Position));
		}

		_prevDiPlus = diPlus;
		_prevDiMinus = diMinus;
		_prevMomentum = momentum;
		_prevTrendUp = st.IsUpTrend;
		_prevAroonUp = aroonUp;
		_prevAroonDown = aroonDown;
	}
}
