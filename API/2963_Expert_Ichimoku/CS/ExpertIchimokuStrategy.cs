using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku-based trend strategy converted from the original Expert Advisor.
/// Combines Tenkan/Kijun crosses with Chikou breakouts and optional martingale sizing.
/// </summary>
public class ExpertIchimokuStrategy : Strategy
{
	private const int HistoryCapacity = 64;

	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<decimal> _stopLossOffset;
	private readonly StrategyParam<decimal> _takeProfitOffset;
	private readonly StrategyParam<decimal> _trailingStopOffset;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<DataType> _candleType;

	private Ichimoku _ichimoku;

	private readonly List<decimal> _tenkanHistory = new();
	private readonly List<decimal> _kijunHistory = new();
	private readonly List<decimal> _senkouAHistory = new();
	private readonly List<decimal> _senkouBHistory = new();
	private readonly List<decimal> _chinkouHistory = new();
	private readonly List<decimal> _openHistory = new();
	private readonly List<decimal> _closeHistory = new();

	private decimal _entryPrice;
	private decimal _entryVolume;
	private int _entryDirection;
	private bool _lastTradeWasLoss;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Tenkan-sen period.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun-sen period.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Senkou Span B period.
	/// </summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss offset in price units.
	/// </summary>
	public decimal StopLossOffset
	{
		get => _stopLossOffset.Value;
		set => _stopLossOffset.Value = value;
	}

