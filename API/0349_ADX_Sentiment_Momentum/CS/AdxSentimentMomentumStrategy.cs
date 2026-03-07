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
/// ADX trend strategy filtered by deterministic sentiment momentum.
/// </summary>
public class AdxSentimentMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _sentimentPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private ADX _adx = null!;
	private decimal _prevSentiment;
	private decimal _currentSentiment;
	private decimal _sentimentMomentum;
	private decimal? _prevDiPlus;
	private decimal? _prevDiMinus;
	private int _cooldownRemaining;

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold for strong trend.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Period for sentiment momentum calculation.
	/// </summary>
	public int SentimentPeriod
	{
		get => _sentimentPeriod.Value;
		set => _sentimentPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Closed candles to wait before another position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy.
	/// </summary>
	public AdxSentimentMomentumStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetRange(5, 30)
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetRange(15m, 35m)
			.SetDisplay("ADX Threshold", "Threshold for strong trend identification", "Indicators");

		_sentimentPeriod = Param(nameof(SentimentPeriod), 5)
			.SetRange(3, 10)
			.SetDisplay("Sentiment Period", "Period for sentiment momentum calculation", "Sentiment");

		_stopLoss = Param(nameof(StopLoss), 2m)
			.SetRange(1m, 5m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 24)
			.SetNotNegative()
			.SetDisplay("Cooldown Bars", "Closed candles to wait before another position change", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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

		_adx?.Reset();
		_adx = null!;
		_prevSentiment = 0m;
		_currentSentiment = 0m;
		_sentimentMomentum = 0m;
		_prevDiPlus = null;
		_prevDiMinus = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_adx = new ADX
		{
			Length = AdxPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adx);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(2, UnitTypes.Percent),
			new Unit(StopLoss, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateSentiment(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (adxValue is not AverageDirectionalIndexValue typedAdx ||
			typedAdx.MovingAverage is not decimal adxMain ||
			typedAdx.Dx.Plus is not decimal diPlus ||
			typedAdx.Dx.Minus is not decimal diMinus)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var bullishCross = _prevDiPlus is decimal previousPlus && _prevDiMinus is decimal previousMinus &&
			previousPlus <= previousMinus && diPlus > diMinus;
		var bearishCross = _prevDiPlus is decimal previousPlus2 && _prevDiMinus is decimal previousMinus2 &&
			previousPlus2 >= previousMinus2 && diMinus > diPlus;
		var strongTrend = adxMain >= AdxThreshold;

		if (_cooldownRemaining == 0 && strongTrend && bullishCross && _sentimentMomentum > 0 && Position <= 0)
		{
			BuyMarket(Volume + (Position < 0 ? Math.Abs(Position) : 0m));
			_cooldownRemaining = CooldownBars;
		}
		else if (_cooldownRemaining == 0 && strongTrend && bearishCross && _sentimentMomentum < 0 && Position >= 0)
		{
			SellMarket(Volume + (Position > 0 ? Math.Abs(Position) : 0m));
			_cooldownRemaining = CooldownBars;
		}
		else if (Position > 0 && (adxMain < 20m || _sentimentMomentum < 0))
		{
			SellMarket(Position);
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && (adxMain < 20m || _sentimentMomentum > 0))
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}

		_prevDiPlus = diPlus;
		_prevDiMinus = diMinus;
	}

	private void UpdateSentiment(ICandleMessage candle)
	{
		_prevSentiment = _currentSentiment;
		_currentSentiment = SimulateSentiment(candle);
		_sentimentMomentum = _currentSentiment - _prevSentiment;
	}

	private decimal SimulateSentiment(ICandleMessage candle)
	{
		var range = Math.Max(candle.HighPrice - candle.LowPrice, 1m);
		var body = candle.ClosePrice - candle.OpenPrice;
		var bodyRatio = body / range;
		var rangeRatio = range / Math.Max(candle.OpenPrice, 1m);
		var trendFactor = Math.Min(0.3m, rangeRatio * SentimentPeriod);

		return Math.Max(-1m, Math.Min(1m, bodyRatio + (Math.Sign(body) * trendFactor)));
	}
}
