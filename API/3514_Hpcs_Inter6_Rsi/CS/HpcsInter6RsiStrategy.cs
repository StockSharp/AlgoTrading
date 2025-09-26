using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader strategy _HPCS_Inter6_MT4_EA_V01_WE.
/// Trades RSI reversals at the 70/30 levels with symmetric fixed targets and stops.
/// </summary>
public class HpcsInter6RsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _upperLevel;
	private readonly StrategyParam<decimal> _lowerLevel;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _offsetInPips;

	private RelativeStrengthIndex _rsi;
	private decimal? _previousRsi;
	private DateTimeOffset? _lastSignalTime;
	private decimal? _targetPrice;
	private decimal? _stopPrice;
	private bool _isLongPosition;

	/// <summary>
	/// Candle type used for the RSI evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI lookback length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Upper RSI level that triggers short entries when crossed from below.
	/// </summary>
	public decimal UpperLevel
	{
		get => _upperLevel.Value;
		set => _upperLevel.Value = value;
	}

	/// <summary>
	/// Lower RSI level that triggers long entries when crossed from above.
	/// </summary>
	public decimal LowerLevel
	{
		get => _lowerLevel.Value;
		set => _lowerLevel.Value = value;
	}

	/// <summary>
	/// Market order volume used for entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Target and stop distance expressed in pips.
	/// </summary>
	public decimal OffsetInPips
	{
		get => _offsetInPips.Value;
		set => _offsetInPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HpcsInter6RsiStrategy"/> class.
	/// </summary>
	public HpcsInter6RsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for RSI evaluation", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Lookback period for RSI", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_upperLevel = Param(nameof(UpperLevel), 70m)
			.SetDisplay("Upper RSI", "Upper RSI level for shorts", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(60m, 90m, 5m);

		_lowerLevel = Param(nameof(LowerLevel), 30m)
			.SetDisplay("Lower RSI", "Lower RSI level for longs", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume for entries", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_offsetInPips = Param(nameof(OffsetInPips), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Offset (pips)", "Target and stop distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 30m, 5m);
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

		_previousRsi = null;
		_lastSignalTime = null;
		_targetPrice = null;
		_stopPrice = null;
		_isLongPosition = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiLength
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_rsi != null)
		{
			_rsi.Length = RsiLength;
		}

		UpdateActivePositionTargets(candle);

		var previousRsi = _previousRsi;
		_previousRsi = rsiValue;

		if (previousRsi is null)
			return;

		var candleTime = candle.OpenTime;

		if (_lastSignalTime.HasValue && _lastSignalTime.Value == candleTime)
			return;

		if (TryEnterShort(candle, rsiValue, previousRsi.Value))
		{
			_lastSignalTime = candleTime;
			return;
		}

		if (TryEnterLong(candle, rsiValue, previousRsi.Value))
		{
			_lastSignalTime = candleTime;
		}
	}

	private void UpdateActivePositionTargets(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (!_isLongPosition)
			{
				_targetPrice = null;
				_stopPrice = null;
				return;
			}

			var shouldExit = (_targetPrice.HasValue && candle.HighPrice >= _targetPrice.Value)
				|| (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value);

			if (shouldExit)
			{
				SellMarket(Math.Abs(Position));
				_targetPrice = null;
				_stopPrice = null;
			}
		}
		else if (Position < 0)
		{
			if (_isLongPosition)
			{
				_targetPrice = null;
				_stopPrice = null;
				return;
			}

			var shouldExit = (_targetPrice.HasValue && candle.LowPrice <= _targetPrice.Value)
				|| (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value);

			if (shouldExit)
			{
				BuyMarket(Math.Abs(Position));
				_targetPrice = null;
				_stopPrice = null;
			}
		}
		else
		{
			_targetPrice = null;
			_stopPrice = null;
			_isLongPosition = false;
		}
	}

	private bool TryEnterShort(ICandleMessage candle, decimal currentRsi, decimal previousRsi)
	{
		if (!(currentRsi > UpperLevel && previousRsi < UpperLevel))
			return false;

		var volume = TradeVolume;
		if (volume <= 0m)
			return false;

		if (Position > 0)
		{
			volume += Math.Abs(Position);
		}

		SellMarket(volume);

		var offset = CalculateOffset();
		if (offset > 0m)
		{
			var entryPrice = candle.ClosePrice;
			_targetPrice = entryPrice - offset;
			_stopPrice = entryPrice + offset;
			_isLongPosition = false;
		}
		else
		{
			_targetPrice = null;
			_stopPrice = null;
			_isLongPosition = false;
		}

		return true;
	}

	private bool TryEnterLong(ICandleMessage candle, decimal currentRsi, decimal previousRsi)
	{
		if (!(currentRsi < LowerLevel && previousRsi > LowerLevel))
			return false;

		var volume = TradeVolume;
		if (volume <= 0m)
			return false;

		if (Position < 0)
		{
			volume += Math.Abs(Position);
		}

		BuyMarket(volume);

		var offset = CalculateOffset();
		if (offset > 0m)
		{
			var entryPrice = candle.ClosePrice;
			_targetPrice = entryPrice + offset;
			_stopPrice = entryPrice - offset;
			_isLongPosition = true;
		}
		else
		{
			_targetPrice = null;
			_stopPrice = null;
			_isLongPosition = true;
		}

		return true;
	}

	private decimal CalculateOffset()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return 0m;

		var decimals = Security?.Decimals ?? 0;
		var factor = decimals is 3 or 5 ? 10m : 1m;

		return OffsetInPips * priceStep * factor;
	}
}