	/// <summary>
	/// Take-profit offset in price units.
	/// </summary>
	public decimal TakeProfitOffset
	{
		get => _takeProfitOffset.Value;
		set => _takeProfitOffset.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price units.
	/// </summary>
	public decimal TrailingStopOffset
	{
		get => _trailingStopOffset.Value;
		set => _trailingStopOffset.Value = value;
	}

	/// <summary>
	/// Minimum price advance required to tighten the trailing stop.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Maximum position multiplier used to cap martingale sizing.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Enable doubling volume after a losing trade.
	/// </summary>
	public bool UseMartingaleAfterLoss
	{
		get => _useMartingale.Value;
		set => _useMartingale.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ExpertIchimokuStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Tenkan-sen length for Ichimoku", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Kijun-sen length for Ichimoku", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span B Period", "Senkou Span B length for Ichimoku", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(40, 70, 2);

		_stopLossOffset = Param(nameof(StopLossOffset), 0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Offset", "Absolute stop-loss distance", "Risk Management");

		_takeProfitOffset = Param(nameof(TakeProfitOffset), 0m)
			.SetNotNegative()
			.SetDisplay("Take Profit Offset", "Absolute take-profit distance", "Risk Management");

		_trailingStopOffset = Param(nameof(TrailingStopOffset), 0m)
			.SetNotNegative()
			.SetDisplay("Trailing Stop Offset", "Base trailing stop distance", "Risk Management");

		_trailingStep = Param(nameof(TrailingStep), 0m)
			.SetNotNegative()
			.SetDisplay("Trailing Step", "Minimum move to tighten trailing stop", "Risk Management");

		_maxPositions = Param(nameof(MaxPositions), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Position Multiplier", "Maximum net volume multiplier", "Money Management");

		_useMartingale = Param(nameof(UseMartingaleAfterLoss), true)
			.SetDisplay("Use Martingale", "Double volume after a losing trade", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "General");
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

		_tenkanHistory.Clear();
		_kijunHistory.Clear();
		_senkouAHistory.Clear();
		_senkouBHistory.Clear();
		_chinkouHistory.Clear();
		_openHistory.Clear();
		_closeHistory.Clear();

		_entryPrice = 0;
		_entryVolume = 0;
		_entryDirection = 0;
		_lastTradeWasLoss = false;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ichimoku, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ichimoku);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ichimoku = (IchimokuValue)ichimokuValue;

		if (ichimoku.Tenkan is not decimal tenkan ||
			ichimoku.Kijun is not decimal kijun ||
			ichimoku.SenkouA is not decimal senkouA ||
			ichimoku.SenkouB is not decimal senkouB ||
			ichimoku.Chinkou is not decimal chinkou)
			return;

		AddToHistory(_tenkanHistory, tenkan);
		AddToHistory(_kijunHistory, kijun);
		AddToHistory(_senkouAHistory, senkouA);
		AddToHistory(_senkouBHistory, senkouB);
		AddToHistory(_chinkouHistory, chinkou);
		AddToHistory(_openHistory, candle.OpenPrice);
		AddToHistory(_closeHistory, candle.ClosePrice);

		if (CheckRiskManagement(candle))
			return;

		if (_tenkanHistory.Count < 2 || _kijunHistory.Count < 2 || _senkouAHistory.Count < 2 || _senkouBHistory.Count < 2 ||
			_chinkouHistory.Count < 2 || _openHistory.Count < 12 || _closeHistory.Count < 12)
			return;

		var tenCurrent = GetHistoryValue(_tenkanHistory, 0);
		var kijCurrent = GetHistoryValue(_kijunHistory, 0);
		var tenPrev = GetHistoryValue(_tenkanHistory, 1);
		var kijPrev = GetHistoryValue(_kijunHistory, 1);
		var spanAPrev = GetHistoryValue(_senkouAHistory, 1);
		var spanBPrev = GetHistoryValue(_senkouBHistory, 1);
		var chinkouCurrent = GetHistoryValue(_chinkouHistory, 0);
		var chinkouPrev = GetHistoryValue(_chinkouHistory, 1);
		var closePrev = GetHistoryValue(_closeHistory, 0);
		var openPrev = GetHistoryValue(_openHistory, 0);
		var closeTenAgo = GetHistoryValue(_closeHistory, 9);
		var closeElevenAgo = GetHistoryValue(_closeHistory, 10);
		var openTenAgo = GetHistoryValue(_openHistory, 9);
		var openElevenAgo = GetHistoryValue(_openHistory, 10);

		var upperCloud = Math.Max(spanAPrev, spanBPrev);
		var lowerCloud = Math.Min(spanAPrev, spanBPrev);
		var bullishPrevCandle = closePrev > openPrev;
		var bearishPrevCandle = closePrev < openPrev;

		var bullishCross = tenPrev <= kijPrev && tenCurrent > kijCurrent;
		var bearishCross = tenPrev >= kijPrev && tenCurrent < kijCurrent;
		var chinkouBreakout = chinkouPrev <= closeElevenAgo && chinkouCurrent > closeTenAgo;
		var chinkouBreakdown = chinkouPrev >= openElevenAgo && chinkouCurrent < openTenAgo;

		var priceAboveCloud = candle.ClosePrice > upperCloud;
		var priceBelowCloud = candle.ClosePrice < lowerCloud;

		if ((bullishCross || chinkouBreakout) && priceAboveCloud && bullishPrevCandle)
		{
			if (Position < 0)
			{
				ExitShort(candle, candle.ClosePrice, "reverse to long");
				if (Position != 0)
					return;
			}

			var volume = CalculateEntryVolume();
			if (volume <= 0)
				return;

			BuyMarket(volume);
			InitializePosition(candle.ClosePrice, volume, 1);
			LogInfo($"Enter long at {candle.ClosePrice}, volume {volume}.");
			return;
		}

		if ((bearishCross || chinkouBreakdown) && priceBelowCloud && bearishPrevCandle)
		{
			if (Position > 0)
			{
				ExitLong(candle, candle.ClosePrice, "reverse to short");
				if (Position != 0)
					return;
			}

			var volume = CalculateEntryVolume();
			if (volume <= 0)
				return;

			SellMarket(volume);
			InitializePosition(candle.ClosePrice, volume, -1);
			LogInfo($"Enter short at {candle.ClosePrice}, volume {volume}.");
		}
	}

	private bool CheckRiskManagement(ICandleMessage candle)
	{
		if (Position == 0 || _entryDirection == 0)
			return false;

		UpdateTrailing(candle);

		var closePrice = candle.ClosePrice;

		if (Position > 0)
		{
			if (_stopPrice.HasValue && closePrice <= _stopPrice.Value)
			{
				ExitLong(candle, closePrice, "stop-loss");
				return true;
			}

			if (_takeProfitPrice.HasValue && closePrice >= _takeProfitPrice.Value)
			{
				ExitLong(candle, closePrice, "take-profit");
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice.HasValue && closePrice >= _stopPrice.Value)
			{
				ExitShort(candle, closePrice, "stop-loss");
				return true;
			}

			if (_takeProfitPrice.HasValue && closePrice <= _takeProfitPrice.Value)
			{
				ExitShort(candle, closePrice, "take-profit");
				return true;
			}
		}

		return false;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopOffset <= 0 || TrailingStep <= 0)
			return;

		if (_entryDirection > 0)
		{
			var move = candle.ClosePrice - _entryPrice;
			if (move <= TrailingStopOffset + TrailingStep)
				return;

			var candidateStop = candle.ClosePrice - TrailingStopOffset;
			var threshold = candle.ClosePrice - (TrailingStopOffset + TrailingStep);

			if (!_stopPrice.HasValue || _stopPrice.Value < threshold)
				_stopPrice = _stopPrice.HasValue ? Math.Max(_stopPrice.Value, candidateStop) : candidateStop;
		}
		else if (_entryDirection < 0)
		{
			var move = _entryPrice - candle.ClosePrice;
			if (move <= TrailingStopOffset + TrailingStep)
				return;

			var candidateStop = candle.ClosePrice + TrailingStopOffset;
			var threshold = candle.ClosePrice + (TrailingStopOffset + TrailingStep);

			if (!_stopPrice.HasValue || _stopPrice.Value > threshold)
				_stopPrice = _stopPrice.HasValue ? Math.Min(_stopPrice.Value, candidateStop) : candidateStop;
		}
	}

	private decimal CalculateEntryVolume()
	{
		var baseVolume = Volume;
		if (baseVolume <= 0)
			return 0;

		if (UseMartingaleAfterLoss && _lastTradeWasLoss)
			baseVolume *= 2;

		var maxVolume = Volume * MaxPositions;
		if (maxVolume > 0 && baseVolume > maxVolume)
			baseVolume = maxVolume;

		return baseVolume;
	}

	private void InitializePosition(decimal entryPrice, decimal volume, int direction)
	{
		_entryPrice = entryPrice;
		_entryVolume = volume;
		_entryDirection = direction;
		_stopPrice = direction > 0 && StopLossOffset > 0 ? entryPrice - StopLossOffset :
			direction < 0 && StopLossOffset > 0 ? entryPrice + StopLossOffset : null;
		_takeProfitPrice = direction > 0 && TakeProfitOffset > 0 ? entryPrice + TakeProfitOffset :
			direction < 0 && TakeProfitOffset > 0 ? entryPrice - TakeProfitOffset : null;
	}

	private void ExitLong(ICandleMessage candle, decimal exitPrice, string reason)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0)
			return;

		SellMarket(volume);
		FinalizeTrade(exitPrice);
		LogInfo($"Exit long at {exitPrice} due to {reason}.");
	}

	private void ExitShort(ICandleMessage candle, decimal exitPrice, string reason)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0)
			return;

		BuyMarket(volume);
		FinalizeTrade(exitPrice);
		LogInfo($"Exit short at {exitPrice} due to {reason}.");
	}

	private void FinalizeTrade(decimal exitPrice)
	{
		if (_entryDirection == 0 || _entryVolume <= 0)
			return;

		decimal pnl = 0m;
		if (_entryDirection > 0)
			pnl = (exitPrice - _entryPrice) * _entryVolume;
		else
			pnl = (_entryPrice - exitPrice) * _entryVolume;

		_lastTradeWasLoss = pnl < 0;
		_entryPrice = 0;
		_entryVolume = 0;
		_entryDirection = 0;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	private static void AddToHistory(List<decimal> history, decimal value)
	{
		history.Add(value);
		if (history.Count > HistoryCapacity)
			history.RemoveAt(0);
	}

	private static decimal GetHistoryValue(List<decimal> history, int offset)
	{
		return history[^ (offset + 1)];
	}
}
