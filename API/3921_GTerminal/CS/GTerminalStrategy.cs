using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor GTerminal V5.
/// Provides line-based manual trading controls using configurable price levels.
/// Recreates the original behaviour of entering and exiting positions when price crosses virtual lines.
/// </summary>
public class GTerminalStrategy : Strategy
{
	private readonly StrategyParam<int> _crossMethod;
	private readonly StrategyParam<int> _startShift;
	private readonly StrategyParam<bool> _pauseTrading;
	private readonly StrategyParam<bool> _useInitialProtection;
	private readonly StrategyParam<decimal> _buyStopLevel;
	private readonly StrategyParam<decimal> _buyLimitLevel;
	private readonly StrategyParam<decimal> _sellStopLevel;
	private readonly StrategyParam<decimal> _sellLimitLevel;
	private readonly StrategyParam<decimal> _longStopLevelParam;
	private readonly StrategyParam<decimal> _longTakeProfitLevelParam;
	private readonly StrategyParam<decimal> _shortStopLevelParam;
	private readonly StrategyParam<decimal> _shortTakeProfitLevelParam;
	private readonly StrategyParam<decimal> _allLongStopLevelParam;
	private readonly StrategyParam<decimal> _allLongTakeProfitLevelParam;
	private readonly StrategyParam<decimal> _allShortStopLevelParam;
	private readonly StrategyParam<decimal> _allShortTakeProfitLevelParam;
	private readonly StrategyParam<decimal> _initialLongStopLevelParam;
	private readonly StrategyParam<decimal> _initialLongTakeProfitLevelParam;
	private readonly StrategyParam<decimal> _initialShortStopLevelParam;
	private readonly StrategyParam<decimal> _initialShortTakeProfitLevelParam;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closeHistory = new();

	private decimal _longEntryPrice;
	private decimal _shortEntryPrice;
	private decimal _longStopLevel;
	private decimal _longTakeProfitLevel;
	private decimal _shortStopLevel;
	private decimal _shortTakeProfitLevel;


	/// <summary>
	/// Crossing method: 0 requires the close to cross the level, 1 triggers on current price being beyond the level.
	/// </summary>
	public int CrossMethod
	{
		get => _crossMethod.Value;
		set => _crossMethod.Value = value;
	}

	/// <summary>
	/// Candle shift used to evaluate the crossing (0 uses the current close, 1 uses the previous close, etc.).
	/// </summary>
	public int StartShift
	{
		get => _startShift.Value;
		set => _startShift.Value = value;
	}

	/// <summary>
	/// Pauses all trading logic when enabled.
	/// </summary>
	public bool PauseTrading
	{
		get => _pauseTrading.Value;
		set => _pauseTrading.Value = value;
	}

	/// <summary>
	/// Applies initial protective stop and take-profit levels after each filled entry.
	/// </summary>
	public bool UseInitialProtection
	{
		get => _useInitialProtection.Value;
		set => _useInitialProtection.Value = value;
	}

	/// <summary>
	/// Price level that opens a long position when crossed from below.
	/// </summary>
	public decimal BuyStopLevel
	{
		get => _buyStopLevel.Value;
		set => _buyStopLevel.Value = value;
	}

	/// <summary>
	/// Price level that opens a long position when crossed from above.
	/// </summary>
	public decimal BuyLimitLevel
	{
		get => _buyLimitLevel.Value;
		set => _buyLimitLevel.Value = value;
	}

	/// <summary>
	/// Price level that opens a short position when crossed from above.
	/// </summary>
	public decimal SellStopLevel
	{
		get => _sellStopLevel.Value;
		set => _sellStopLevel.Value = value;
	}

	/// <summary>
	/// Price level that opens a short position when crossed from below.
	/// </summary>
	public decimal SellLimitLevel
	{
		get => _sellLimitLevel.Value;
		set => _sellLimitLevel.Value = value;
	}

	/// <summary>
	/// Stop-loss level that closes the current long position when broken.
	/// </summary>
	public decimal LongStopLevel
	{
		get => _longStopLevelParam.Value;
		set => _longStopLevelParam.Value = value;
	}

	/// <summary>
	/// Take-profit level that closes the current long position when reached.
	/// </summary>
	public decimal LongTakeProfitLevel
	{
		get => _longTakeProfitLevelParam.Value;
		set => _longTakeProfitLevelParam.Value = value;
	}

