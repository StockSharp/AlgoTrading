using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Range breakout strategy using ATR-based thresholds and trailing stop.
/// </summary>
public class IuRangeTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrTargetFactor;
	private readonly StrategyParam<decimal> _atrRangeFactor;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private AverageTrueRange _atr;

	private bool _previousRangeCond;
	private decimal _rangeHigh;
	private decimal _rangeLow;

	private decimal? _sl0;
	private decimal? _sl1;
	private decimal? _trailingSl;
	private int _prevPosition;
	private decimal _entryPrice;

	/// <summary>
	/// Lookback period for range detection.
	/// </summary>
	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Multiplier for trailing stop step.
	/// </summary>
	public decimal AtrTargetFactor
	{
		get => _atrTargetFactor.Value;
		set => _atrTargetFactor.Value = value;
	}

	/// <summary>
	/// ATR multiplier to validate range.
	/// </summary>
	public decimal AtrRangeFactor
	{
		get => _atrRangeFactor.Value;
		set => _atrRangeFactor.Value = value;
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
	/// Initializes a new instance of <see cref="IuRangeTradingStrategy"/>.
	/// </summary>
	public IuRangeTradingStrategy()
	{
		_rangeLength = Param(nameof(RangeLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Range Length", "Lookback period for range detection", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_atrTargetFactor = Param(nameof(AtrTargetFactor), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Target Factor", "Multiplier for trailing stop step", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 3.0m, 0.5m);

		_atrRangeFactor = Param(nameof(AtrRangeFactor), 1.75m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Range Factor", "ATR multiplier to validate range", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1.0m, 3.0m, 0.25m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_previousRangeCond = false;
		_rangeHigh = 0;
		_rangeLow = 0;
		_sl0 = null;
		_sl1 = null;
		_trailingSl = null;
		_prevPosition = 0;
		_entryPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = RangeLength };
		_lowest = new Lowest { Length = RangeLength };
		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_highest, _lowest, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_atr.IsFormed || !_highest.IsFormed || !_lowest.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rangeCond = (highestValue - lowestValue) <= atrValue * AtrRangeFactor;

		if (rangeCond && !_previousRangeCond && Position == 0)
		{
			_rangeHigh = highestValue;
			_rangeLow = lowestValue;
		}
		else if (rangeCond && _previousRangeCond && Position == 0)
		{
			_rangeHigh = Math.Max(_rangeHigh, highestValue);
			_rangeLow = Math.Min(_rangeLow, lowestValue);
		}

		if (Position == 0 && _rangeHigh != 0 && _rangeLow != 0)
		{
			if (candle.HighPrice >= _rangeHigh)
				BuyMarket(Volume + Math.Abs(Position));
			else if (candle.LowPrice <= _rangeLow)
				SellMarket(Volume + Math.Abs(Position));
		}

		var currentPosition = Position;

		if (currentPosition != 0 && _prevPosition == 0)
		{
			_entryPrice = candle.ClosePrice;
			var atrStop = atrValue * AtrTargetFactor;

			if (currentPosition > 0)
			{
				_sl0 = _entryPrice - atrStop;
				_sl1 = _entryPrice;
				_trailingSl = _entryPrice + atrStop;
			}
			else
			{
				_sl0 = _entryPrice + atrStop;
				_sl1 = _entryPrice;
				_trailingSl = _entryPrice - atrStop;
			}
		}

		if (currentPosition > 0 && _sl0.HasValue && _sl1.HasValue && _trailingSl.HasValue)
		{
			if (candle.HighPrice > _trailingSl.Value)
			{
				var step = atrValue * AtrTargetFactor;
				_sl0 = _sl1;
				_sl1 = _trailingSl;
				_trailingSl += step;
			}

			if (candle.LowPrice <= _sl0.Value)
			{
				SellMarket(Math.Abs(currentPosition));
				_sl0 = _sl1 = _trailingSl = null;
			}
		}
		else if (currentPosition < 0 && _sl0.HasValue && _sl1.HasValue && _trailingSl.HasValue)
		{
			if (candle.LowPrice < _trailingSl.Value)
			{
				var step = atrValue * AtrTargetFactor;
				_sl0 = _sl1;
				_sl1 = _trailingSl;
				_trailingSl -= step;
			}

			if (candle.HighPrice >= _sl0.Value)
			{
				BuyMarket(Math.Abs(currentPosition));
				_sl0 = _sl1 = _trailingSl = null;
			}
		}

		if (currentPosition == 0 && _prevPosition != 0)
		{
			_sl0 = _sl1 = _trailingSl = null;
		}

		_previousRangeCond = rangeCond;
		_prevPosition = currentPosition;
	}
}
