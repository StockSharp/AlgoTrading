using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Universal Investor strategy ported from MetaTrader that combines EMA and LWMA trend confirmation.
/// Opens a long position when the LWMA stays above the EMA while both move upward, and opens a short position when the indicators align downward.
/// Positions are closed on the opposite crossover and the trade volume mimics the original risk and decrease factor rules.
/// </summary>
public class UniversalInvestor3920Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _movingPeriod;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _decreaseFactor;

	private decimal? _prevEma;
	private decimal? _prevPrevEma;
	private decimal? _prevLwma;
	private decimal? _prevPrevLwma;
	private decimal _entryPrice;
	private bool _isLong;
	private int _lossCount;

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Indicator smoothing period.
	/// </summary>
	public int MovingPeriod
	{
		get => _movingPeriod.Value;
		set => _movingPeriod.Value = value;
	}

	/// <summary>
	/// Maximum risk percent expressed as a fraction (0.05 equals 5%).
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Factor that reduces the lot size after consecutive losing trades.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="UniversalInvestor3920Strategy"/> class.
	/// </summary>
	public UniversalInvestor3920Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_movingPeriod = Param(nameof(MovingPeriod), 23)
			.SetGreaterThanZero()
			.SetDisplay("Moving Period", "EMA and LWMA length", "Indicators")
			.SetCanOptimize(true);

		_maximumRisk = Param(nameof(MaximumRisk), 0.05m)
			.SetDisplay("Maximum Risk", "Risk fraction used for volume calculation", "Risk")
			.SetCanOptimize(true);

		_decreaseFactor = Param(nameof(DecreaseFactor), 0m)
			.SetDisplay("Decrease Factor", "Volume reduction factor after losses", "Risk")
			.SetCanOptimize(true);
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

		_prevEma = null;
		_prevPrevEma = null;
		_prevLwma = null;
		_prevPrevLwma = null;
		_entryPrice = 0m;
		_isLong = false;
		_lossCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new ExponentialMovingAverage { Length = MovingPeriod };
		var lwma = new LinearWeightedMovingAverage { Length = MovingPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, lwma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, lwma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal lwmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevEma = _prevEma;
		var prevPrevEma = _prevPrevEma;
		var prevLwma = _prevLwma;
		var prevPrevLwma = _prevPrevLwma;

		_prevPrevEma = _prevEma;
		_prevEma = emaValue;
		_prevPrevLwma = _prevLwma;
		_prevLwma = lwmaValue;

		if (prevEma is null || prevPrevEma is null || prevLwma is null || prevPrevLwma is null)
			return;

		var sellSignal = prevLwma < prevEma && prevLwma < prevPrevLwma && prevEma < prevPrevEma;
		var buySignal = prevLwma > prevEma && prevLwma > prevPrevLwma && prevEma > prevPrevEma;
		var closeLongSignal = prevLwma < prevEma;
		var closeShortSignal = prevLwma > prevEma;

		if (Position > 0)
		{
			if (closeLongSignal)
			{
				ClosePosition();
				UpdateLossCount(candle.ClosePrice);
			}
		}
		else if (Position < 0)
		{
			if (closeShortSignal)
			{
				ClosePosition();
				UpdateLossCount(candle.ClosePrice);
			}
		}
		else
		{
			if (buySignal && !sellSignal)
			{
				var volume = CalculateTradeVolume(candle.ClosePrice);
				if (volume > 0m)
				{
					BuyMarket(volume);
					_entryPrice = candle.ClosePrice;
					_isLong = true;
				}
			}
			else if (sellSignal && !buySignal)
			{
				var volume = CalculateTradeVolume(candle.ClosePrice);
				if (volume > 0m)
				{
					SellMarket(volume);
					_entryPrice = candle.ClosePrice;
					_isLong = false;
				}
			}
		}
	}

	private decimal CalculateTradeVolume(decimal price)
	{
		var baseVolume = Volume > 0m ? Volume : 1m;
		var equity = Portfolio?.CurrentValue ?? 0m;
		var volume = baseVolume;

		if (MaximumRisk > 0m && equity > 0m)
		{
			var riskVolume = equity * MaximumRisk / 1000m;
			if (riskVolume > volume)
				volume = riskVolume;
		}

		if (DecreaseFactor > 0m && _lossCount > 1)
		{
			var reduction = volume * _lossCount / DecreaseFactor;
			volume -= reduction;
			if (volume <= 0m)
				volume = baseVolume;
		}

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security != null)
		{
			var step = security.VolumeStep ?? 1m;
			if (step <= 0m)
				step = 1m;

			if (volume < step)
				volume = step;

			var steps = Math.Floor(volume / step);
			if (steps < 1m)
				steps = 1m;

			volume = steps * step;
		}

		if (volume <= 0m)
			volume = 1m;

		return volume;
	}

	private void UpdateLossCount(decimal exitPrice)
	{
		if (_entryPrice <= 0m)
		{
			_lossCount = 0;
			return;
		}

		var profit = _isLong ? exitPrice - _entryPrice : _entryPrice - exitPrice;

		if (profit < 0m)
		{
			_lossCount++;
		}
		else if (profit > 0m)
		{
			_lossCount = 0;
		}

		_entryPrice = 0m;
	}
}

