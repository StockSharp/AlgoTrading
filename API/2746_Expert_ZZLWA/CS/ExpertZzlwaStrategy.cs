using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the ExpertZZLWA MetaTrader strategy with three operation modes.
/// </summary>
public class ExpertZzlwaStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<decimal> _maximumVolume;
	private readonly StrategyParam<StrategyMode> _mode;
	private readonly StrategyParam<TermLevel> _termLevel;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private SmoothedMovingAverage _slowMa;
	private SimpleMovingAverage _fastMa;

	private bool _pendingBuySignal;
	private bool _pendingSellSignal;
	private bool _originalBuyReady;
	private bool _originalSellReady;
	private int _zigZagDirection;
	private decimal _prevSlow;
	private decimal _prevFast;

	private decimal _trackedPosition;
	private decimal _averageEntryPrice;
	private decimal _lastClosedVolume;
	private bool _lastTradeLoss;

	/// <summary>
	/// Operation modes reproduced from the original expert.
	/// </summary>
	public enum StrategyMode
	{
		Original,
		ZigZagAddition,
		MovingAverageTest,
	}

	/// <summary>
	/// ZigZag sensitivity presets available in addition mode.
	/// </summary>
	public enum TermLevel
	{
		ShortTerm,
		MediumTerm,
		LongTerm,
	}

	/// <summary>
	/// Protective stop size in price points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Profit target size in price points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Base order volume used by the strategy.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Enable martingale style position sizing.
	/// </summary>
	public bool UseMartingale
	{
		get => _useMartingale.Value;
		set => _useMartingale.Value = value;
	}

	/// <summary>
	/// Multiplier applied after a losing trade when martingale is active.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum allowed order volume.
	/// </summary>
	public decimal MaximumVolume
	{
		get => _maximumVolume.Value;
		set => _maximumVolume.Value = value;
	}

	/// <summary>
	/// Selected trading mode.
	/// </summary>
	public StrategyMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// ZigZag term preset for addition mode.
	/// </summary>
	public TermLevel ZigZagTerm
	{
		get => _termLevel.Value;
		set => _termLevel.Value = value;
	}

	/// <summary>
	/// Period of the slow smoothed moving average used in MA test mode.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the fast simple moving average used in MA test mode.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpertZzlwaStrategy"/> class.
	/// </summary>
	public ExpertZzlwaStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 600)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (points)", "Protective stop in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 700)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (points)", "Profit target in points", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Default order volume", "Trading");

		_useMartingale = Param(nameof(UseMartingale), false)
		.SetDisplay("Use Martingale", "Enable martingale sizing", "Trading");

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Martingale Multiplier", "Multiplier applied after a loss", "Trading");

		_maximumVolume = Param(nameof(MaximumVolume), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Maximum Volume", "Upper cap for order size", "Trading");

		_mode = Param(nameof(Mode), StrategyMode.Original)
		.SetDisplay("Mode", "Operating mode", "General");

		_termLevel = Param(nameof(ZigZagTerm), TermLevel.LongTerm)
		.SetDisplay("ZigZag Term", "Sensitivity preset for ZigZag", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 150)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA Period", "Smoothed MA length", "Indicators");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA Period", "Simple MA length", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Time frame to analyse", "General");
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

		_highest = null;
		_lowest = null;
		_slowMa = null;
		_fastMa = null;
		_pendingBuySignal = false;
		_pendingSellSignal = false;
		_originalBuyReady = true;
		_originalSellReady = true;
		_zigZagDirection = 0;
		_prevSlow = 0m;
		_prevFast = 0m;
		_trackedPosition = 0m;
		_averageEntryPrice = 0m;
		_lastClosedVolume = BaseVolume;
		_lastTradeLoss = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
		stopLoss: new Unit(StopLossPoints * GetPriceStep(), UnitTypes.Point),
		takeProfit: new Unit(TakeProfitPoints * GetPriceStep(), UnitTypes.Point));

		_originalBuyReady = true;
		_originalSellReady = true;
		_pendingBuySignal = false;
		_pendingSellSignal = false;
		_trackedPosition = 0m;
		_averageEntryPrice = 0m;
		_lastClosedVolume = BaseVolume;
		_lastTradeLoss = false;

		var subscription = SubscribeCandles(CandleType);

		switch (Mode)
		{
			case StrategyMode.Original:
				subscription.Bind(ProcessOriginalCandle).Start();
				break;

			case StrategyMode.ZigZagAddition:
				_highest = new Highest { Length = GetZigZagDepth(ZigZagTerm) };
				_lowest = new Lowest { Length = GetZigZagDepth(ZigZagTerm) };
				subscription.Bind(_highest, _lowest, ProcessAdditionCandle).Start();
				break;

			case StrategyMode.MovingAverageTest:
				_slowMa = new SmoothedMovingAverage { Length = SlowMaPeriod };
				_fastMa = new SimpleMovingAverage { Length = FastMaPeriod };
				subscription.Bind(_slowMa, _fastMa, ProcessMovingAverageCandle).Start();
				break;

			default:
				throw new NotSupportedException($"Unsupported mode {Mode}.");
			}

			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);

				switch (Mode)
				{
					case StrategyMode.ZigZagAddition:
						DrawIndicator(area, _highest);
						DrawIndicator(area, _lowest);
						break;
					case StrategyMode.MovingAverageTest:
						DrawIndicator(area, _slowMa);
						DrawIndicator(area, _fastMa);
						break;
				}

				DrawOwnTrades(area);
			}
		}

		private void ProcessOriginalCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
			return;

			if (!IsFormedAndOnlineAndAllowTrading())
			return;

			if (Position == 0)
			{
				if (_originalBuyReady)
				{
					ExecuteTrade(Sides.Buy);
					_originalBuyReady = false;
					_originalSellReady = true;
				}
				else if (_originalSellReady)
				{
					ExecuteTrade(Sides.Sell);
					_originalSellReady = false;
					_originalBuyReady = true;
				}
			}
		}

		private void ProcessAdditionCandle(ICandleMessage candle, decimal highest, decimal lowest)
		{
			if (candle.State != CandleStates.Finished)
			return;

			if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

			// Detect fresh ZigZag pivots similar to the original indicator buffers.
			if (candle.HighPrice >= highest && _zigZagDirection != 1)
			{
				_pendingSellSignal = true;
				_pendingBuySignal = false;
				_zigZagDirection = 1;
			}
			else if (candle.LowPrice <= lowest && _zigZagDirection != -1)
			{
				_pendingBuySignal = true;
				_pendingSellSignal = false;
				_zigZagDirection = -1;
			}

			DispatchSignals();
		}

		private void ProcessMovingAverageCandle(ICandleMessage candle, decimal slow, decimal fast)
		{
			if (candle.State != CandleStates.Finished)
			return;

			if (!_slowMa.IsFormed || !_fastMa.IsFormed)
			return;

			// Reproduce cross checks from the MQL version.
			var crossDown = _prevSlow > _prevFast && slow < fast;
			var crossUp = _prevSlow < _prevFast && slow > fast;

			_prevSlow = slow;
			_prevFast = fast;

			if (crossUp)
			{
				_pendingBuySignal = true;
				_pendingSellSignal = false;
			}
			else if (crossDown)
			{
				_pendingSellSignal = true;
				_pendingBuySignal = false;
			}

			DispatchSignals();
		}

		private void DispatchSignals()
		{
			if (!IsFormedAndOnlineAndAllowTrading())
			return;

			if (_pendingBuySignal)
			{
				ExecuteTrade(Sides.Buy);
				_pendingBuySignal = false;
				_pendingSellSignal = false;
			}
			else if (_pendingSellSignal)
			{
				ExecuteTrade(Sides.Sell);
				_pendingSellSignal = false;
				_pendingBuySignal = false;
			}
		}

		private void ExecuteTrade(Sides side)
		{
			var volume = GetOrderVolume();
			if (volume <= 0)
			return;

			if (side == Sides.Buy)
			BuyMarket(volume);
			else
			SellMarket(volume);
		}

		private decimal GetOrderVolume()
		{
			if (!UseMartingale)
			return BaseVolume;

			if (!_lastTradeLoss)
			return BaseVolume;

			var nextVolume = _lastClosedVolume * MartingaleMultiplier;
			return nextVolume > MaximumVolume ? BaseVolume : nextVolume;
		}

		private int GetZigZagDepth(TermLevel level)
		{
			return level switch
			{
				TermLevel.ShortTerm => 12,
				TermLevel.MediumTerm => 24,
				_ => 48,
			};
		}

		private decimal GetPriceStep()
		{
			return Security?.PriceStep ?? 1m;
		}

		/// <inheritdoc />
		protected override void OnOwnTradeReceived(MyTrade trade)
		{
			if (trade?.Order?.Side == null)
			return;

			var side = trade.Order.Side.Value;
			var volume = trade.Trade.Volume;
			var price = trade.Trade.Price;

			var previousPosition = _trackedPosition;

			if (side == Sides.Buy)
			{
				if (previousPosition >= 0)
				{
					// Building or creating a long position.
					var newPosition = previousPosition + volume;
					_averageEntryPrice = newPosition == 0m
					? 0m
					: (_averageEntryPrice * previousPosition + price * volume) / newPosition;
					_trackedPosition = newPosition;
				}
				else
				{
					// Closing part or all of a short position.
					var closingVolume = Math.Min(volume, Math.Abs(previousPosition));
					var profit = (_averageEntryPrice - price) * closingVolume;
					var remaining = previousPosition + volume;

					if (remaining >= 0m)
					{
						RegisterClosedTrade(closingVolume, profit);
						if (remaining > 0m)
						{
							// Flip into a new long position with leftover quantity.
							_trackedPosition = remaining;
							_averageEntryPrice = price;
						}
						else
						{
							_trackedPosition = 0m;
							_averageEntryPrice = 0m;
						}
					}
					else
					{
						_trackedPosition = remaining;
						// Average price of the remaining short stays unchanged.
					}
				}
			}
			else
			{
				if (previousPosition <= 0)
				{
					// Building or creating a short position.
					var newPosition = previousPosition - volume;
					var absPrev = Math.Abs(previousPosition);
					var absNew = Math.Abs(newPosition);
					_averageEntryPrice = absNew == 0m
					? 0m
					: (_averageEntryPrice * absPrev + price * volume) / absNew;
					_trackedPosition = newPosition;
				}
				else
				{
					// Closing part or all of a long position.
					var closingVolume = Math.Min(volume, previousPosition);
					var profit = (price - _averageEntryPrice) * closingVolume;
					var remaining = previousPosition - volume;

					if (remaining <= 0m)
					{
						RegisterClosedTrade(closingVolume, profit);
						if (remaining < 0m)
						{
							_trackedPosition = remaining;
							_averageEntryPrice = price;
						}
						else
						{
							_trackedPosition = 0m;
							_averageEntryPrice = 0m;
						}
					}
					else
					{
						_trackedPosition = remaining;
						// Average entry price is preserved for the reduced long position.
					}
				}
			}
		}

		private void RegisterClosedTrade(decimal volume, decimal profit)
		{
			_lastClosedVolume = volume;
			_lastTradeLoss = profit < 0m;
		}
	}