	/// <summary>
	/// Stop-loss level that closes the current short position when broken.
	/// </summary>
	public decimal ShortStopLevel
	{
		get => _shortStopLevelParam.Value;
		set => _shortStopLevelParam.Value = value;
	}

	/// <summary>
	/// Take-profit level that closes the current short position when reached.
	/// </summary>
	public decimal ShortTakeProfitLevel
	{
		get => _shortTakeProfitLevelParam.Value;
		set => _shortTakeProfitLevelParam.Value = value;
	}

	/// <summary>
	/// Global stop level for all long exposure.
	/// </summary>
	public decimal AllLongStopLevel
	{
		get => _allLongStopLevelParam.Value;
		set => _allLongStopLevelParam.Value = value;
	}

	/// <summary>
	/// Global take-profit level for all long exposure.
	/// </summary>
	public decimal AllLongTakeProfitLevel
	{
		get => _allLongTakeProfitLevelParam.Value;
		set => _allLongTakeProfitLevelParam.Value = value;
	}

	/// <summary>
	/// Global stop level for all short exposure.
	/// </summary>
	public decimal AllShortStopLevel
	{
		get => _allShortStopLevelParam.Value;
		set => _allShortStopLevelParam.Value = value;
	}

	/// <summary>
	/// Global take-profit level for all short exposure.
	/// </summary>
	public decimal AllShortTakeProfitLevel
	{
		get => _allShortTakeProfitLevelParam.Value;
		set => _allShortTakeProfitLevelParam.Value = value;
	}

	/// <summary>
	/// Initial stop level applied to new long positions when automatic protection is enabled.
	/// </summary>
	public decimal InitialLongStopLevel
	{
		get => _initialLongStopLevelParam.Value;
		set => _initialLongStopLevelParam.Value = value;
	}

	/// <summary>
	/// Initial take-profit level applied to new long positions when automatic protection is enabled.
	/// </summary>
	public decimal InitialLongTakeProfitLevel
	{
		get => _initialLongTakeProfitLevelParam.Value;
		set => _initialLongTakeProfitLevelParam.Value = value;
	}

	/// <summary>
	/// Initial stop level applied to new short positions when automatic protection is enabled.
	/// </summary>
	public decimal InitialShortStopLevel
	{
		get => _initialShortStopLevelParam.Value;
		set => _initialShortStopLevelParam.Value = value;
	}

