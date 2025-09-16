using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the "Example of MACD Automated" MQL4 expert advisor.
/// The strategy waits for MACD agreement on two timeframes and uses AdvancedMM sizing.
/// </summary>
public class ExampleOfMacdAutomatedStrategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<DataType> _entryCandleType;
	private readonly StrategyParam<DataType> _filterCandleType;

	private MovingAverageConvergenceDivergenceSignal _entryMacd = null!;
	private MovingAverageConvergenceDivergenceSignal _filterMacd = null!;

	private decimal? _lastEntryMacd;
	private decimal? _lastFilterMacd;

	private readonly List<TradeInfo> _tradeHistory = new();

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal _entryVolume;
	private int _entryDirection;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExampleOfMacdAutomatedStrategy"/> class.
	/// </summary>
	public ExampleOfMacdAutomatedStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Starting order volume for AdvancedMM", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (steps)", "Stop-loss distance in price steps", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 30m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (steps)", "Take-profit distance in price steps", "Risk")
			.SetCanOptimize(true);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length", "Indicators")
			.SetCanOptimize(true);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length", "Indicators")
			.SetCanOptimize(true);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA length", "Indicators")
			.SetCanOptimize(true);

		_entryCandleType = Param(nameof(EntryCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Entry Timeframe", "Working timeframe for entries", "General");

		_filterCandleType = Param(nameof(FilterCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Filter Timeframe", "Higher timeframe used as trend filter", "General");
	}

	/// <summary>
	/// Base volume parameter.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// MACD signal EMA length.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Timeframe used for entries.
	/// </summary>
	public DataType EntryCandleType
	{
		get => _entryCandleType.Value;
		set => _entryCandleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe used as a trend filter.
	/// </summary>
	public DataType FilterCandleType
	{
		get => _filterCandleType.Value;
		set => _filterCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, EntryCandleType), (Security, FilterCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastEntryMacd = null;
		_lastFilterMacd = null;
		_tradeHistory.Clear();
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_entryVolume = 0m;
		_entryDirection = 0;

		_entryMacd?.Reset();
		_filterMacd?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create MACD indicators for entry and filter timeframes.
		_entryMacd = CreateMacd();
		_filterMacd = CreateMacd();

		var entrySubscription = SubscribeCandles(EntryCandleType);
		entrySubscription
			.BindEx(_entryMacd, ProcessEntryCandle)
			.Start();

		var filterSubscription = SubscribeCandles(FilterCandleType);
		filterSubscription
			.BindEx(_filterMacd, ProcessFilterCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, entrySubscription);
			DrawIndicator(area, _entryMacd);
			DrawIndicator(area, _filterMacd);
			DrawOwnTrades(area);
		}
	}

	private MovingAverageConvergenceDivergenceSignal CreateMacd()
	{
		// Instantiate MACD with shared parameters for both timeframes.
		return new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
			},
			SignalMa = { Length = MacdSignalLength }
		};
	}

	private void ProcessFilterCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		// Process only completed candles on the filter timeframe.
		if (candle.State != CandleStates.Finished)
		return;

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		_lastFilterMacd = macd.Macd;
	}

	private void ProcessEntryCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		// Ensure that we operate on final candle values only.
		if (candle.State != CandleStates.Finished)
		return;

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		_lastEntryMacd = macd.Macd;

		// Manage protective exits before searching for new entries.
		if (HandleProtection(candle))
		return;

		// Skip further processing if there is still an open position.
		if (Position != 0)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_entryMacd.IsFormed || !_filterMacd.IsFormed)
		return;

		if (_lastEntryMacd is null || _lastFilterMacd is null)
		return;

		var entryMacdValue = _lastEntryMacd.Value;
		var filterMacdValue = _lastFilterMacd.Value;

		// Long signal requires both MACD values above zero.
		if (entryMacdValue > 0m && filterMacdValue > 0m)
		{
			EnterPosition(candle.ClosePrice, true);
		}
		// Short signal requires both MACD values below zero.
		else if (entryMacdValue < 0m && filterMacdValue < 0m)
		{
			EnterPosition(candle.ClosePrice, false);
		}
	}

	private void EnterPosition(decimal price, bool isLong)
	{
		var volume = CalculateTradeVolume();
		if (volume <= 0m)
		return;

		if (isLong)
		{
			BuyMarket(volume);
			RegisterEntry(price, volume, 1);
		}
		else
		{
			SellMarket(volume);
			RegisterEntry(price, volume, -1);
		}
	}

	private void RegisterEntry(decimal price, decimal volume, int direction)
	{
		// Store entry information for later profit calculation.
		_entryPrice = price;
		_entryVolume = volume;
		_entryDirection = direction;

		UpdateProtectionLevels(price, direction > 0);
	}

	private void UpdateProtectionLevels(decimal price, bool isLong)
	{
		var point = GetPointValue();

		if (point <= 0m)
		{
			_stopPrice = null;
			_takeProfitPrice = null;
			return;
		}

		if (isLong)
		{
			_stopPrice = StopLossPoints > 0m ? price - StopLossPoints * point : null;
			_takeProfitPrice = TakeProfitPoints > 0m ? price + TakeProfitPoints * point : null;
		}
		else
		{
			_stopPrice = StopLossPoints > 0m ? price + StopLossPoints * point : null;
			_takeProfitPrice = TakeProfitPoints > 0m ? price - TakeProfitPoints * point : null;
		}
	}

	private bool HandleProtection(ICandleMessage candle)
	{
		if (Position == 0 || _entryDirection == 0)
		return false;

		if (_entryDirection > 0)
		{
			if (TryGetLongExitPrice(candle, out var exitPrice))
			{
				SellMarket(Math.Abs(Position));
				RegisterClosedTrade(exitPrice);
				return true;
			}
		}
		else
		{
			if (TryGetShortExitPrice(candle, out var exitPrice))
			{
				BuyMarket(Math.Abs(Position));
				RegisterClosedTrade(exitPrice);
				return true;
			}
		}

		return false;
	}

	private bool TryGetLongExitPrice(ICandleMessage candle, out decimal exitPrice)
	{
		exitPrice = 0m;

		if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
		{
			exitPrice = _stopPrice.Value;
			return true;
		}

		if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
		{
			exitPrice = _takeProfitPrice.Value;
			return true;
		}

		return false;
	}

	private bool TryGetShortExitPrice(ICandleMessage candle, out decimal exitPrice)
	{
		exitPrice = 0m;

		if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
		{
			exitPrice = _stopPrice.Value;
			return true;
		}

		if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
		{
			exitPrice = _takeProfitPrice.Value;
			return true;
		}

		return false;
	}

	private void RegisterClosedTrade(decimal exitPrice)
	{
		if (!_entryPrice.HasValue || _entryVolume <= 0m || _entryDirection == 0)
		return;

		var entryPrice = _entryPrice.Value;
		var volume = _entryVolume;
		var direction = _entryDirection;

		var profit = (exitPrice - entryPrice) * direction * volume;

		_tradeHistory.Add(new TradeInfo(volume, profit));
		if (_tradeHistory.Count > 200)
		_tradeHistory.RemoveAt(0);

		_entryPrice = null;
		_entryVolume = 0m;
		_entryDirection = 0;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	private decimal CalculateTradeVolume()
	{
		var baseVolume = BaseVolume;
		if (baseVolume <= 0m)
		return 0m;

		if (_tradeHistory.Count < 2)
		return baseVolume;

		var advancedLots = 0m;
		var profit1 = false;
		var profit2 = false;
		var firstIteration = true;

		for (var i = _tradeHistory.Count - 1; i >= 0; i--)
		{
			var trade = _tradeHistory[i];
			var isProfit = trade.Profit >= 0m;

			if (isProfit && profit1)
			return baseVolume;

			if (firstIteration)
			{
				if (isProfit)
				{
					profit1 = true;
				}
				else
				{
					return trade.Volume;
				}

				firstIteration = false;
			}

			if (isProfit && profit2)
			return advancedLots > 0m ? advancedLots : baseVolume;

			if (isProfit)
			{
				profit2 = true;
			}
			else
			{
				profit1 = false;
				profit2 = false;
				advancedLots += trade.Volume;
			}
		}

		return advancedLots > 0m ? advancedLots : baseVolume;
	}

	private decimal GetPointValue()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}

	private readonly struct TradeInfo
	{
		public TradeInfo(decimal volume, decimal profit)
		{
			Volume = volume;
			Profit = profit;
		}

		public decimal Volume { get; }
		public decimal Profit { get; }
	}
}
