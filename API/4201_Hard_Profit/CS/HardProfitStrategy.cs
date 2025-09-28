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
/// Port of the MetaTrader expert advisor HardProfit.
/// Implements the breakout, trend filter, dynamic money management, and staged exit logic with StockSharp high-level API.
/// </summary>
public class HardProfitStrategy : Strategy
{
	private readonly StrategyParam<int> _breakoutPeriod;
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<bool> _onlyShort;
	private readonly StrategyParam<bool> _onlyLong;
	private readonly StrategyParam<int> _maxTradesPerBar;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _breakEvenPips;
	private readonly StrategyParam<decimal> _trailingActivationPips;
	private readonly StrategyParam<decimal> _partialTakeProfit1Pips;
	private readonly StrategyParam<decimal> _partialRatio1;
	private readonly StrategyParam<decimal> _partialTakeProfit2Pips;
	private readonly StrategyParam<decimal> _partialRatio2;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _maxSpreadPips;
	private readonly StrategyParam<MoneyManagementModes> _moneyManagementMode;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _geometricalFactor;
	private readonly StrategyParam<decimal> _proportionalRiskPercent;
	private readonly StrategyParam<int> _lastTradesCount;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<decimal> _tssfTrigger1;
	private readonly StrategyParam<decimal> _tssfRatio1;
	private readonly StrategyParam<decimal> _tssfTrigger2;
	private readonly StrategyParam<decimal> _tssfRatio2;
	private readonly StrategyParam<decimal> _tssfTrigger3;
	private readonly StrategyParam<decimal> _tssfRatio3;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _trendAverage = null!;
	private Highest _breakoutHigh = null!;
	private Lowest _breakoutLow = null!;

	private readonly LinkedList<decimal> _recentTradePnL = new();

	private decimal _previousTrendValue;
	private bool _hasPreviousTrend;
	private decimal _previousHighValue;
	private decimal _previousLowValue;
	private bool _hasPreviousRange;

	private decimal _pipSize;

	private DateTimeOffset _lastTradeBarTime;
	private DateTimeOffset _currentBarTime;
	private int _tradesThisBar;

	private decimal _previousPosition;
	private decimal _entryPrice;
	private bool _breakEvenArmed;
	private bool _advancedStopArmed;
	private int _partialStage;
	private decimal _lastRealizedPnL;

	private decimal _lastClosePrice;

	/// <summary>
	/// Money management behaviour replicated from the original expert advisor.
	/// </summary>
	public enum MoneyManagementModes
	{
		Fixed = 1,
		Geometrical = 2,
		Proportional = 3,
		Smart = 4,
		Tssf = 5,
	}

