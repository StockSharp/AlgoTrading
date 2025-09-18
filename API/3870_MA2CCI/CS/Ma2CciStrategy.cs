
using System;
using System.Collections.Generic;
using StockSharp.Algo;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA2CCI strategy converted from the MetaTrader 4 expert advisor.
/// Combines a dual moving average crossover with a CCI filter and ATR-based stop-losses.
/// </summary>
public class Ma2CciStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _riskFraction;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousFast;
	private decimal? _previousSlow;
	private decimal? _previousCci;
	private decimal _signedPosition;
	private Sides? _lastEntrySide;
	private decimal? _lastEntryPrice;
	private decimal? _pendingStopPrice;
	private Order? _activeStopOrder;
	private int _consecutiveLosses;

	/// <summary>
	/// Initializes a new instance of the <see cref="Ma2CciStrategy"/> class.
	/// </summary>
	public Ma2CciStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 4)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA Period", "Length of the fast simple moving average", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 8)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA Period", "Length of the slow simple moving average", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 4)
		.SetGreaterThanZero()
		.SetDisplay("CCI Period", "Lookback for the Commodity Channel Index", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 4)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Length of the Average True Range stop filter", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("ATR Multiplier", "Multiplier applied to the ATR stop distance", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Minimum trade size before risk adjustment", "Position Sizing");

		_riskFraction = Param(nameof(RiskFraction), 0.02m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("Risk Fraction", "Fraction of portfolio equity risked per trade", "Position Sizing");

		_decreaseFactor = Param(nameof(DecreaseFactor), 3m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("Decrease Factor", "Divisor for reducing size after consecutive losses", "Position Sizing");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used by the indicators", "General");
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Commodity Channel Index period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Average True Range period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the ATR stop distance.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Minimum volume used before risk sizing adjustments.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Fraction of equity risked per trade.
	/// </summary>
	public decimal RiskFraction
	{
		get => _riskFraction.Value;
		set => _riskFraction.Value = value;
	}

	/// <summary>
	/// Divisor controlling how quickly size decreases after losses.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
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

		_previousFast = null;
		_previousSlow = null;
		_previousCci = null;
		_signedPosition = 0m;
		_lastEntrySide = null;
		_lastEntryPrice = null;
		_pendingStopPrice = null;
		_activeStopOrder = null;
		_consecutiveLosses = 0;

		Volume = BaseVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = BaseVolume;

		var fastMa = new SimpleMovingAverage { Length = FastMaPeriod };
		var slowMa = new SimpleMovingAverage { Length = SlowMaPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(fastMa, slowMa, cci, atr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			// Visualize the price action together with the indicators for easier validation.
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawIndicator(area, cci);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal cciValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_previousFast.HasValue || !_previousSlow.HasValue || !_previousCci.HasValue)
		{
			// Wait until all indicators have produced at least one completed value.
			_previousFast = fastValue;
			_previousSlow = slowValue;
			_previousCci = cciValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousFast = fastValue;
			_previousSlow = slowValue;
			_previousCci = cciValue;
			return;
		}

		var previousFast = _previousFast.Value;
		var previousSlow = _previousSlow.Value;
		var previousCci = _previousCci.Value;

		// Detect moving average crossovers using previous candle values.
		var fastCrossUp = previousFast <= previousSlow && fastValue > slowValue;
		var fastCrossDown = previousFast >= previousSlow && fastValue < slowValue;

		// Detect CCI zero-line crosses to confirm direction.
		var cciCrossUp = previousCci <= 0m && cciValue > 0m;
		var cciCrossDown = previousCci >= 0m && cciValue < 0m;

		// Handle exit logic before evaluating new entries.
		if (fastCrossDown && _signedPosition > 0m)
		{
			SellMarket(_signedPosition);
			CancelStopOrder();
		}
		else if (fastCrossUp && _signedPosition < 0m)
		{
			BuyMarket(Math.Abs(_signedPosition));
			CancelStopOrder();
		}

		// Entry rules mirror the MetaTrader expert advisor: trade only when no position is open.
		if (_signedPosition == 0m)
		{
			if (fastCrossUp && cciCrossUp)
			{
				var volume = CalculateTradeVolume();
				if (volume > 0m)
				{
					BuyMarket(volume);

					if (atrValue > 0m)
					{
						_pendingStopPrice = candle.ClosePrice - AtrMultiplier * atrValue;
					}
				}
			}
			else if (fastCrossDown && cciCrossDown)
			{
				var volume = CalculateTradeVolume();
				if (volume > 0m)
				{
					SellMarket(volume);

					if (atrValue > 0m)
					{
						_pendingStopPrice = candle.ClosePrice + AtrMultiplier * atrValue;
					}
				}
			}
		}

		_previousFast = fastValue;
		_previousSlow = slowValue;
		_previousCci = cciValue;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var volume = trade.Trade.Volume;
		if (volume <= 0m)
		return;

		var delta = trade.Order.Side == Sides.Buy ? volume : -volume;
		var previousPosition = _signedPosition;
		_signedPosition += delta;

		if (previousPosition == 0m && _signedPosition != 0m)
		{
			// A new position has just been opened.
			_lastEntrySide = trade.Order.Side;
			_lastEntryPrice = trade.Trade.Price;

			if (_pendingStopPrice.HasValue)
			{
				SubmitStopOrder(_pendingStopPrice.Value);
				_pendingStopPrice = null;
			}
		}
		else if (previousPosition != 0m && _signedPosition == 0m)
		{
			// Entire position closed, evaluate trade result for the loss streak logic.
			if (_lastEntrySide.HasValue && _lastEntryPrice.HasValue)
			{
				var exitPrice = trade.Trade.Price;
				var entryPrice = _lastEntryPrice.Value;
				var profit = _lastEntrySide == Sides.Buy
				? exitPrice - entryPrice
				: entryPrice - exitPrice;

				if (profit > 0m)
				{
					_consecutiveLosses = 0;
				}
				else if (profit < 0m)
				{
					_consecutiveLosses++;
				}
			}

			_lastEntrySide = null;
			_lastEntryPrice = null;
			_pendingStopPrice = null;
			CancelStopOrder();
		}
		else if (Math.Sign(previousPosition) == Math.Sign(_signedPosition) && _pendingStopPrice.HasValue)
		{
			// Additional fills in the same direction reuse the latest computed stop level.
			SubmitStopOrder(_pendingStopPrice.Value);
			_pendingStopPrice = null;
		}
	}

	private decimal CalculateTradeVolume()
	{
		// Start from the configured base volume, mirroring the MT4 "Lots" parameter.
		var volume = BaseVolume > 0m ? BaseVolume : 1m;

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity > 0m && RiskFraction > 0m)
		{
			var riskBasedVolume = equity * RiskFraction / 1000m;
			if (riskBasedVolume > 0m)
			volume = Math.Max(volume, riskBasedVolume);
		}

		if (DecreaseFactor > 0m && _consecutiveLosses > 1)
		{
			var reduction = volume * _consecutiveLosses / DecreaseFactor;
			volume -= reduction;
		}

		if (volume <= 0m)
		volume = BaseVolume > 0m ? BaseVolume : 1m;

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

	private void SubmitStopOrder(decimal stopPrice)
	{
		if (_signedPosition == 0m)
		return;

		CancelStopOrder();

		var volume = Math.Abs(_signedPosition);
		if (volume <= 0m)
		return;

		var side = _signedPosition > 0m ? Sides.Sell : Sides.Buy;
		var stopOrder = CreateOrder(side, stopPrice, volume);

		_activeStopOrder = stopOrder;
		RegisterOrder(stopOrder);
	}

	private void CancelStopOrder()
	{
		if (_activeStopOrder == null)
		return;

		CancelOrder(_activeStopOrder);
		_activeStopOrder = null;
	}
}
