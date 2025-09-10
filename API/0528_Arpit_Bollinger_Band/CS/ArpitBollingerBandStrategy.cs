using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Band strategy with early signal based on two-bar ago breakout.
/// </summary>
public class ArpitBollingerBandStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<decimal> _stopRangePercent;

	private decimal? _twoCandlesAgoUpperCrossLow;
	private decimal? _twoCandlesAgoLowerCrossHigh;

	private decimal? _prevClose1;
	private decimal? _prevClose2;
	private decimal? _prevUpper1;
	private decimal? _prevUpper2;
	private decimal? _prevLower1;
	private decimal? _prevLower2;
	private decimal? _prevHigh1;
	private decimal? _prevHigh2;
	private decimal? _prevLow1;
	private decimal? _prevLow2;

	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	public decimal BollingerMultiplier
	{
		get => _bollingerMultiplier.Value;
		set => _bollingerMultiplier.Value = value;
	}

	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	public decimal StopRangePercent
	{
		get => _stopRangePercent.Value;
		set => _stopRangePercent.Value = value;
	}

	public ArpitBollingerBandStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Bollinger EMA length", "Bollinger");

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "StdDev multiplier", "Bollinger");

		_riskReward = Param(nameof(RiskReward), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Risk reward ratio", "Risk");

		_stopRangePercent = Param(nameof(StopRangePercent), 0.05m)
			.SetRange(0.001m, 1m)
			.SetDisplay("Stop Range %", "Percent of candle range added to stop", "Risk");
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

		_twoCandlesAgoUpperCrossLow = null;
		_twoCandlesAgoLowerCrossHigh = null;
		_prevClose1 = _prevClose2 = null;
		_prevUpper1 = _prevUpper2 = null;
		_prevLower1 = _prevLower2 = null;
		_prevHigh1 = _prevHigh2 = null;
		_prevLow1 = _prevLow2 = null;
		_longStop = _longTarget = null;
		_shortStop = _shortTarget = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerMultiplier,
			MovingAverage = new ExponentialMovingAverage { Length = BollingerLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevClose2.HasValue && _prevUpper2.HasValue && _prevLow2.HasValue && _prevClose2 > _prevUpper2)
			_twoCandlesAgoUpperCrossLow = _prevLow2;

		if (_prevClose2.HasValue && _prevLower2.HasValue && _prevHigh2.HasValue && _prevClose2 < _prevLower2)
			_twoCandlesAgoLowerCrossHigh = _prevHigh2;

		if (Position > 0)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop)
			{
				RegisterOrder(CreateOrder(Sides.Sell, _longStop.Value, Volume));
				_longStop = _longTarget = null;
			}
			else if (_longTarget.HasValue && candle.HighPrice >= _longTarget)
			{
				RegisterOrder(CreateOrder(Sides.Sell, _longTarget.Value, Volume));
				_longStop = _longTarget = null;
			}
		}
		else if (Position < 0)
		{
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop)
			{
				RegisterOrder(CreateOrder(Sides.Buy, _shortStop.Value, Volume));
				_shortStop = _shortTarget = null;
			}
			else if (_shortTarget.HasValue && candle.LowPrice <= _shortTarget)
			{
				RegisterOrder(CreateOrder(Sides.Buy, _shortTarget.Value, Volume));
				_shortStop = _shortTarget = null;
			}
		}

		var longCondition = _twoCandlesAgoLowerCrossHigh.HasValue && candle.HighPrice > _twoCandlesAgoLowerCrossHigh;
		var shortCondition = _twoCandlesAgoUpperCrossLow.HasValue && candle.LowPrice < _twoCandlesAgoUpperCrossLow;

		if (longCondition && Position <= 0)
		{
			RegisterOrder(CreateOrder(Sides.Buy, candle.ClosePrice, Volume));
			var stopLoss = candle.LowPrice - (candle.HighPrice - candle.LowPrice) * StopRangePercent;
			_longStop = stopLoss;
			_longTarget = candle.ClosePrice + (candle.ClosePrice - stopLoss) * RiskReward;
		}
		else if (shortCondition && Position >= 0)
		{
			RegisterOrder(CreateOrder(Sides.Sell, candle.ClosePrice, Volume));
			var stopLoss = candle.HighPrice + (candle.HighPrice - candle.LowPrice) * StopRangePercent;
			_shortStop = stopLoss;
			_shortTarget = candle.ClosePrice - (stopLoss - candle.ClosePrice) * RiskReward;
		}

		_prevClose2 = _prevClose1;
		_prevUpper2 = _prevUpper1;
		_prevLower2 = _prevLower1;
		_prevHigh2 = _prevHigh1;
		_prevLow2 = _prevLow1;

		_prevClose1 = candle.ClosePrice;
		_prevUpper1 = upper;
		_prevLower1 = lower;
		_prevHigh1 = candle.HighPrice;
		_prevLow1 = candle.LowPrice;
	}
}