	/// <summary>
	/// Initializes a new instance of <see cref="HardProfitStrategy"/>.
	/// </summary>
	public HardProfitStrategy()
	{
		_breakoutPeriod = Param(nameof(BreakoutPeriod), 1)
			.SetRange(1, 200)
			.SetCanOptimize(true)
			.SetDisplay("Breakout Period", "Number of finished candles used to compute the breakout range", "Signals");

		_trendPeriod = Param(nameof(TrendPeriod), 3)
			.SetRange(1, 200)
			.SetCanOptimize(true)
			.SetDisplay("Trend Period", "Length of the smoothed moving average applied to median price", "Signals");

		_onlyShort = Param(nameof(OnlyShort), false)
			.SetDisplay("Only Short", "Enable to restrict trading to short entries", "Filters");

		_onlyLong = Param(nameof(OnlyLong), false)
			.SetDisplay("Only Long", "Enable to restrict trading to long entries", "Filters");

		_maxTradesPerBar = Param(nameof(MaxTradesPerBar), 1)
			.SetRange(0, 10)
			.SetDisplay("Max Trades Per Bar", "Maximum number of entries allowed within the same candle", "Filters");

		_stopLossPips = Param(nameof(StopLossPips), 40m)
			.SetRange(0m, 5000m)
			.SetCanOptimize(true)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk");

		_breakEvenPips = Param(nameof(BreakEvenPips), 30m)
			.SetRange(0m, 5000m)
			.SetCanOptimize(true)
			.SetDisplay("Break-even (pips)", "Profit distance that arms the break-even stop", "Risk");

		_trailingActivationPips = Param(nameof(TrailingActivationPips), 330m)
			.SetRange(0m, 10000m)
			.SetDisplay("Trailing Activation (pips)", "Profit distance that moves the stop into profit by the original stop size", "Risk");

		_partialTakeProfit1Pips = Param(nameof(PartialTakeProfit1Pips), 550m)
			.SetRange(0m, 20000m)
			.SetDisplay("Partial TP1 (pips)", "Distance for the first partial take profit", "Exits");

		_partialRatio1 = Param(nameof(PartialRatio1), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("Partial Ratio 1 (%)", "Percentage of the current position to close at the first partial", "Exits");

		_partialTakeProfit2Pips = Param(nameof(PartialTakeProfit2Pips), 1100m)
			.SetRange(0m, 20000m)
			.SetDisplay("Partial TP2 (pips)", "Distance for the second partial take profit", "Exits");

		_partialRatio2 = Param(nameof(PartialRatio2), 70m)
			.SetRange(0m, 100m)
			.SetDisplay("Partial Ratio 2 (%)", "Percentage of the remaining position to close at the second partial", "Exits");

		_takeProfitPips = Param(nameof(TakeProfitPips), 1500m)
			.SetRange(0m, 30000m)
			.SetDisplay("Take Profit (pips)", "Final take profit distance; set to zero to disable", "Exits");

		_maxSpreadPips = Param(nameof(MaxSpreadPips), 22m)
			.SetRange(0m, 200m)
			.SetDisplay("Max Spread (pips)", "Maximum allowed spread before blocking new entries", "Filters");

		_moneyManagementMode = Param(nameof(ManagementMode), MoneyManagementModes.Proportional)
			.SetDisplay("Money Management", "Volume sizing mode derived from the original EA", "Money Management");

		_fixedVolume = Param(nameof(FixedVolume), 0.1m)
			.SetRange(0m, 100m)
			.SetDisplay("Fixed Volume", "Volume used when the money management mode is Fixed", "Money Management");

		_geometricalFactor = Param(nameof(GeometricalFactor), 3m)
			.SetRange(0m, 100m)
			.SetDisplay("Geometrical Factor", "Multiplier applied in the geometrical sizing formula", "Money Management");

		_proportionalRiskPercent = Param(nameof(ProportionalRiskPercent), 90m)
			.SetRange(0m, 500m)
			.SetDisplay("Risk Percent", "Risk percentage used by proportional, smart, and TSSF modes", "Money Management");

		_lastTradesCount = Param(nameof(LastTradesCount), 10)
			.SetRange(1, 200)
			.SetDisplay("Last Trades", "Number of closed trades considered by adaptive money management", "Money Management");

		_decreaseFactor = Param(nameof(DecreaseFactor), 2m)
			.SetRange(0.1m, 100m)
			.SetDisplay("Decrease Factor", "Divider applied when multiple consecutive losses are detected", "Money Management");

		_tssfTrigger1 = Param(nameof(TssfTrigger1), 1m)
			.SetRange(0m, 100m)
			.SetDisplay("TSSF Trigger 1", "First threshold of the TSSF sizing formula", "Money Management");

		_tssfRatio1 = Param(nameof(TssfRatio1), 50m)
			.SetRange(1m, 1000m)
			.SetDisplay("TSSF Ratio 1", "Risk divisor when TSSF metric sits between trigger 1 and 2", "Money Management");

		_tssfTrigger2 = Param(nameof(TssfTrigger2), 2m)
			.SetRange(0m, 100m)
			.SetDisplay("TSSF Trigger 2", "Second threshold of the TSSF sizing formula", "Money Management");

		_tssfRatio2 = Param(nameof(TssfRatio2), 75m)
			.SetRange(1m, 1000m)
			.SetDisplay("TSSF Ratio 2", "Risk divisor when TSSF metric sits between trigger 2 and 3", "Money Management");

		_tssfTrigger3 = Param(nameof(TssfTrigger3), 3m)
			.SetRange(0m, 100m)
			.SetDisplay("TSSF Trigger 3", "Third threshold of the TSSF sizing formula", "Money Management");

		_tssfRatio3 = Param(nameof(TssfRatio3), 100m)
			.SetRange(1m, 1000m)
			.SetDisplay("TSSF Ratio 3", "Risk divisor when the TSSF metric sits above trigger 3", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe driving signal generation", "General");
	}

	/// <summary>
	/// Number of candles used to build the breakout range (previous highs and lows).
	/// </summary>
	public int BreakoutPeriod
	{
		get => _breakoutPeriod.Value;
		set => _breakoutPeriod.Value = value;
	}

	/// <summary>
	/// Length of the smoothed moving average that defines the trend bias.
	/// </summary>
	public int TrendPeriod
	{
		get => _trendPeriod.Value;
		set => _trendPeriod.Value = value;
	}

	/// <summary>
	/// Restricts trading to short entries when enabled.
	/// </summary>
	public bool OnlyShort
	{
		get => _onlyShort.Value;
		set => _onlyShort.Value = value;
	}

	/// <summary>
	/// Restricts trading to long entries when enabled.
	/// </summary>
	public bool OnlyLong
	{
		get => _onlyLong.Value;
		set => _onlyLong.Value = value;
	}

	/// <summary>
	/// Maximum amount of entries allowed within the same candle (0 disables the limit).
	/// </summary>
	public int MaxTradesPerBar
	{
		get => _maxTradesPerBar.Value;
		set => _maxTradesPerBar.Value = value;
	}

	/// <summary>
	/// Protective stop distance in pips. Zero disables the stop.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Profit distance in pips that arms the break-even stop.
	/// </summary>
	public decimal BreakEvenPips
	{
		get => _breakEvenPips.Value;
		set => _breakEvenPips.Value = value;
	}

	/// <summary>
	/// Profit distance that upgrades the stop into positive territory by one stop size.
	/// </summary>
	public decimal TrailingActivationPips
	{
		get => _trailingActivationPips.Value;
		set => _trailingActivationPips.Value = value;
	}

	/// <summary>
	/// Distance in pips required to trigger the first partial take profit.
	/// </summary>
	public decimal PartialTakeProfit1Pips
	{
		get => _partialTakeProfit1Pips.Value;
		set => _partialTakeProfit1Pips.Value = value;
	}

	/// <summary>
	/// Percentage of the current position closed at the first partial take profit.
	/// </summary>
	public decimal PartialRatio1
	{
		get => _partialRatio1.Value;
		set => _partialRatio1.Value = value;
	}

	/// <summary>
	/// Distance in pips required to trigger the second partial take profit.
	/// </summary>
	public decimal PartialTakeProfit2Pips
	{
		get => _partialTakeProfit2Pips.Value;
		set => _partialTakeProfit2Pips.Value = value;
	}

	/// <summary>
	/// Percentage of the remaining position closed at the second partial take profit.
	/// </summary>
	public decimal PartialRatio2
	{
		get => _partialRatio2.Value;
		set => _partialRatio2.Value = value;
	}

	/// <summary>
	/// Final take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread in pips when submitting new entries.
	/// </summary>
	public decimal MaxSpreadPips
	{
		get => _maxSpreadPips.Value;
		set => _maxSpreadPips.Value = value;
	}

	/// <summary>
	/// Money management behaviour used to size new positions.
	/// </summary>
	public MoneyManagementModes ManagementMode
	{
		get => _moneyManagementMode.Value;
		set => _moneyManagementMode.Value = value;
	}

	/// <summary>
	/// Volume used by the fixed money management mode.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = value;
	}

	/// <summary>
	/// Multiplier applied by the geometrical money management formula.
	/// </summary>
	public decimal GeometricalFactor
	{
		get => _geometricalFactor.Value;
		set => _geometricalFactor.Value = value;
	}

	/// <summary>
	/// Risk percentage used by proportional, smart, and TSSF sizing.
	/// </summary>
	public decimal ProportionalRiskPercent
	{
		get => _proportionalRiskPercent.Value;
		set => _proportionalRiskPercent.Value = value;
	}

	/// <summary>
	/// Number of recent trades used by adaptive money management.
	/// </summary>
	public int LastTradesCount
	{
		get => _lastTradesCount.Value;
		set => _lastTradesCount.Value = value;
	}

	/// <summary>
	/// Divider applied when multiple consecutive losses are detected.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// First TSSF trigger threshold.
	/// </summary>
	public decimal TssfTrigger1
	{
		get => _tssfTrigger1.Value;
		set => _tssfTrigger1.Value = value;
	}

	/// <summary>
	/// Risk divisor used when the TSSF metric sits between trigger 1 and 2.
	/// </summary>
	public decimal TssfRatio1
	{
		get => _tssfRatio1.Value;
		set => _tssfRatio1.Value = value;
	}

	/// <summary>
	/// Second TSSF trigger threshold.
	/// </summary>
	public decimal TssfTrigger2
	{
		get => _tssfTrigger2.Value;
		set => _tssfTrigger2.Value = value;
	}

	/// <summary>
	/// Risk divisor used when the TSSF metric sits between trigger 2 and 3.
	/// </summary>
	public decimal TssfRatio2
	{
		get => _tssfRatio2.Value;
		set => _tssfRatio2.Value = value;
	}

	/// <summary>
	/// Third TSSF trigger threshold.
	/// </summary>
	public decimal TssfTrigger3
	{
		get => _tssfTrigger3.Value;
		set => _tssfTrigger3.Value = value;
	}

	/// <summary>
	/// Risk divisor used when the TSSF metric is above trigger 3.
	/// </summary>
	public decimal TssfRatio3
	{
		get => _tssfRatio3.Value;
		set => _tssfRatio3.Value = value;
	}

	/// <summary>
	/// Candle type that drives calculations and signal evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_recentTradePnL.Clear();

		_previousTrendValue = 0m;
		_hasPreviousTrend = false;
		_previousHighValue = 0m;
		_previousLowValue = 0m;
		_hasPreviousRange = false;

		_pipSize = 0m;

		_lastTradeBarTime = default;
		_currentBarTime = default;
		_tradesThisBar = 0;

		_previousPosition = 0m;
		_entryPrice = 0m;
		_breakEvenArmed = false;
		_advancedStopArmed = false;
		_partialStage = 0;
		_lastRealizedPnL = 0m;

		_lastClosePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_trendAverage = new SmoothedMovingAverage { Length = Math.Max(1, TrendPeriod) };
		_breakoutHigh = new Highest { Length = Math.Max(1, BreakoutPeriod) };
		_breakoutLow = new Lowest { Length = Math.Max(1, BreakoutPeriod) };

		_pipSize = GetPipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		var absPosition = Math.Abs(Position);
		var prevAbsPosition = Math.Abs(_previousPosition);

		if (prevAbsPosition == 0m && absPosition > 0m)
		{
			_entryPrice = PositionPrice ?? _lastClosePrice;
			_breakEvenArmed = false;
			_advancedStopArmed = false;
			_partialStage = 0;
			_lastRealizedPnL = PnL;
			_tradesThisBar++;
			_lastTradeBarTime = _currentBarTime;
		}
		else if (absPosition < prevAbsPosition)
		{
			var realizedDelta = PnL - _lastRealizedPnL;
			if (realizedDelta != 0m)
			{
				RecordTradePnL(realizedDelta);
				_lastRealizedPnL = PnL;
			}

			if (absPosition == 0m)
			{
				_entryPrice = 0m;
				_breakEvenArmed = false;
				_advancedStopArmed = false;
				_partialStage = 0;
			}
		}
		else if (absPosition > 0m)
		{
			_entryPrice = PositionPrice ?? _entryPrice;
		}
		else
		{
			_lastRealizedPnL = PnL;
		}

		_previousPosition = Position;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_currentBarTime = candle.OpenTime;

		if (_currentBarTime != _lastTradeBarTime)
			_tradesThisBar = 0;

		_lastClosePrice = candle.ClosePrice;

		ManageOpenPosition(candle);

		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2m;
		var trendValue = _trendAverage.Process(medianPrice, candle.OpenTime, true).ToDecimal();

		var highestValue = _breakoutHigh.Process(candle.HighPrice, candle.OpenTime, true).ToDecimal();
		var lowestValue = _breakoutLow.Process(candle.LowPrice, candle.OpenTime, true).ToDecimal();

		if (!_hasPreviousRange)
		{
			_previousHighValue = highestValue;
			_previousLowValue = lowestValue;
			_hasPreviousRange = true;
			_previousTrendValue = trendValue;
			_hasPreviousTrend = true;
			return;
		}

		var signalTrend = _hasPreviousTrend ? trendValue - _previousTrendValue : 0m;

		_previousTrendValue = trendValue;
		_previousHighValue = highestValue;
		_previousLowValue = lowestValue;
		_hasPreviousTrend = true;

		if (!_trendAverage.IsFormed || !_breakoutHigh.IsFormed || !_breakoutLow.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m)
			return;

		var reachedMaxTrades = MaxTradesPerBar > 0 && _lastTradeBarTime == _currentBarTime && _tradesThisBar >= MaxTradesPerBar;
		if (reachedMaxTrades)
			return;

		var spreadLimit = MaxSpreadPips > 0m ? MaxSpreadPips * _pipSize : 0m;
		if (spreadLimit > 0m)
		{
			var ask = Security?.BestAsk?.Price;
			var bid = Security?.BestBid?.Price;
			if (ask.HasValue && bid.HasValue)
			{
				var spread = ask.Value - bid.Value;
				if (spread > spreadLimit)
					return;
			}
		}

		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var breakoutHigh = _previousHighValue;
		var breakoutLow = _previousLowValue;

		if (!OnlyShort && close == high && close > breakoutHigh && signalTrend > 0m)
		{
			var volume = CalculateEntryVolume(close);
			if (volume > 0m)
				BuyMarket(volume);
		}
		else if (!OnlyLong && close == low && close < breakoutLow && signalTrend < 0m)
		{
			var volume = CalculateEntryVolume(close);
			if (volume > 0m)
				SellMarket(volume);
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		var position = Position;
		if (position == 0m || _pipSize <= 0m)
			return;

		var entryPrice = PositionPrice ?? _entryPrice;
		if (entryPrice <= 0m)
			return;

		var absPos = Math.Abs(position);
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		if (position > 0m)
		{
			var profitPips = (close - entryPrice) / _pipSize;

			if (!_breakEvenArmed && BreakEvenPips > 0m && profitPips >= BreakEvenPips)
				_breakEvenArmed = true;

			if (!_advancedStopArmed && TrailingActivationPips > 0m && profitPips >= TrailingActivationPips && StopLossPips > 0m)
				_advancedStopArmed = true;

			if (_partialStage == 0 && PartialTakeProfit1Pips > 0m)
			{
				var distance = (high - entryPrice) / _pipSize;
				if (distance >= PartialTakeProfit1Pips)
				{
					if (ClosePartial(absPos, PartialRatio1, Sides.Sell))
					{
						_partialStage = 1;
						return;
					}
				}
			}

			if (_partialStage == 1 && PartialTakeProfit2Pips > 0m)
			{
				var distance = (high - entryPrice) / _pipSize;
				if (distance >= PartialTakeProfit2Pips)
				{
					if (ClosePartial(Math.Abs(Position), PartialRatio2, Sides.Sell))
					{
						_partialStage = 2;
						return;
					}
				}
			}

			var stopPrice = 0m;
			if (StopLossPips > 0m)
			{
				if (_advancedStopArmed)
					stopPrice = entryPrice + StopLossPips * _pipSize;
				else if (_breakEvenArmed)
					stopPrice = entryPrice;
				else
					stopPrice = entryPrice - StopLossPips * _pipSize;

				stopPrice = NormalizePrice(stopPrice);

				if (stopPrice > 0m && low <= stopPrice)
				{
					SellMarket(position);
					return;
				}
			}

			if (TakeProfitPips > 0m)
			{
				var targetPrice = NormalizePrice(entryPrice + TakeProfitPips * _pipSize);
				if (targetPrice > 0m && high >= targetPrice)
				{
					SellMarket(position);
					return;
				}
			}
		}
		else if (position < 0m)
		{
			var profitPips = (entryPrice - close) / _pipSize;

			if (!_breakEvenArmed && BreakEvenPips > 0m && profitPips >= BreakEvenPips)
				_breakEvenArmed = true;

			if (!_advancedStopArmed && TrailingActivationPips > 0m && profitPips >= TrailingActivationPips && StopLossPips > 0m)
				_advancedStopArmed = true;

			if (_partialStage == 0 && PartialTakeProfit1Pips > 0m)
			{
				var distance = (entryPrice - low) / _pipSize;
				if (distance >= PartialTakeProfit1Pips)
				{
					if (ClosePartial(absPos, PartialRatio1, Sides.Buy))
					{
						_partialStage = 1;
						return;
					}
				}
			}

			if (_partialStage == 1 && PartialTakeProfit2Pips > 0m)
			{
				var distance = (entryPrice - low) / _pipSize;
				if (distance >= PartialTakeProfit2Pips)
				{
					if (ClosePartial(Math.Abs(Position), PartialRatio2, Sides.Buy))
					{
						_partialStage = 2;
						return;
					}
				}
			}

			var stopPrice = 0m;
			if (StopLossPips > 0m)
			{
				if (_advancedStopArmed)
					stopPrice = entryPrice - StopLossPips * _pipSize;
				else if (_breakEvenArmed)
					stopPrice = entryPrice;
				else
					stopPrice = entryPrice + StopLossPips * _pipSize;

				stopPrice = NormalizePrice(stopPrice);

				if (stopPrice > 0m && high >= stopPrice)
				{
					BuyMarket(-position);
					return;
				}
			}

			if (TakeProfitPips > 0m)
			{
				var targetPrice = NormalizePrice(entryPrice - TakeProfitPips * _pipSize);
				if (targetPrice > 0m && low <= targetPrice)
				{
					BuyMarket(-position);
					return;
				}
			}
		}
	}

	private bool ClosePartial(decimal absPosition, decimal ratioPercent, Sides side)
	{
		if (absPosition <= 0m || ratioPercent <= 0m)
			return false;

		var volume = absPosition * ratioPercent / 100m;
		volume = AdjustVolume(volume);

		var currentAbs = Math.Abs(Position);
		if (volume <= 0m || volume > currentAbs)
			return false;

		if (side == Sides.Sell)
			SellMarket(volume);
		else
			BuyMarket(volume);

		return true;
	}

	private decimal CalculateEntryVolume(decimal closePrice)
	{
		var portfolio = Portfolio;
		var balance = portfolio?.CurrentValue ?? portfolio?.BeginValue ?? 0m;
		var freeMargin = portfolio?.CurrentValue ?? balance;

		decimal volume = FixedVolume;
		var riskFraction = ProportionalRiskPercent / 100m;

		switch (ManagementMode)
		{
			case MoneyManagementModes.Fixed:
				volume = FixedVolume;
				break;
			case MoneyManagementModes.Geometrical:
			{
				if (balance > 0m)
				{
					var ratio = Math.Sqrt((double)(balance / 1000m));
					volume = 0.1m * (decimal)ratio * GeometricalFactor;
				}
				break;
			}
			case MoneyManagementModes.Proportional:
			{
				if (closePrice > 0m)
					volume = freeMargin * riskFraction / (closePrice * 1000m);
				break;
			}
			case MoneyManagementModes.Smart:
			{
				volume = freeMargin * riskFraction / 100m;
				var losses = CountRecentConsecutiveLosses();
				if (losses > 1 && DecreaseFactor > 0m)
				{
					var reduction = volume * losses / DecreaseFactor;
					volume -= reduction;
				}
				break;
			}
			case MoneyManagementModes.Tssf:
			{
				volume = CalculateTssfVolume(freeMargin, FixedVolume);
				break;
			}
		}

		if (volume <= 0m)
			volume = FixedVolume;

		return AdjustVolume(volume);
	}

	private decimal CalculateTssfVolume(decimal freeMargin, decimal fallback)
	{
		if (LastTradesCount <= 0 || _recentTradePnL.Count == 0)
			return fallback;

		var totalTrades = 0;
		var winCount = 0;
		var lossCount = 0;
		decimal winSum = 0m;
		decimal lossSum = 0m;

		for (var node = _recentTradePnL.First; node != null; node = node.Next)
		{
			var pnl = node.Value;
			totalTrades++;

			if (pnl >= 0m)
			{
				winCount++;
				winSum += pnl;
			}
			else
			{
				lossCount++;
				lossSum += Math.Abs(pnl);
			}
		}

		if (totalTrades < Math.Min(LastTradesCount, 2) || winCount == 0 || lossCount == 0)
			return fallback;

		var avgWin = winSum / winCount;
		var avgLoss = lossCount > 0 ? lossSum / lossCount : 0m;
		var winRate = totalTrades > 0 ? (decimal)winCount / totalTrades : 0m;

		if (avgLoss <= 0m || winRate <= 0.1m || winRate >= 1m)
			return fallback;

		var tssf = (avgWin / avgLoss) * ((1.1m - winRate) / (winRate - 0.1m) + 1m);

		var riskBase = freeMargin * (ProportionalRiskPercent / 100m);

		if (tssf <= TssfTrigger1)
			return AdjustVolume(0.1m);

		if (tssf <= TssfTrigger2)
			return AdjustVolume(riskBase / Math.Max(TssfRatio1, 1m));

		if (tssf <= TssfTrigger3)
			return AdjustVolume(riskBase / Math.Max(TssfRatio2, 1m));

		return AdjustVolume(riskBase / Math.Max(TssfRatio3, 1m));
	}

	private int CountRecentConsecutiveLosses()
	{
		if (_recentTradePnL.Count == 0)
			return 0;

		var losses = 0;
		for (var node = _recentTradePnL.Last; node != null; node = node.Previous)
		{
			if (node.Value >= 0m)
				break;

			losses++;
		}

		return losses;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security != null)
		{
			var step = security.VolumeStep ?? 0m;
			if (step > 0m)
			{
				var steps = Math.Floor(volume / step);
				volume = steps * step;
			}

			var minVolume = security.MinVolume ?? 0m;
			if (minVolume > 0m && volume < minVolume)
				volume = minVolume;

			var maxVolume = security.MaxVolume;
			if (maxVolume.HasValue && maxVolume.Value > 0m && volume > maxVolume.Value)
				volume = maxVolume.Value;
		}

		return volume;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		if (security == null)
			return price;

		var normalized = security.ShrinkPrice(price);
		return normalized > 0m ? normalized : price;
	}

	private decimal GetPipSize()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? 0.0001m;
		var decimals = security.Decimals ?? 0;

		if (decimals >= 3)
			return step * 10m;

		return step > 0m ? step : 0.0001m;
	}

	private void RecordTradePnL(decimal pnl)
	{
		if (LastTradesCount <= 0)
			return;

		_recentTradePnL.AddLast(pnl);
		while (_recentTradePnL.Count > LastTradesCount)
			_recentTradePnL.RemoveFirst();
	}
}
