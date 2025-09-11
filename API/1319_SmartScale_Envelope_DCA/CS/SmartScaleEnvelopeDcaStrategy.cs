
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SmartScale envelope dollar-cost averaging strategy.
/// </summary>
public class SmartScaleEnvelopeDcaStrategy : Strategy
{
	private readonly StrategyParam<int> _envelopeLength;
	private readonly StrategyParam<decimal> _percentOffset;
	private readonly StrategyParam<bool> _useEma;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<int> _cooldown;
	private readonly StrategyParam<int> _maxBuys;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _ma = default!;
	private decimal? _avgEntryPrice;
	private decimal? _lastBuyPrice;
	private int _buyCount;
	private int _lastBuyBar = -1;
	private int _barIndex;
	private decimal? _prevBasis;
	private DateTimeOffset _startDate;

	/// <summary>
	/// Envelope length.
	/// </summary>
	public int EnvelopeLength
	{
		get => _envelopeLength.Value;
		set => _envelopeLength.Value = value;
	}

	/// <summary>
	/// Envelope percent offset.
	/// </summary>
	public decimal PercentOffset
	{
		get => _percentOffset.Value;
		set => _percentOffset.Value = value;
	}

	/// <summary>
	/// Use EMA instead of SMA.
	/// </summary>
	public bool UseEma
	{
		get => _useEma.Value;
		set => _useEma.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percent from average entry.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Candles between buys.
	/// </summary>
	public int Cooldown
	{
		get => _cooldown.Value;
		set => _cooldown.Value = value;
	}

	/// <summary>
	/// Maximum number of buy-ins.
	/// </summary>
	public int MaxBuys
	{
		get => _maxBuys.Value;
		set => _maxBuys.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SmartScaleEnvelopeDcaStrategy"/>.
	/// </summary>
	public SmartScaleEnvelopeDcaStrategy()
	{
		_envelopeLength = Param(nameof(EnvelopeLength), 13)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Envelope Length", "Envelope length", "Parameters");

		_percentOffset = Param(nameof(PercentOffset), 6.6m)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Envelope % Offset", "Envelope percent offset", "Parameters");

		_useEma = Param(nameof(UseEma), false)
			.SetCanOptimize(true)
			.SetDisplay("Use EMA", "Use exponential MA", "Parameters");

		_stopLossPercent = Param(nameof(StopLossPercent), 15m)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Parameters");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 5m)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Take Profit %", "Take profit percent", "Parameters");

		_cooldown = Param(nameof(Cooldown), 7)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Candles Between Buys", "Cooldown between buys", "Parameters");

		_maxBuys = Param(nameof(MaxBuys), 8)
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetDisplay("Max Buys", "Maximum buy-ins", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "Parameters");
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
		_ma = default!;
		_prevBasis = null;
		_barIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		_startDate = CurrentTime - TimeSpan.FromDays(365);

		_ma = UseEma ? new ExponentialMovingAverage { Length = EnvelopeLength } : new SimpleMovingAverage { Length = EnvelopeLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma, ProcessCandle)
			.Chart(_ma)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal basis)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		if (!_ma.IsFormed || !IsFormedAndOnlineAndAllowTrading())
		{
			_prevBasis = basis;
			return;
		}

		var percent = PercentOffset / 100m;
		var upper = basis * (1 + percent);
		var lower = basis * (1 - percent);

		var inDateRange = candle.OpenTime >= _startDate;
		var isUptrend = _prevBasis.HasValue && basis > _prevBasis.Value;
		var lowBelowLower = candle.LowPrice < lower;
		var highAboveUpper = candle.HighPrice > upper;
		var cooldownPassed = _lastBuyBar == -1 || _barIndex - _lastBuyBar >= Cooldown;
		var belowAvgEntry = !_avgEntryPrice.HasValue || candle.ClosePrice < _avgEntryPrice.Value;
		var lowerThanLastBuy = !_lastBuyPrice.HasValue || candle.ClosePrice < _lastBuyPrice.Value;
		var allowBuyIn = (belowAvgEntry && lowerThanLastBuy) || isUptrend;
		var priceNotHigherThanLastBuy = !_lastBuyPrice.HasValue || candle.ClosePrice <= _lastBuyPrice.Value;
		var sellCondition = _avgEntryPrice.HasValue && candle.ClosePrice >= _avgEntryPrice.Value * (1 + TakeProfitPercent / 100m);
		var stopLossTriggered = _avgEntryPrice.HasValue && candle.ClosePrice <= _avgEntryPrice.Value * (1 - StopLossPercent / 100m);

		if (inDateRange && lowBelowLower && cooldownPassed && _buyCount < MaxBuys && allowBuyIn && priceNotHigherThanLastBuy)
		{
			BuyMarket();
			_buyCount++;
			_lastBuyBar = _barIndex;
			_lastBuyPrice = candle.ClosePrice;
			_avgEntryPrice = _avgEntryPrice is null ? candle.ClosePrice : (_avgEntryPrice * (_buyCount - 1) + candle.ClosePrice) / _buyCount;
		}

		if (Position > 0)
		{
			if (highAboveUpper && sellCondition)
			{
				SellMarket(Position);
				ResetState();
			}
			else if (stopLossTriggered)
			{
				SellMarket(Position);
				ResetState();
			}
		}

		_prevBasis = basis;
	}

	private void ResetState()
	{
		_avgEntryPrice = null;
		_lastBuyPrice = null;
		_buyCount = 0;
		_lastBuyBar = -1;
	}
}
