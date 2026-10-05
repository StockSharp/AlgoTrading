namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Multi-band comparison strategy.
/// The trigger line is the UpperQuantile quantile of the last Length closes minus BollingerMultiplier standard deviations.
/// A long opens after EntryConfirmBars consecutive closes above the line and closes after ExitConfirmBars consecutive closes below it.
/// The SMA middle band is drawn for comparison. Long only, no stops.
/// </summary>
public class MultiBandComparisonStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<decimal> _upperQuantile;
	private readonly StrategyParam<int> _entryConfirmBars;
	private readonly StrategyParam<int> _exitConfirmBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _closes = new();
	private int _aboveCount;
	private int _belowCount;

	/// <summary>
	/// Period of the SMA, standard deviation and quantile window.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier.
	/// </summary>
	public decimal BollingerMultiplier
	{
		get => _bollingerMultiplier.Value;
		set => _bollingerMultiplier.Value = value;
	}

	/// <summary>
	/// Quantile of the closes that forms the upper band.
	/// </summary>
	public decimal UpperQuantile
	{
		get => _upperQuantile.Value;
		set => _upperQuantile.Value = value;
	}

	/// <summary>
	/// Consecutive closes above the line required to enter.
	/// </summary>
	public int EntryConfirmBars
	{
		get => _entryConfirmBars.Value;
		set => _entryConfirmBars.Value = value;
	}

	/// <summary>
	/// Consecutive closes below the line required to exit.
	/// </summary>
	public int ExitConfirmBars
	{
		get => _exitConfirmBars.Value;
		set => _exitConfirmBars.Value = value;
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
	/// Constructor.
	/// </summary>
	public MultiBandComparisonStrategy()
	{
		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Period of the SMA, standard deviation and quantile window", "Bands");

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 1m)
			.SetNotNegative()
			.SetDisplay("BB Mult", "Standard deviation multiplier", "Bands");

		_upperQuantile = Param(nameof(UpperQuantile), 0.95m)
			.SetRange(0m, 1m)
			.SetDisplay("Upper Quantile", "Quantile of the closes that forms the upper band", "Bands");

		_entryConfirmBars = Param(nameof(EntryConfirmBars), 1)
			.SetGreaterThanZero()
			.SetDisplay("Entry Confirm Bars", "Consecutive closes above the line required to enter", "Trading");

		_exitConfirmBars = Param(nameof(ExitConfirmBars), 1)
			.SetGreaterThanZero()
			.SetDisplay("Exit Confirm Bars", "Consecutive closes below the line required to exit", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_closes.Clear();
		_aboveCount = 0;
		_belowCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var sma = new SimpleMovingAverage { Length = Length };
		var std = new StandardDeviation { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, std, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private decimal GetQuantile()
	{
		var sorted = _closes.OrderBy(c => c).ToArray();
		var position = UpperQuantile * (sorted.Length - 1);
		var lower = (int)Math.Floor(position);
		var upper = Math.Min(lower + 1, sorted.Length - 1);
		var fraction = position - lower;
		return sorted[lower] + (sorted[upper] - sorted[lower]) * fraction;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue, IIndicatorValue stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		_closes.Enqueue(close);
		if (_closes.Count > Length)
			_closes.Dequeue();

		if (!smaValue.IsFormed || !stdValue.IsFormed || _closes.Count < Length)
			return;

		var line = GetQuantile() - stdValue.ToDecimal() * BollingerMultiplier;

		if (close > line)
		{
			_aboveCount++;
			_belowCount = 0;
		}
		else if (close < line)
		{
			_belowCount++;
			_aboveCount = 0;
		}
		else
		{
			_aboveCount = 0;
			_belowCount = 0;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position <= 0 && _aboveCount >= EntryConfirmBars)
			BuyMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && _belowCount >= ExitConfirmBars)
			SellMarket(Position);
	}
}
