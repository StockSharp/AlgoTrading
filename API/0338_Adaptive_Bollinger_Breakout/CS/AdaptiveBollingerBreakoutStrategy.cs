using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades adaptive Bollinger mean reversion selected by ATR volatility regime.
/// </summary>
public class AdaptiveBollingerBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _minBollingerPeriod;
	private readonly StrategyParam<int> _maxBollingerPeriod;
	private readonly StrategyParam<decimal> _minBollingerDeviation;
	private readonly StrategyParam<decimal> _maxBollingerDeviation;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastSma = null!;
	private SimpleMovingAverage _slowSma = null!;
	private StandardDeviation _fastStd = null!;
	private StandardDeviation _slowStd = null!;
	private AverageTrueRange _atr = null!;
	private decimal _atrSum;
	private int _atrCount;
	private int _cooldownRemaining;

	/// <summary>
	/// Strategy parameter: Minimum Bollinger period.
	/// </summary>
	public int MinBollingerPeriod
	{
		get => _minBollingerPeriod.Value;
		set => _minBollingerPeriod.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Maximum Bollinger period.
	/// </summary>
	public int MaxBollingerPeriod
	{
		get => _maxBollingerPeriod.Value;
		set => _maxBollingerPeriod.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Minimum Bollinger deviation.
	/// </summary>
	public decimal MinBollingerDeviation
	{
		get => _minBollingerDeviation.Value;
		set => _minBollingerDeviation.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Maximum Bollinger deviation.
	/// </summary>
	public decimal MaxBollingerDeviation
	{
		get => _maxBollingerDeviation.Value;
		set => _maxBollingerDeviation.Value = value;
	}

	/// <summary>
	/// Strategy parameter: ATR period for volatility calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Closed candles to wait between signals.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Strategy parameter: Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AdaptiveBollingerBreakoutStrategy()
	{
		_minBollingerPeriod = Param(nameof(MinBollingerPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Min Bollinger Period", "Short Bollinger period for volatile regimes", "Indicator Settings");

		_maxBollingerPeriod = Param(nameof(MaxBollingerPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Max Bollinger Period", "Long Bollinger period for quiet regimes", "Indicator Settings");

		_minBollingerDeviation = Param(nameof(MinBollingerDeviation), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Min Bollinger Deviation", "Narrow band width for quiet regimes", "Indicator Settings");

		_maxBollingerDeviation = Param(nameof(MaxBollingerDeviation), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("Max Bollinger Deviation", "Wide band width for volatile regimes", "Indicator Settings");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR volatility calculation", "Indicator Settings");

		_cooldownBars = Param(nameof(CooldownBars), 6)
			.SetNotNegative()
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another breakout entry", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_fastSma?.Reset();
		_slowSma?.Reset();
		_fastStd?.Reset();
		_slowStd?.Reset();
		_atr?.Reset();

		_atrSum = 0m;
		_atrCount = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastSma = new SimpleMovingAverage
		{
			Length = MinBollingerPeriod
		};

		_slowSma = new SimpleMovingAverage
		{
			Length = MaxBollingerPeriod
		};

		_fastStd = new StandardDeviation
		{
			Length = MinBollingerPeriod
		};

		_slowStd = new StandardDeviation
		{
			Length = MaxBollingerPeriod
		};

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastSma);
			DrawIndicator(area, _slowSma);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var atrValue = _atr.Process(new CandleIndicatorValue(_atr, candle) { IsFinal = true });
		var fastSmaValue = _fastSma.Process(new DecimalIndicatorValue(_fastSma, candle.ClosePrice, candle.OpenTime) { IsFinal = true });
		var slowSmaValue = _slowSma.Process(new DecimalIndicatorValue(_slowSma, candle.ClosePrice, candle.OpenTime) { IsFinal = true });
		var fastStdValue = _fastStd.Process(new DecimalIndicatorValue(_fastStd, candle.ClosePrice, candle.OpenTime) { IsFinal = true });
		var slowStdValue = _slowStd.Process(new DecimalIndicatorValue(_slowStd, candle.ClosePrice, candle.OpenTime) { IsFinal = true });

		if (!_atr.IsFormed || !_fastSma.IsFormed || !_slowSma.IsFormed || !_fastStd.IsFormed || !_slowStd.IsFormed ||
			atrValue.IsEmpty || fastSmaValue.IsEmpty || slowSmaValue.IsEmpty || fastStdValue.IsEmpty || slowStdValue.IsEmpty)
			return;

		var currentAtr = atrValue.ToDecimal();
		_atrSum += currentAtr;
		_atrCount++;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var averageAtr = _atrCount > 0 ? _atrSum / _atrCount : currentAtr;
		var useFastBands = currentAtr >= averageAtr;
		var middleBand = useFastBands ? fastSmaValue.ToDecimal() : slowSmaValue.ToDecimal();
		var standardDeviation = useFastBands ? fastStdValue.ToDecimal() : slowStdValue.ToDecimal();
		var bandWidth = useFastBands ? MaxBollingerDeviation : MinBollingerDeviation;
		var upperBand = middleBand + (standardDeviation * bandWidth);
		var lowerBand = middleBand - (standardDeviation * bandWidth);

		var close = candle.ClosePrice;

		if (Position > 0 && close >= middleBand)
		{
			SellMarket(Position);
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && close <= middleBand)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		else if (_cooldownRemaining == 0 && close < lowerBand && Position <= 0)
		{
			BuyMarket(Volume + (Position < 0 ? Math.Abs(Position) : 0m));
			_cooldownRemaining = CooldownBars;
		}
		else if (_cooldownRemaining == 0 && close > upperBand && Position >= 0)
		{
			SellMarket(Volume + (Position > 0 ? Math.Abs(Position) : 0m));
			_cooldownRemaining = CooldownBars;
		}
	}
}
