using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// TrendGuard Flag Finder strategy.
/// Uses SuperTrend to confirm trend and searches for bull/bear flag patterns.
/// </summary>
public class TrendGuardFlagFinderStrategy : Strategy
{
	private readonly StrategyParam<TradeDirection> _direction;
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendFactor;
	private readonly StrategyParam<decimal> _maxDepth;
	private readonly StrategyParam<int> _minFlagLength;
	private readonly StrategyParam<int> _maxFlagLength;
	private readonly StrategyParam<decimal> _maxRally;
	private readonly StrategyParam<int> _minFlagLengthBear;
	private readonly StrategyParam<int> _maxFlagLengthBear;
	private readonly StrategyParam<decimal> _poleMin;
	private readonly StrategyParam<int> _poleLength;
	private readonly StrategyParam<decimal> _poleMinBear;
	private readonly StrategyParam<int> _poleLengthBear;
	private readonly StrategyParam<DataType> _candleType;

	private SuperTrend _superTrend = null!;
	private Lowest _poleLow = null!;
	private Highest _poleHigh = null!;

	private decimal _baseHigh;
	private decimal _baseLow;
	private int _flagLength;
	private bool _flagActive;

	private decimal _baseLowBear;
	private decimal _baseHighBear;
	private int _flagLengthBear;
	private bool _flagActiveBear;

	/// <summary>
	/// Trading direction.
	/// </summary>
	public TradeDirection TradingDirection
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TrendGuardFlagFinderStrategy()
	{
		_direction = Param(nameof(TradingDirection), TradeDirection.Both)
		.SetDisplay("Direction", "Trading direction", "General");

		_supertrendPeriod = Param(nameof(_supertrendPeriod), 10)
		.SetDisplay("SuperTrend Length", "ATR period", "SuperTrend")
		.SetCanOptimize(true);

		_supertrendFactor = Param(nameof(_supertrendFactor), 4m)
		.SetDisplay("SuperTrend Factor", "ATR multiplier", "SuperTrend")
		.SetCanOptimize(true);

		_maxDepth = Param(nameof(_maxDepth), 5m)
		.SetDisplay("Max Flag Depth", "Max pullback percent", "Bull Flag")
		.SetCanOptimize(true);

		_minFlagLength = Param(nameof(_minFlagLength), 3)
		.SetDisplay("Min Flag Length", "Min bars for flag", "Bull Flag")
		.SetCanOptimize(true);

		_maxFlagLength = Param(nameof(_maxFlagLength), 7)
		.SetDisplay("Max Flag Length", "Max bars for flag", "Bull Flag")
		.SetCanOptimize(true);

		_maxRally = Param(nameof(_maxRally), 5m)
		.SetDisplay("Max Flag Rally", "Max rally percent", "Bear Flag")
		.SetCanOptimize(true);

		_minFlagLengthBear = Param(nameof(_minFlagLengthBear), 3)
		.SetDisplay("Min Bear Flag Length", "Min bars", "Bear Flag")
		.SetCanOptimize(true);

		_maxFlagLengthBear = Param(nameof(_maxFlagLengthBear), 7)
		.SetDisplay("Max Bear Flag Length", "Max bars", "Bear Flag")
		.SetCanOptimize(true);

		_poleMin = Param(nameof(_poleMin), 3m)
		.SetDisplay("Prior Uptrend Minimum", "Min percent run-up", "Bull Flag")
		.SetCanOptimize(true);

		_poleLength = Param(nameof(_poleLength), 7)
		.SetDisplay("Flag Pole Length", "Bars for run-up", "Bull Flag")
		.SetCanOptimize(true);

		_poleMinBear = Param(nameof(_poleMinBear), 3m)
		.SetDisplay("Prior Downtrend Minimum", "Min percent drop", "Bear Flag")
		.SetCanOptimize(true);

		_poleLengthBear = Param(nameof(_poleLengthBear), 7)
		.SetDisplay("Flag Pole Length Bear", "Bars for drop", "Bear Flag")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_superTrend = null!;
		_poleLow = null!;
		_poleHigh = null!;
		_baseHigh = default;
		_baseLow = default;
		_flagLength = default;
		_flagActive = default;
		_baseLowBear = default;
		_baseHighBear = default;
		_flagLengthBear = default;
		_flagActiveBear = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_superTrend = new SuperTrend { Length = _supertrendPeriod.Value, Multiplier = _supertrendFactor.Value };
		_poleLow = new Lowest { Length = _poleLength.Value };
		_poleHigh = new Highest { Length = _poleLengthBear.Value };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_superTrend, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _superTrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal superTrendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// update pole indicators
		_poleLow.Process(candle.LowPrice);
		_poleHigh.Process(candle.HighPrice);

		var isUptrend = candle.ClosePrice > superTrendValue;

		// Bull flag detection
		if (candle.HighPrice > _baseHigh)
		{
			var poleLow = _poleLow.GetCurrentValue();
			var poleDepth = ((candle.HighPrice / poleLow) - 1m) * 100m;
			_flagActive = poleDepth >= _poleMin.Value;
			_baseHigh = candle.HighPrice;
			_baseLow = candle.LowPrice;
			_flagLength = 0;
		}
		else if (_flagActive)
		{
			if (candle.LowPrice < _baseLow)
				_baseLow = candle.LowPrice;

			_flagLength++;
			var depth = ((_baseHigh / _baseLow) - 1m) * 100m;

			if (depth > _maxDepth.Value || _flagLength > _maxFlagLength.Value)
			{
				_flagActive = false;
			}
			else if (_flagLength >= _minFlagLength.Value && candle.ClosePrice > _baseHigh && isUptrend &&
			(TradingDirection == TradeDirection.Both || TradingDirection == TradeDirection.Long) && IsFormedAndOnlineAndAllowTrading() && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_flagActive = false;
			}
		}

		// Bear flag detection
		if (candle.LowPrice < _baseLowBear)
		{
			var poleHigh = _poleHigh.GetCurrentValue();
			var poleDepth = ((poleHigh / candle.LowPrice) - 1m) * 100m;
			_flagActiveBear = poleDepth >= _poleMinBear.Value;
			_baseLowBear = candle.LowPrice;
			_baseHighBear = candle.HighPrice;
			_flagLengthBear = 0;
		}
		else if (_flagActiveBear)
		{
			if (candle.HighPrice > _baseHighBear)
				_baseHighBear = candle.HighPrice;

			_flagLengthBear++;
			var rally = ((_baseHighBear / _baseLowBear) - 1m) * 100m;

			if (rally > _maxRally.Value || _flagLengthBear > _maxFlagLengthBear.Value)
			{
				_flagActiveBear = false;
			}
			else if (_flagLengthBear >= _minFlagLengthBear.Value && candle.ClosePrice < _baseLowBear && !isUptrend &&
			(TradingDirection == TradeDirection.Both || TradingDirection == TradeDirection.Short) && IsFormedAndOnlineAndAllowTrading() && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_flagActiveBear = false;
			}
		}
	}
}