	/// <summary>
	/// Initial take-profit level applied to new short positions when automatic protection is enabled.
	/// </summary>
	public decimal InitialShortTakeProfitLevel
	{
		get => _initialShortTakeProfitLevelParam.Value;
		set => _initialShortTakeProfitLevelParam.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate crossings.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public GTerminalStrategy()
	{

		_crossMethod = Param(nameof(CrossMethod), 1)
			.SetDisplay("Cross Method", "0 = strict crossing, 1 = instant trigger", "General")
			.SetCanOptimize(true)
			.SetOptimize(0, 1, 1);

		_startShift = Param(nameof(StartShift), 0)
			.SetDisplay("Start Shift", "Shift of the candle close used for comparison", "General");

		_pauseTrading = Param(nameof(PauseTrading), false)
			.SetDisplay("Pause Trading", "Disable all trading actions", "General");

		_useInitialProtection = Param(nameof(UseInitialProtection), true)
			.SetDisplay("Use Initial Protection", "Apply initial stops/take profits after fills", "Protection");

		_buyStopLevel = Param(nameof(BuyStopLevel), 0m)
			.SetDisplay("Buy Stop Level", "Price that triggers a long entry when crossed upward", "Entries");

		_buyLimitLevel = Param(nameof(BuyLimitLevel), 0m)
			.SetDisplay("Buy Limit Level", "Price that triggers a long entry when crossed downward", "Entries");

		_sellStopLevel = Param(nameof(SellStopLevel), 0m)
			.SetDisplay("Sell Stop Level", "Price that triggers a short entry when crossed downward", "Entries");

		_sellLimitLevel = Param(nameof(SellLimitLevel), 0m)
			.SetDisplay("Sell Limit Level", "Price that triggers a short entry when crossed upward", "Entries");

		_longStopLevelParam = Param(nameof(LongStopLevel), 0m)
			.SetDisplay("Long Stop Level", "Stop line that closes the long position", "Exits");

		_longTakeProfitLevelParam = Param(nameof(LongTakeProfitLevel), 0m)
			.SetDisplay("Long Take Profit", "Take-profit line for long positions", "Exits");

		_shortStopLevelParam = Param(nameof(ShortStopLevel), 0m)
			.SetDisplay("Short Stop Level", "Stop line that closes the short position", "Exits");

		_shortTakeProfitLevelParam = Param(nameof(ShortTakeProfitLevel), 0m)
			.SetDisplay("Short Take Profit", "Take-profit line for short positions", "Exits");

		_allLongStopLevelParam = Param(nameof(AllLongStopLevel), 0m)
			.SetDisplay("All Long Stop", "Global stop line that closes every long", "Global Exits");

		_allLongTakeProfitLevelParam = Param(nameof(AllLongTakeProfitLevel), 0m)
			.SetDisplay("All Long Take Profit", "Global take-profit for long exposure", "Global Exits");

		_allShortStopLevelParam = Param(nameof(AllShortStopLevel), 0m)
			.SetDisplay("All Short Stop", "Global stop line that closes every short", "Global Exits");

		_allShortTakeProfitLevelParam = Param(nameof(AllShortTakeProfitLevel), 0m)
			.SetDisplay("All Short Take Profit", "Global take-profit for short exposure", "Global Exits");

		_initialLongStopLevelParam = Param(nameof(InitialLongStopLevel), 0m)
			.SetDisplay("Initial Long Stop", "Initial protective stop for new longs", "Protection");

		_initialLongTakeProfitLevelParam = Param(nameof(InitialLongTakeProfitLevel), 0m)
			.SetDisplay("Initial Long Take Profit", "Initial target for new longs", "Protection");

		_initialShortStopLevelParam = Param(nameof(InitialShortStopLevel), 0m)
			.SetDisplay("Initial Short Stop", "Initial protective stop for new shorts", "Protection");

		_initialShortTakeProfitLevelParam = Param(nameof(InitialShortTakeProfitLevel), 0m)
			.SetDisplay("Initial Short Take Profit", "Initial target for new shorts", "Protection");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for price comparisons", "General");
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
		_closeHistory.Clear();
		_longEntryPrice = 0m;
		_shortEntryPrice = 0m;
		_longStopLevel = 0m;
		_longTakeProfitLevel = 0m;
		_shortStopLevel = 0m;
		_shortTakeProfitLevel = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_closeHistory.Clear();
		_longEntryPrice = 0m;
		_shortEntryPrice = 0m;
		_longStopLevel = 0m;
		_longTakeProfitLevel = 0m;
		_shortStopLevel = 0m;
		_shortTakeProfitLevel = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Security != Security)
			return;

		if (Position > 0 && trade.Order.Side == Sides.Buy)
		{
			_longEntryPrice = trade.Trade.Price;
			_longStopLevel = UseInitialProtection ? InitialLongStopLevel : 0m;
			_longTakeProfitLevel = UseInitialProtection ? InitialLongTakeProfitLevel : 0m;
		}
		else if (Position < 0 && trade.Order.Side == Sides.Sell)
		{
			_shortEntryPrice = trade.Trade.Price;
			_shortStopLevel = UseInitialProtection ? InitialShortStopLevel : 0m;
			_shortTakeProfitLevel = UseInitialProtection ? InitialShortTakeProfitLevel : 0m;
		}
		else if (Position == 0)
		{
			if (trade.Order.Side == Sides.Sell)
			{
				_longEntryPrice = 0m;
				_longStopLevel = 0m;
				_longTakeProfitLevel = 0m;
			}
			else if (trade.Order.Side == Sides.Buy)
			{
				_shortEntryPrice = 0m;
				_shortStopLevel = 0m;
				_shortTakeProfitLevel = 0m;
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closeHistory.Add(candle.ClosePrice);

		var required = StartShift + 2;
		if (_closeHistory.Count < required)
			return;

		var currentIndex = _closeHistory.Count - 1 - StartShift;
		if (currentIndex <= 0)
			return;

		var currentClose = _closeHistory[currentIndex];
		var previousClose = _closeHistory[currentIndex - 1];

		var maxBuffer = Math.Max(required, 20);
		if (_closeHistory.Count > maxBuffer)
			_closeHistory.RemoveRange(0, _closeHistory.Count - maxBuffer);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (PauseTrading || Volume <= 0m)
			return;

		if (HandleExits(previousClose, currentClose))
			return;

		HandleEntries(previousClose, currentClose);
	}

	private bool HandleExits(decimal previousClose, decimal currentClose)
	{
		var longVolume = Position > 0 ? Math.Abs(Position) : 0m;
		if (longVolume > 0)
		{
			if (_longStopLevel > 0m && IsCrossDown(previousClose, currentClose, _longStopLevel))
			{
				SellMarket(longVolume);
				return true;
			}

			if (LongStopLevel > 0m && IsCrossDown(previousClose, currentClose, LongStopLevel))
			{
				SellMarket(longVolume);
				return true;
			}

			if (_longTakeProfitLevel > 0m && IsCrossUp(previousClose, currentClose, _longTakeProfitLevel))
			{
				SellMarket(longVolume);
				return true;
			}

			if (LongTakeProfitLevel > 0m && IsCrossUp(previousClose, currentClose, LongTakeProfitLevel))
			{
				SellMarket(longVolume);
				return true;
			}

			if (AllLongStopLevel > 0m && IsCrossDown(previousClose, currentClose, AllLongStopLevel))
			{
				SellMarket(longVolume);
				return true;
			}

			if (AllLongTakeProfitLevel > 0m && IsCrossUp(previousClose, currentClose, AllLongTakeProfitLevel))
			{
				SellMarket(longVolume);
				return true;
			}
		}

		var shortVolume = Position < 0 ? Math.Abs(Position) : 0m;
		if (shortVolume > 0)
		{
			if (_shortStopLevel > 0m && IsCrossUp(previousClose, currentClose, _shortStopLevel))
			{
				BuyMarket(shortVolume);
				return true;
			}

			if (ShortStopLevel > 0m && IsCrossUp(previousClose, currentClose, ShortStopLevel))
			{
				BuyMarket(shortVolume);
				return true;
			}

			if (_shortTakeProfitLevel > 0m && IsCrossDown(previousClose, currentClose, _shortTakeProfitLevel))
			{
				BuyMarket(shortVolume);
				return true;
			}

			if (ShortTakeProfitLevel > 0m && IsCrossDown(previousClose, currentClose, ShortTakeProfitLevel))
			{
				BuyMarket(shortVolume);
				return true;
			}

			if (AllShortStopLevel > 0m && IsCrossUp(previousClose, currentClose, AllShortStopLevel))
			{
				BuyMarket(shortVolume);
				return true;
			}

			if (AllShortTakeProfitLevel > 0m && IsCrossDown(previousClose, currentClose, AllShortTakeProfitLevel))
			{
				BuyMarket(shortVolume);
				return true;
			}
		}

		return false;
	}

	private void HandleEntries(decimal previousClose, decimal currentClose)
	{
		var longVolume = Volume + (Position < 0 ? Math.Abs(Position) : 0m);
		if (BuyStopLevel > 0m && IsCrossUp(previousClose, currentClose, BuyStopLevel))
		{
			BuyMarket(longVolume);
			return;
		}

		if (BuyLimitLevel > 0m && IsCrossDown(previousClose, currentClose, BuyLimitLevel))
		{
			BuyMarket(longVolume);
			return;
		}

		var shortVolume = Volume + (Position > 0 ? Math.Abs(Position) : 0m);
		if (SellStopLevel > 0m && IsCrossDown(previousClose, currentClose, SellStopLevel))
		{
			SellMarket(shortVolume);
			return;
		}

		if (SellLimitLevel > 0m && IsCrossUp(previousClose, currentClose, SellLimitLevel))
		{
			SellMarket(shortVolume);
		}
	}

	private bool IsCrossUp(decimal previousClose, decimal currentClose, decimal level)
	{
		if (level <= 0m)
			return false;

		return CrossMethod switch
		{
			0 => previousClose < level && currentClose > level,
			_ => previousClose <= level && currentClose > level,
		};
	}

	private bool IsCrossDown(decimal previousClose, decimal currentClose, decimal level)
	{
		if (level <= 0m)
			return false;

		return CrossMethod switch
		{
			0 => previousClose > level && currentClose < level,
			_ => previousClose >= level && currentClose < level,
		};
	}
}
