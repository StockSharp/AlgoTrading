using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the ExpPricePosition MetaTrader expert.
/// Combines price position with a step trend filter based on smoothed moving averages.
/// </summary>
public class ExpPricePositionStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _medianFastPeriod;
	private readonly StrategyParam<int> _medianSlowPeriod;
	private readonly StrategyParam<decimal> _tpSlRatio;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal _lastCrossLevel;
	private bool _hasCrossLevel;

	/// <summary>
	/// Fast smoothed moving average period (default: 2).
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow smoothed moving average period (default: 30).
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Median SMMA period used for price position (default: 26).
	/// </summary>
	public int MedianFastPeriod
	{
		get => _medianFastPeriod.Value;
		set => _medianFastPeriod.Value = value;
	}

	/// <summary>
	/// Median SMA period used for price position (default: 20).
	/// </summary>
	public int MedianSlowPeriod
	{
		get => _medianSlowPeriod.Value;
		set => _medianSlowPeriod.Value = value;
	}

	/// <summary>
	/// Take profit to stop loss ratio (default: 3).
	/// </summary>
	public decimal TpSlRatio
	{
		get => _tpSlRatio.Value;
		set => _tpSlRatio.Value = value;
	}

	/// <summary>
	/// Trailing stop value in points (default: 10).
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Use trailing stop protection.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="ExpPricePositionStrategy"/>.
	/// </summary>
	public ExpPricePositionStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 2)
			.SetDisplay("Fast Period", "Fast SMMA period", "Parameters")
			.SetGreaterThanZero();

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetDisplay("Slow Period", "Slow SMMA period", "Parameters")
			.SetGreaterThanZero();

		_medianFastPeriod = Param(nameof(MedianFastPeriod), 26)
			.SetDisplay("Median Fast Period", "Median SMMA period", "Parameters")
			.SetGreaterThanZero();

		_medianSlowPeriod = Param(nameof(MedianSlowPeriod), 20)
			.SetDisplay("Median Slow Period", "Median SMA period", "Parameters")
			.SetGreaterThanZero();

		_tpSlRatio = Param(nameof(TpSlRatio), 3m)
			.SetDisplay("TP/SL Ratio", "Take profit to stop loss ratio", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
			.SetDisplay("Trailing Stop", "Trailing stop value in points", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing Stop", "Enable trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (UseTrailingStop)
		{
			StartProtection(
				stopLoss: new Unit(TrailingStopPips, UnitTypes.Point),
				takeProfit: new Unit(TrailingStopPips * TpSlRatio, UnitTypes.Point));
		}

		var fast = new SmoothedMovingAverage { Length = FastPeriod };
		var slow = new SmoothedMovingAverage { Length = SlowPeriod };
		var medianFast = new SmoothedMovingAverage { Length = MedianFastPeriod };
		var medianSlow = new SimpleMovingAverage { Length = MedianSlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, medianFast, medianSlow, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle,
		decimal fast, decimal slow, decimal medianFast, decimal medianSlow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var signal = (medianFast + medianSlow) / 2m;

		if (candle.OpenPrice <= signal && candle.ClosePrice > signal)
		{
			_lastCrossLevel = candle.LowPrice;
			_hasCrossLevel = true;
		}
		else if (candle.OpenPrice >= signal && candle.ClosePrice < signal)
		{
			_lastCrossLevel = candle.HighPrice;
			_hasCrossLevel = true;
		}

		if (!_hasCrossLevel)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		if (_prevFast is null || _prevSlow is null)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var pricePos = candle.ClosePrice > _lastCrossLevel ? 1 : -1;
		var stepUp = fast > slow && fast > _prevFast && _prevFast > _prevSlow;
		var stepDown = fast < slow && fast < _prevFast && _prevFast < _prevSlow;

		if (pricePos > 0 && stepUp && candle.ClosePrice > candle.OpenPrice && candle.LowPrice < fast && Position <= 0)
			BuyMarket();
		else if (pricePos < 0 && stepDown && candle.ClosePrice < candle.OpenPrice && candle.HighPrice > fast && Position >= 0)
			SellMarket();

		_prevFast = fast;
		_prevSlow = slow;
	}
}

