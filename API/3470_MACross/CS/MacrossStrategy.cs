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
/// Moving average crossover strategy converted from the MQL implementation.
/// Opens long positions when the fast SMA crosses above the slow SMA and shorts on the opposite signal.
/// Includes fixed stop-loss / take-profit distances expressed in pips and a minimum equity guard.
/// </summary>
public class MacrossStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<decimal> _minEquity;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _fastMa;
	private SMA _slowMa;
	private decimal? _previousFast;
	private decimal? _previousSlow;
	private decimal? _entryPrice;
	private Sides? _currentSide;
	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of <see cref="MacrossStrategy"/>.
	/// </summary>
	public MacrossStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Length of the fast simple moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(4, 20, 2);

		_slowPeriod = Param(nameof(SlowPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Length of the slow simple moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Distance to the profit target expressed in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 50m, 5m);

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Distance to the protective stop expressed in pips", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 50m, 5m);

		_lotSize = Param(nameof(LotSize), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Size", "Volume used for each market entry", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 2m, 0.1m);

		_minEquity = Param(nameof(MinEquity), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Equity", "Suspend new trades if portfolio value drops below this level", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for indicator calculations", "General");
	}

	/// <summary>
	/// Length of the fast moving average.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Length of the slow moving average.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Volume used for each market order.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Minimum equity required to allow new trades.
	/// </summary>
	public decimal MinEquity
	{
		get => _minEquity.Value;
		set => _minEquity.Value = value;
	}

	/// <summary>
	/// Type of candles subscribed for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

	_fastMa = null;
	_slowMa = null;
	_previousFast = null;
	_previousSlow = null;
	_entryPrice = null;
	_currentSide = null;
	_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	Volume = LotSize;
	StartProtection();

	_fastMa = new SMA { Length = FastPeriod };
	_slowMa = new SMA { Length = SlowPeriod };

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(_fastMa, _slowMa, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _fastMa);
	DrawIndicator(area, _slowMa);
	DrawOwnTrades(area);
	}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (_fastMa is null || _slowMa is null)
	return;

	EnsurePipSize();

	var closePrice = candle.ClosePrice;

	HandleRiskManagement(closePrice);

	var fastReady = _fastMa.IsFormed;
	var slowReady = _slowMa.IsFormed;

	if (!fastReady || !slowReady)
	{
	_previousFast = fastValue;
	_previousSlow = slowValue;
	return;
	}

	if (_previousFast is null || _previousSlow is null)
	{
	_previousFast = fastValue;
	_previousSlow = slowValue;
	return;
	}

	var crossedUp = _previousFast <= _previousSlow && fastValue > slowValue;
	var crossedDown = _previousFast >= _previousSlow && fastValue < slowValue;

	if (crossedUp || crossedDown)
	{
	if (!IsFormedAndOnlineAndAllowTrading())
	{
	_previousFast = fastValue;
	_previousSlow = slowValue;
	return;
	}

	if (!HasSufficientEquity())
	{
	_previousFast = fastValue;
	_previousSlow = slowValue;
	return;
	}

	if (crossedUp)
	{
	EnterLong(closePrice);
	}
	else if (crossedDown)
	{
	EnterShort(closePrice);
	}
	}

	_previousFast = fastValue;
	_previousSlow = slowValue;
	}

	private void HandleRiskManagement(decimal currentPrice)
	{
	if (_entryPrice is null || _currentSide is null)
	return;

	var entry = _entryPrice.Value;
	var takeProfitOffset = TakeProfitPips > 0m ? GetPriceOffset(TakeProfitPips) : 0m;
	var stopLossOffset = StopLossPips > 0m ? GetPriceOffset(StopLossPips) : 0m;

	if (_currentSide == Sides.Buy && Position > 0)
	{
	if (takeProfitOffset > 0m && currentPrice >= entry + takeProfitOffset)
	{
	SellMarket(Position);
	ResetPositionState();
	return;
	}

	if (stopLossOffset > 0m && currentPrice <= entry - stopLossOffset)
	{
	SellMarket(Position);
	ResetPositionState();
	}
	}
	else if (_currentSide == Sides.Sell && Position < 0)
	{
	var absPosition = Math.Abs(Position);
	if (takeProfitOffset > 0m && currentPrice <= entry - takeProfitOffset)
	{
	BuyMarket(absPosition);
	ResetPositionState();
	return;
	}

	if (stopLossOffset > 0m && currentPrice >= entry + stopLossOffset)
	{
	BuyMarket(absPosition);
	ResetPositionState();
	}
	}
	else if (Position == 0)
	{
	ResetPositionState();
	}
	}

	private void EnterLong(decimal referencePrice)
	{
	if (Position < 0)
	{
	BuyMarket(Math.Abs(Position));
	}

	var volume = PrepareVolume(LotSize);
	if (volume <= 0m)
	return;

	BuyMarket(volume);
	_entryPrice = referencePrice;
	_currentSide = Sides.Buy;
	}

	private void EnterShort(decimal referencePrice)
	{
	if (Position > 0)
	{
	SellMarket(Position);
	}

	var volume = PrepareVolume(LotSize);
	if (volume <= 0m)
	return;

	SellMarket(volume);
	_entryPrice = referencePrice;
	_currentSide = Sides.Sell;
	}

	private bool HasSufficientEquity()
	{
	if (MinEquity <= 0m || Portfolio is null)
	return true;

	var currentValue = Portfolio.CurrentValue ?? Portfolio.BeginValue;
	return currentValue is null || currentValue.Value >= MinEquity;
	}

	private decimal PrepareVolume(decimal desiredVolume)
	{
	var security = Security;
	if (security is null)
	return desiredVolume;

	var volume = desiredVolume;

	var step = security.VolumeStep;
	if (step.HasValue && step.Value > 0m)
	{
	var steps = Math.Round(volume / step.Value, MidpointRounding.AwayFromZero);
	volume = steps * step.Value;
	}

	var min = security.MinVolume;
	if (min.HasValue && volume < min.Value)
	volume = min.Value;

	var max = security.MaxVolume;
	if (max.HasValue && volume > max.Value)
	volume = max.Value;

	return volume;
	}

	private decimal GetPriceOffset(decimal pips)
	{
	return pips * (_pipSize > 0m ? _pipSize : 1m);
	}

	private void EnsurePipSize()
	{
	if (_pipSize > 0m)
	return;

	var security = Security;
	if (security != null)
	{
	if (security.Decimals.HasValue)
	{
	switch (security.Decimals.Value)
	{
		case 2:
		case 3:
			_pipSize = 0.01m;
			break;
		case 4:
		case 5:
			_pipSize = 0.0001m;
			break;
		default:
			_pipSize = security.PriceStep ?? (decimal)Math.Pow(10, -security.Decimals.Value);
			break;
	}
	}
	else if (security.PriceStep.HasValue)
	{
	_pipSize = security.PriceStep.Value;
	}
	}

	if (_pipSize <= 0m)
	_pipSize = 0.0001m;
	}

	private void ResetPositionState()
	{
	_entryPrice = null;
	_currentSide = null;
	}
}

