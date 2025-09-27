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
/// Nirvaman Imax trend strategy converted from MetaTrader 4.
/// Combines Heikin-Ashi candles, dual EMA crossover and an EMA trend filter with timed exits.
/// </summary>
public class NirvamanImaxStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastTrendLength;
	private readonly StrategyParam<int> _slowTrendLength;
	private readonly StrategyParam<int> _filterLength;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<TimeSpan> _closeAfter;
	private readonly StrategyParam<int> _noTradeStartHour;
	private readonly StrategyParam<int> _noTradeEndHour;
	private readonly StrategyParam<int> _brokerTimeOffset;

	private HeikinAshi _heikinAshi = null!;
	private ExponentialMovingAverage _fastTrend = null!;
	private ExponentialMovingAverage _slowTrend = null!;
	private ExponentialMovingAverage _filterEma = null!;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal? _prevClose;

	private DateTimeOffset? _positionOpenTime;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Base trading volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA length representing the iMAX fast phase.
	/// </summary>
	public int FastTrendLength
	{
		get => _fastTrendLength.Value;
		set => _fastTrendLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length representing the iMAX slow phase.
	/// </summary>
	public int SlowTrendLength
	{
		get => _slowTrendLength.Value;
		set => _slowTrendLength.Value = value;
	}

	/// <summary>
	/// EMA period used as baseline trend filter.
	/// </summary>
	public int FilterLength
	{
		get => _filterLength.Value;
		set => _filterLength.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Maximum holding time for a position.
	/// </summary>
	public TimeSpan CloseAfter
	{
		get => _closeAfter.Value;
		set => _closeAfter.Value = value;
	}

	/// <summary>
	/// Start hour of the restricted trading window (server time).
	/// </summary>
	public int NoTradeStartHour
	{
		get => _noTradeStartHour.Value;
		set => _noTradeStartHour.Value = value;
	}

	/// <summary>
	/// End hour of the restricted trading window (server time).
	/// </summary>
	public int NoTradeEndHour
	{
		get => _noTradeEndHour.Value;
		set => _noTradeEndHour.Value = value;
	}

	/// <summary>
	/// Broker time zone offset applied to candle times.
	/// </summary>
	public int BrokerTimeOffset
	{
		get => _brokerTimeOffset.Value;
		set => _brokerTimeOffset.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="NirvamanImaxStrategy"/>.
	/// </summary>
	public NirvamanImaxStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base order volume", "Trading")
			.SetCanOptimize(true, 0.01m, 2m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for signals", "Data");

		_fastTrendLength = Param(nameof(FastTrendLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Trend Length", "Fast EMA length replacing iMAX blue phase", "Indicators")
			.SetCanOptimize(true, 3, 40);

		_slowTrendLength = Param(nameof(SlowTrendLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Slow Trend Length", "Slow EMA length replacing iMAX red phase", "Indicators")
			.SetCanOptimize(true, 5, 80);

		_filterLength = Param(nameof(FilterLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Filter Length", "EMA filter period matching Moving Averages2", "Indicators")
			.SetCanOptimize(true, 5, 60);

		_stopLoss = Param(nameof(StopLoss), 50m)
			.SetDisplay("Stop Loss", "Stop loss distance in price steps", "Risk")
			.SetCanOptimize(true, 0m, 200m);

		_takeProfit = Param(nameof(TakeProfit), 100m)
			.SetDisplay("Take Profit", "Take profit distance in price steps", "Risk")
			.SetCanOptimize(true, 0m, 300m);

		_closeAfter = Param(nameof(CloseAfter), TimeSpan.FromSeconds(15000))
			.SetDisplay("Close After", "Maximum holding time for a trade", "Risk");

		_noTradeStartHour = Param(nameof(NoTradeStartHour), 22)
			.SetDisplay("No-Trade Start", "Restricted window start hour", "Trading")
			.SetRange(0, 23);

		_noTradeEndHour = Param(nameof(NoTradeEndHour), 2)
			.SetDisplay("No-Trade End", "Restricted window end hour", "Trading")
			.SetRange(0, 23);

		_brokerTimeOffset = Param(nameof(BrokerTimeOffset), 0)
			.SetDisplay("Broker Offset", "Broker time zone offset in hours", "Trading")
			.SetCanOptimize(true, -12, 12);
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

		_heikinAshi = null!;
		_fastTrend = null!;
		_slowTrend = null!;
		_filterEma = null!;

		_prevFast = null;
		_prevSlow = null;
		_prevClose = null;

		ClearPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_heikinAshi = new HeikinAshi();
		_fastTrend = new ExponentialMovingAverage { Length = FastTrendLength };
		_slowTrend = new ExponentialMovingAverage { Length = SlowTrendLength };
		_filterEma = new ExponentialMovingAverage { Length = FilterLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_heikinAshi, _fastTrend, _slowTrend, _filterEma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastTrend);
			DrawIndicator(area, _slowTrend);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue heikinValue, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue filterValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Use close time when available for consistent time-based controls.
		var candleTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;

		// Skip candle until Heikin-Ashi indicator outputs valid values.
		if (heikinValue is not HeikinAshiValue ha || ha.Open is not decimal haOpen || ha.Close is not decimal haClose)
		{
			UpdatePrevValues(fastValue.ToDecimal(), slowValue.ToDecimal(), candle.ClosePrice);
			return;
		}

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var ema = filterValue.ToDecimal();

		// Wait until all EMA filters are ready.
		if (!_fastTrend.IsFormed || !_slowTrend.IsFormed || !_filterEma.IsFormed)
		{
			UpdatePrevValues(fast, slow, candle.ClosePrice);
			return;
		}

		// Need previous close to replicate Close[1] check from MT4 version.
		if (_prevClose is null)
		{
			UpdatePrevValues(fast, slow, candle.ClosePrice);
			return;
		}

		// Respect restricted trading hours from the original expert advisor.
		var hour = GetBrokerHour(candleTime);
		if (IsWithinRestrictedHours(hour))
		{
			UpdatePrevValues(fast, slow, candle.ClosePrice);
			return;
		}

		// Close position when maximum holding time is reached.
		if (Position != 0 && _positionOpenTime is DateTimeOffset openTime && CloseAfter > TimeSpan.Zero)
		{
			if (candleTime - openTime >= CloseAfter)
			{
				ExitPosition();
				UpdatePrevValues(fast, slow, candle.ClosePrice);
				return;
			}
		}

		// Apply price-based exits translated from MT4 points.
		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				ExitPosition();
				UpdatePrevValues(fast, slow, candle.ClosePrice);
				return;
			}

			if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				ExitPosition();
				UpdatePrevValues(fast, slow, candle.ClosePrice);
				return;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				ExitPosition();
				UpdatePrevValues(fast, slow, candle.ClosePrice);
				return;
			}

			if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				ExitPosition();
				UpdatePrevValues(fast, slow, candle.ClosePrice);
				return;
			}
		}

		// Avoid trading when the strategy is not ready or trading is disabled.
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdatePrevValues(fast, slow, candle.ClosePrice);
			return;
		}

		// Only evaluate new entries when no position is open.
		if (Position != 0)
		{
			UpdatePrevValues(fast, slow, candle.ClosePrice);
			return;
		}

		// Detect EMA crossover to emulate the iMAX phase cross from MT4.
		var perceptron = 0;
		if (_prevFast is decimal prevFast && _prevSlow is decimal prevSlow)
		{
			if (fast > slow && prevFast <= prevSlow)
				perceptron = 1;
			else if (fast < slow && prevFast >= prevSlow)
				perceptron = -1;
		}

		// Determine Heikin-Ashi candle bias.
		var haDirection = haClose > haOpen ? 1 : haClose < haOpen ? -1 : 0;
		var prevClose = _prevClose.Value;

		// Long entry requires bullish crossover, bullish Heikin-Ashi and price above EMA filter.
		if (perceptron > 0 && haDirection > 0 && prevClose > ema)
		{
			var volume = TradeVolume + (Position < 0 ? Math.Abs(Position) : 0m);
			if (volume > 0)
			{
				BuyMarket(volume);
				OpenLong(candle.ClosePrice, candleTime);
			}
		}
		// Short entry mirrors the bearish conditions from the original advisor.
		else if (perceptron < 0 && haDirection < 0 && prevClose < ema)
		{
			var volume = TradeVolume + (Position > 0 ? Position : 0m);
			if (volume > 0)
			{
				SellMarket(volume);
				OpenShort(candle.ClosePrice, candleTime);
			}
		}

		UpdatePrevValues(fast, slow, candle.ClosePrice);
	}

	private void UpdatePrevValues(decimal fast, decimal slow, decimal close)
	{
		_prevFast = fast;
		_prevSlow = slow;
		_prevClose = close;
	}

	private int GetBrokerHour(DateTimeOffset time)
	{
		var shifted = time + TimeSpan.FromHours(BrokerTimeOffset);
		return shifted.Hour;
	}

	private bool IsWithinRestrictedHours(int hour)
	{
		var start = NoTradeStartHour;
		var end = NoTradeEndHour;

		if (start == end)
			return false;

		if (start < end)
			return hour > start && hour < end;

		return hour > start || hour < end;
	}

	private void OpenLong(decimal entryPrice, DateTimeOffset entryTime)
	{
		_positionOpenTime = entryTime;
		_entryPrice = entryPrice;

		var step = GetPriceStep();

		// Pre-calculate stop-loss and take-profit levels for a long position.
		_stopPrice = StopLoss > 0m ? entryPrice - StopLoss * step : null;
		_takePrice = TakeProfit > 0m ? entryPrice + TakeProfit * step : null;
	}

	private void OpenShort(decimal entryPrice, DateTimeOffset entryTime)
	{
		_positionOpenTime = entryTime;
		_entryPrice = entryPrice;

		var step = GetPriceStep();

		// Pre-calculate stop-loss and take-profit levels for a short position.
		_stopPrice = StopLoss > 0m ? entryPrice + StopLoss * step : null;
		_takePrice = TakeProfit > 0m ? entryPrice - TakeProfit * step : null;
	}

	private void ExitPosition()
	{
		// Market exit mirrors MT4 OrderClose behaviour.
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		ClearPositionState();
	}

	private void ClearPositionState()
	{
		_positionOpenTime = null;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private decimal GetPriceStep()
	{
		// Default to step of 1 when instrument metadata is unavailable.
		return Security?.PriceStep ?? 1m;
	}
}
