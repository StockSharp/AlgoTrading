namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trades the votes of seven high/low percentile windows, confirmed by the price Supertrend.
/// </summary>
public class PivotPercentileTrendStrategy : Strategy
{
	private const int _windows = 7;
	private readonly StrategyParam<int> _percentileLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly Queue<(decimal High, decimal Low)> _candles = new();

	/// <summary>
	/// Initializes the percentile and Supertrend settings.
	/// </summary>
	public PivotPercentileTrendStrategy()
	{
		_percentileLength = Param(nameof(PercentileLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("Percentile Length", "Base window; the longest contains seven times as many candles", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Wilder ATR period of the price Supertrend", "Indicators");
		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR width of the price Supertrend", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for calculation", "General");
	}

	/// <summary>
	/// Base lookback; the seven windows contain one through seven times this many candles.
	/// </summary>
	public int PercentileLength
	{
		get => _percentileLength.Value;
		set => _percentileLength.Value = value;
	}

	/// <summary>
	/// Period of the Wilder ATR used by Supertrend.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Positive multiplier of the Supertrend ATR.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type used for signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private int MaximumHistory => checked(PercentileLength * _windows);

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_candles.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_candles.Clear();
		var supertrend = new SuperTrend { Length = AtrPeriod, Multiplier = AtrMultiplier };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindWithEmpty(supertrend, (candle, line) =>
			{
				StoreCandle(candle);
				if (line is decimal value)
					ProcessCandle(candle, value);
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void StoreCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		_candles.Enqueue((candle.HighPrice, candle.LowPrice));
		if (_candles.Count > MaximumHistory)
			_candles.Dequeue();
	}

	private void ProcessCandle(ICandleMessage candle, decimal supertrend)
	{
		if (candle.State != CandleStates.Finished || _candles.Count < MaximumHistory)
			return;

		var history = _candles.ToArray();
		var strength = 0;
		for (var window = 1; window <= _windows; window++)
		{
			var period = PercentileLength * window;
			var recent = history.AsSpan(history.Length - period, period);
			var highs = new decimal[period];
			var lows = new decimal[period];
			for (var index = 0; index < period; index++)
			{
				highs[index] = recent[index].High;
				lows[index] = recent[index].Low;
			}
			Array.Sort(highs);
			Array.Sort(lows);
			var upper = highs[(int)((3L * period + 3) / 4) - 1];
			var lower = lows[(int)(((long)period + 3) / 4) - 1];
			if (candle.HighPrice > upper)
				strength++;
			if (candle.LowPrice < lower)
				strength--;
		}

		if (strength > 0 && candle.ClosePrice > supertrend && Position <= 0m)
			BuyMarket(Volume + Math.Abs(Position));
		else if (strength < 0 && candle.ClosePrice < supertrend && Position >= 0m)
			SellMarket(Volume + Math.Abs(Position));
	}
}
