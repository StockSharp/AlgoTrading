using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands Modified strategy.
/// A close above the upper Bollinger band goes long and a close below the lower band goes short, reversing an opposite position.
/// With CrossoverCheck (CrossunderCheck) the close also has to have been at or below the upper band (at or above the lower band) on
/// the previous candle, and with EmaTrend a long needs the close above EMA(EmaLength) and a short below it. Each entry freezes a
/// stop at the lowest low of the last LowestLength candles (highest high of the last HighestLength candles for shorts) and a
/// target TargetFactor times that risk away.
/// </summary>
public class BollingerBandsModifiedStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _highestLength;
	private readonly StrategyParam<int> _lowestLength;
	private readonly StrategyParam<decimal> _targetFactor;
	private readonly StrategyParam<bool> _emaTrend;
	private readonly StrategyParam<bool> _crossoverCheck;
	private readonly StrategyParam<bool> _crossunderCheck;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevUpper;
	private decimal? _prevLower;
	private decimal? _stopPrice;
	private decimal? _targetPrice;

	/// <summary>
	/// Bollinger period.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Bollinger standard deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// EMA period of the trend filter.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Candles of the highest high for the short stop.
	/// </summary>
	public int HighestLength
	{
		get => _highestLength.Value;
		set => _highestLength.Value = value;
	}

	/// <summary>
	/// Candles of the lowest low for the long stop.
	/// </summary>
	public int LowestLength
	{
		get => _lowestLength.Value;
		set => _lowestLength.Value = value;
	}

	/// <summary>
	/// Target distance as a multiple of the risk.
	/// </summary>
	public decimal TargetFactor
	{
		get => _targetFactor.Value;
		set => _targetFactor.Value = value;
	}

	/// <summary>
	/// Require the EMA trend filter.
	/// </summary>
	public bool EmaTrend
	{
		get => _emaTrend.Value;
		set => _emaTrend.Value = value;
	}

	/// <summary>
	/// Require an actual cross above the upper band for longs.
	/// </summary>
	public bool CrossoverCheck
	{
		get => _crossoverCheck.Value;
		set => _crossoverCheck.Value = value;
	}

	/// <summary>
	/// Require an actual cross below the lower band for shorts.
	/// </summary>
	public bool CrossunderCheck
	{
		get => _crossunderCheck.Value;
		set => _crossunderCheck.Value = value;
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
	public BollingerBandsModifiedStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Bollinger period", "Bollinger");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 0.38m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Bollinger standard deviation multiplier", "Bollinger");

		_emaLength = Param(nameof(EmaLength), 80)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period of the trend filter", "Trend");

		_highestLength = Param(nameof(HighestLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Highest Length", "Candles of the highest high for the short stop", "Risk");

		_lowestLength = Param(nameof(LowestLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Lowest Length", "Candles of the lowest low for the long stop", "Risk");

		_targetFactor = Param(nameof(TargetFactor), 1.6m)
			.SetGreaterThanZero()
			.SetDisplay("Target Factor", "Target distance as a multiple of the risk", "Risk");

		_emaTrend = Param(nameof(EmaTrend), true)
			.SetDisplay("EMA Trend", "Require the EMA trend filter", "Trend");

		_crossoverCheck = Param(nameof(CrossoverCheck), false)
			.SetDisplay("Crossover Check", "Require an actual cross above the upper band for longs", "Signals");

		_crossunderCheck = Param(nameof(CrossunderCheck), false)
			.SetDisplay("Crossunder Check", "Require an actual cross below the lower band for shorts", "Signals");

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
		_prevClose = null;
		_prevUpper = null;
		_prevLower = null;
		_stopPrice = null;
		_targetPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var bollinger = new BollingerBands { Length = BollingerLength, Width = BollingerDeviation };
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var highest = new Highest { Length = HighestLength };
		var lowest = new Lowest { Length = LowestLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ema, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue emaValue, IIndicatorValue highestValue, IIndicatorValue lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed || !emaValue.IsFormed || !highestValue.IsFormed || !lowestValue.IsFormed)
			return;

		var bands = (BollingerBandsValue)bollingerValue;

		if (bands.UpBand is not decimal upper || bands.LowBand is not decimal lower)
			return;

		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		var prevUpper = _prevUpper;
		var prevLower = _prevLower;
		_prevClose = close;
		_prevUpper = upper;
		_prevLower = lower;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Stop and target frozen at entry.
		if (Position > 0 && _stopPrice is decimal longStop && _targetPrice is decimal longTarget && (candle.LowPrice <= longStop || candle.HighPrice >= longTarget))
		{
			SellMarket(Position);
			_stopPrice = null;
			_targetPrice = null;
			return;
		}

		if (Position < 0 && _stopPrice is decimal shortStop && _targetPrice is decimal shortTarget && (candle.HighPrice >= shortStop || candle.LowPrice <= shortTarget))
		{
			BuyMarket(-Position);
			_stopPrice = null;
			_targetPrice = null;
			return;
		}

		var ema = emaValue.GetValue<decimal>();
		var longSignal = close > upper && (!CrossoverCheck || (prevClose is decimal pc && prevUpper is decimal pu && pc <= pu)) && (!EmaTrend || close > ema);
		var shortSignal = close < lower && (!CrossunderCheck || (prevClose is decimal pc2 && prevLower is decimal pl && pc2 >= pl)) && (!EmaTrend || close < ema);

		if (longSignal && Position <= 0)
		{
			var stop = lowestValue.GetValue<decimal>();
			var risk = close - stop;

			if (risk <= 0)
				return;

			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = stop;
			_targetPrice = close + risk * TargetFactor;
		}
		else if (shortSignal && Position >= 0)
		{
			var stop = highestValue.GetValue<decimal>();
			var risk = stop - close;

			if (risk <= 0)
				return;

			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = stop;
			_targetPrice = close - risk * TargetFactor;
		}
	}
}
