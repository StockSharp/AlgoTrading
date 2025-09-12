using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-step FlexiMA strategy using variable-length SMA oscillator and SuperTrend.
/// Partial exits at three take-profit levels.
/// </summary>
public class MultiStepFlexiMaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _indicatorLength;
	private readonly StrategyParam<decimal> _startingFactor;
	private readonly StrategyParam<decimal> _incrementFactor;
	private readonly StrategyParam<NormalizeMethod> _normalizeMethod;
	private readonly StrategyParam<int> _superTrendPeriod;
	private readonly StrategyParam<decimal> _superTrendMultiplier;
	private readonly StrategyParam<TradeDirection> _direction;
	private readonly StrategyParam<decimal> _tpLevel1;
	private readonly StrategyParam<decimal> _tpLevel2;
	private readonly StrategyParam<decimal> _tpLevel3;
	private readonly StrategyParam<decimal> _tpPercent1;
	private readonly StrategyParam<decimal> _tpPercent2;
	private readonly StrategyParam<decimal> _tpPercent3;

	private SuperTrend _superTrend;
	private readonly SMA[] _smas = new SMA[20];

	private decimal _entryVolume;
	private bool _tp1Done;
	private bool _tp2Done;
	private bool _tp3Done;

	/// <summary>
	/// Normalization method for oscillator.
	/// </summary>
	public enum NormalizeMethod
	{
		None,
		MaxMin,
		AbsoluteSum
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Base length for oscillator moving averages.
	/// </summary>
	public int IndicatorLength { get => _indicatorLength.Value; set => _indicatorLength.Value = value; }

	/// <summary>
	/// Starting factor for oscillator.
	/// </summary>
	public decimal StartingFactor { get => _startingFactor.Value; set => _startingFactor.Value = value; }

	/// <summary>
	/// Increment factor for oscillator.
	/// </summary>
	public decimal IncrementFactor { get => _incrementFactor.Value; set => _incrementFactor.Value = value; }

	/// <summary>
	/// Normalization method.
	/// </summary>
	public NormalizeMethod Normalization { get => _normalizeMethod.Value; set => _normalizeMethod.Value = value; }

	/// <summary>
	/// SuperTrend ATR period.
	/// </summary>
	public int SuperTrendPeriod { get => _superTrendPeriod.Value; set => _superTrendPeriod.Value = value; }

	/// <summary>
	/// SuperTrend ATR multiplier.
	/// </summary>
	public decimal SuperTrendMultiplier { get => _superTrendMultiplier.Value; set => _superTrendMultiplier.Value = value; }

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public TradeDirection Direction { get => _direction.Value; set => _direction.Value = value; }

	/// <summary>
	/// Take profit level 1 in percent.
	/// </summary>
	public decimal TakeProfitLevel1 { get => _tpLevel1.Value; set => _tpLevel1.Value = value; }

	/// <summary>
	/// Take profit level 2 in percent.
	/// </summary>
	public decimal TakeProfitLevel2 { get => _tpLevel2.Value; set => _tpLevel2.Value = value; }

	/// <summary>
	/// Take profit level 3 in percent.
	/// </summary>
	public decimal TakeProfitLevel3 { get => _tpLevel3.Value; set => _tpLevel3.Value = value; }

	/// <summary>
	/// Percent of position to exit at level 1.
	/// </summary>
	public decimal TakeProfitPercent1 { get => _tpPercent1.Value; set => _tpPercent1.Value = value; }

	/// <summary>
	/// Percent of position to exit at level 2.
	/// </summary>
	public decimal TakeProfitPercent2 { get => _tpPercent2.Value; set => _tpPercent2.Value = value; }

	/// <summary>
	/// Percent of position to exit at level 3.
	/// </summary>
	public decimal TakeProfitPercent3 { get => _tpPercent3.Value; set => _tpPercent3.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="MultiStepFlexiMaStrategy"/>.
	/// </summary>
	public MultiStepFlexiMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");

		_indicatorLength = Param(nameof(IndicatorLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Indicator Length", "Base period for oscillator", "FlexiMA");

		_startingFactor = Param(nameof(StartingFactor), 1m)
		.SetDisplay("Start Factor", "Starting factor", "FlexiMA");

		_incrementFactor = Param(nameof(IncrementFactor), 2m)
		.SetDisplay("Increment Factor", "Increment between steps", "FlexiMA");

		_normalizeMethod = Param(nameof(Normalization), NormalizeMethod.None)
		.SetDisplay("Normalization", "Normalization method", "FlexiMA");

		_superTrendPeriod = Param(nameof(SuperTrendPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("SuperTrend Period", "ATR period", "SuperTrend");

		_superTrendMultiplier = Param(nameof(SuperTrendMultiplier), 15m)
		.SetGreaterThanZero()
		.SetDisplay("SuperTrend Multiplier", "ATR multiplier", "SuperTrend");

		_direction = Param(nameof(Direction), TradeDirection.Both)
		.SetDisplay("Trade Direction", "Allowed trading direction", "General");

		_tpLevel1 = Param(nameof(TakeProfitLevel1), 2m)
		.SetDisplay("TP Level 1 (%)", "First take profit level", "Risk");

		_tpLevel2 = Param(nameof(TakeProfitLevel2), 8m)
		.SetDisplay("TP Level 2 (%)", "Second take profit level", "Risk");

		_tpLevel3 = Param(nameof(TakeProfitLevel3), 18m)
		.SetDisplay("TP Level 3 (%)", "Third take profit level", "Risk");

		_tpPercent1 = Param(nameof(TakeProfitPercent1), 30m)
		.SetDisplay("TP Percent 1", "Percent to exit at level 1", "Risk");

		_tpPercent2 = Param(nameof(TakeProfitPercent2), 20m)
		.SetDisplay("TP Percent 2", "Percent to exit at level 2", "Risk");

		_tpPercent3 = Param(nameof(TakeProfitPercent3), 15m)
		.SetDisplay("TP Percent 3", "Percent to exit at level 3", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_superTrend = new SuperTrend { Length = SuperTrendPeriod, Multiplier = SuperTrendMultiplier };

		for (var i = 0; i < _smas.Length; i++)
		{
			var factor = StartingFactor + i * IncrementFactor;
			var len = Math.Max(1, (int)Math.Round(IndicatorLength * factor));
			_smas[i] = new SMA { Length = len };
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_superTrend, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _superTrend);
			foreach (var sma in _smas)
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal superTrendValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_superTrend.IsFormed)
		return;

		var source = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		var diffs = new decimal[_smas.Length];
		var denom = 0m;
		for (var i = 0; i < _smas.Length; i++)
		{
			var smaVal = _smas[i].Process(new DecimalIndicatorValue(_smas[i], source));
			if (!smaVal.IsFinal || smaVal is not DecimalIndicatorValue smaResult)
			return;

			var diff = source - smaResult.Value;
			diffs[i] = diff;
			denom += Math.Abs(diff);
		}

		var normalized = new decimal[diffs.Length];

		switch (Normalization)
		{
			case NormalizeMethod.MaxMin:
			var min = decimal.MaxValue;
			var max = decimal.MinValue;
			for (var i = 0; i < diffs.Length; i++)
			{
				var d = diffs[i];
				if (d < min)
				min = d;
				if (d > max)
				max = d;
			}
			var range = max - min;
			for (var i = 0; i < diffs.Length; i++)
			normalized[i] = range == 0m ? 0m : (diffs[i] - min) / range;
			break;
			case NormalizeMethod.AbsoluteSum:
			for (var i = 0; i < diffs.Length; i++)
			normalized[i] = denom == 0m ? 0m : diffs[i] / denom;
			break;
			default:
			for (var i = 0; i < diffs.Length; i++)
			normalized[i] = diffs[i];
			break;
		}

		var sorted = (decimal[])normalized.Clone();
		Array.Sort(sorted);
		var median = (sorted[9] + sorted[10]) / 2m;

		var direction = candle.ClosePrice > superTrendValue ? -1 : 1;

		var allowLong = Direction == TradeDirection.Both || Direction == TradeDirection.Long;
		var allowShort = Direction == TradeDirection.Both || Direction == TradeDirection.Short;

		if (allowLong && direction < 0 && median > 0 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entryVolume = volume;
			_tp1Done = _tp2Done = _tp3Done = false;
		}
		else if (allowShort && direction > 0 && median < 0 && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_entryVolume = volume;
			_tp1Done = _tp2Done = _tp3Done = false;
		}
		else if (Position > 0 && direction > 0 && median < 0)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && direction < 0 && median > 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		if (Position > 0)
		{
			var entry = PositionPrice;
			if (!_tp1Done && candle.ClosePrice >= entry * (1 + TakeProfitLevel1 / 100m))
			{
				SellMarket(Math.Min(Position, _entryVolume * TakeProfitPercent1 / 100m));
				_tp1Done = true;
			}

			if (!_tp2Done && candle.ClosePrice >= entry * (1 + TakeProfitLevel2 / 100m))
			{
				SellMarket(Math.Min(Position, _entryVolume * TakeProfitPercent2 / 100m));
				_tp2Done = true;
			}

			if (!_tp3Done && candle.ClosePrice >= entry * (1 + TakeProfitLevel3 / 100m))
			{
				SellMarket(Math.Min(Position, _entryVolume * TakeProfitPercent3 / 100m));
				_tp3Done = true;
			}
		}
		else if (Position < 0)
		{
			var entry = PositionPrice;
			if (!_tp1Done && candle.ClosePrice <= entry * (1 - TakeProfitLevel1 / 100m))
			{
				BuyMarket(Math.Min(Math.Abs(Position), _entryVolume * TakeProfitPercent1 / 100m));
				_tp1Done = true;
			}

			if (!_tp2Done && candle.ClosePrice <= entry * (1 - TakeProfitLevel2 / 100m))
			{
				BuyMarket(Math.Min(Math.Abs(Position), _entryVolume * TakeProfitPercent2 / 100m));
				_tp2Done = true;
			}

			if (!_tp3Done && candle.ClosePrice <= entry * (1 - TakeProfitLevel3 / 100m))
			{
				BuyMarket(Math.Min(Math.Abs(Position), _entryVolume * TakeProfitPercent3 / 100m));
				_tp3Done = true;
			}
		}
		else
		{
			_tp1Done = _tp2Done = _tp3Done = false;
		}
	}
}
