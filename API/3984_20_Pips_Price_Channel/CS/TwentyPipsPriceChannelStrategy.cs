using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Converted "20 pips" price channel strategy.
/// </summary>
public class TwentyPipsPriceChannelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingOffsetPips;
	private readonly StrategyParam<decimal> _recoveryMultiplier;
	private readonly StrategyParam<decimal> _volumeParam;

	private SimpleMovingAverage _fastMa;
	private SimpleMovingAverage _slowMa;
	private Highest _channelHigh;
	private Lowest _channelLow;

	private decimal? _prevOpen;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal? _channelUpperPrev;
	private decimal? _channelUpperPrev2;
	private decimal? _channelLowerPrev;
	private decimal? _channelLowerPrev2;

	private decimal? _entryPrice;
	private bool _isLong;
	private bool _lastTradeWasLoss;
	private decimal? _takeProfitPrice;
	private decimal? _stopPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="TwentyPipsPriceChannelStrategy"/>.
	/// </summary>
	public TwentyPipsPriceChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type", "General");

		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Donchian channel lookback", "Parameters");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Slow moving average length", "Parameters");

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Target distance in pips", "Risk");

		_trailingOffsetPips = Param(nameof(TrailingOffsetPips), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Offset (pips)", "Offset for stop trail", "Risk");

		_recoveryMultiplier = Param(nameof(RecoveryMultiplier), 2m)
			.SetGreaterOrEquals(1m)
			.SetDisplay("Recovery Multiplier", "Volume multiplier after a loss", "Money Management");

		_volumeParam = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base trading volume", "General");
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Donchian channel lookback period.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop offset expressed in pips.
	/// </summary>
	public decimal TrailingOffsetPips
	{
		get => _trailingOffsetPips.Value;
		set => _trailingOffsetPips.Value = value;
	}

	/// <summary>
	/// Volume multiplier applied after losing trades.
	/// </summary>
	public decimal RecoveryMultiplier
	{
		get => _recoveryMultiplier.Value;
		set => _recoveryMultiplier.Value = value;
	}

	/// <summary>
	/// Base trading volume.
	/// </summary>
	public decimal BaseVolume
	{
		get => _volumeParam.Value;
		set => _volumeParam.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMa = null;
		_slowMa = null;
		_channelHigh = null;
		_channelLow = null;

		_prevOpen = null;
		_prevHigh = null;
		_prevLow = null;
		_prevFast = null;
		_prevSlow = null;
		_channelUpperPrev = null;
		_channelUpperPrev2 = null;
		_channelLowerPrev = null;
		_channelLowerPrev2 = null;

		_entryPrice = null;
		_isLong = false;
		_lastTradeWasLoss = false;
		_takeProfitPrice = null;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new SimpleMovingAverage { Length = 1 };
		_slowMa = new SimpleMovingAverage { Length = SlowMaPeriod };
		_channelHigh = new Highest { Length = ChannelPeriod };
		_channelLow = new Lowest { Length = ChannelPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle);
		subscription.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		var fastValue = _fastMa.Process(typical);
		var slowValue = _slowMa.Process(candle.ClosePrice);
		var upperValue = _channelHigh.Process(candle.HighPrice);
		var lowerValue = _channelLow.Process(candle.LowPrice);

		if (!fastValue.IsFinal || !slowValue.IsFinal || !upperValue.IsFinal || !lowerValue.IsFinal)
		{
			UpdateHistory(candle, null, null, null, null);
			return;
		}

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();
		var channelUpper = upperValue.GetValue<decimal>();
		var channelLower = lowerValue.GetValue<decimal>();

		var upperShift2 = _channelUpperPrev2;
		var lowerShift2 = _channelLowerPrev2;

		TryHandleOpen(candle, fast, slow);
		TryHandleExistingPosition(candle, upperShift2, lowerShift2);

		UpdateHistory(candle, fast, slow, channelUpper, channelLower);
	}

	private void TryHandleOpen(ICandleMessage candle, decimal currentFast, decimal currentSlow)
	{
		if (Position != 0)
			return;

		if (_prevFast is null || _prevSlow is null || _prevOpen is null)
			return;

		var volume = BaseVolume;
		if (_lastTradeWasLoss)
			volume *= RecoveryMultiplier;

		var openDecreased = candle.OpenPrice < _prevOpen.Value;
		var openIncreased = candle.OpenPrice > _prevOpen.Value;
		var fastAboveSlow = _prevFast.Value > _prevSlow.Value;
		var fastBelowSlow = _prevFast.Value < _prevSlow.Value;

		if (fastAboveSlow && openDecreased)
		{
			EnterLong(candle.OpenPrice, volume);
		}
		else if (fastBelowSlow && openIncreased)
		{
			EnterShort(candle.OpenPrice, volume);
		}
	}

	private void TryHandleExistingPosition(ICandleMessage candle, decimal? channelUpperShift2, decimal? channelLowerShift2)
	{
		if (Position == 0 || _entryPrice is null)
			return;

		var priceStep = GetPriceStep();
		var takeProfitReached = false;
		var stopHit = false;
		decimal exitPrice = candle.ClosePrice;

		if (_isLong)
		{
			if (_takeProfitPrice is decimal tp && candle.HighPrice >= tp)
			{
				exitPrice = tp;
				takeProfitReached = true;
			}

			if (!takeProfitReached && _stopPrice is decimal sp && candle.LowPrice <= sp)
			{
				exitPrice = sp;
				stopHit = true;
			}

			if (!takeProfitReached && !stopHit && channelLowerShift2 is decimal lower && _prevLow is decimal prevLow)
			{
				if (prevLow < lower)
				{
					if (candle.OpenPrice < prevLow)
					{
						exitPrice = candle.OpenPrice;
						stopHit = true;
					}
					else
					{
						_stopPrice = prevLow - TrailingOffsetPips * priceStep;
					}
				}
			}
		}
		else
		{
			if (_takeProfitPrice is decimal tp && candle.LowPrice <= tp)
			{
				exitPrice = tp;
				takeProfitReached = true;
			}

			if (!takeProfitReached && _stopPrice is decimal sp && candle.HighPrice >= sp)
			{
				exitPrice = sp;
				stopHit = true;
			}

			if (!takeProfitReached && !stopHit && channelUpperShift2 is decimal upper && _prevHigh is decimal prevHigh)
			{
				if (prevHigh > upper)
				{
					if (candle.OpenPrice > prevHigh)
					{
						exitPrice = candle.OpenPrice;
						stopHit = true;
					}
					else
					{
						_stopPrice = prevHigh + TrailingOffsetPips * priceStep;
					}
				}
			}
		}

		if (takeProfitReached || stopHit)
		{
			ExitPosition(exitPrice);
			return;
		}
	}

	private void EnterLong(decimal price, decimal volume)
	{
		BuyMarket(volume);
		_entryPrice = price;
		_isLong = true;
		_takeProfitPrice = price + TakeProfitPips * GetPriceStep();
		_stopPrice = null;
	}

	private void EnterShort(decimal price, decimal volume)
	{
		SellMarket(volume);
		_entryPrice = price;
		_isLong = false;
		_takeProfitPrice = price - TakeProfitPips * GetPriceStep();
		_stopPrice = null;
	}

	private void ExitPosition(decimal exitPrice)
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));

		if (_entryPrice is decimal entry)
		{
			var profit = _isLong ? exitPrice - entry : entry - exitPrice;
			_lastTradeWasLoss = profit < 0m;
		}

		_entryPrice = null;
		_takeProfitPrice = null;
		_stopPrice = null;
	}

	private void UpdateHistory(ICandleMessage candle, decimal? fast, decimal? slow, decimal? channelUpper, decimal? channelLower)
	{
		_prevOpen = candle.OpenPrice;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;

		if (fast.HasValue)
		{
			_prevFast = fast;
		}

		if (slow.HasValue)
		{
			_prevSlow = slow;
		}

		if (channelUpper.HasValue)
		{
			_channelUpperPrev2 = _channelUpperPrev;
			_channelUpperPrev = channelUpper;
		}

		if (channelLower.HasValue)
		{
			_channelLowerPrev2 = _channelLowerPrev;
			_channelLowerPrev = channelLower;
		}
	}

	private decimal GetPriceStep()
	{
		return Security?.PriceStep ?? 0.0001m;
	}
}
