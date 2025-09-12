using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA cross strategy with optional session filter and trailing stop.
/// Uses two zero lag exponential moving averages.
/// </summary>
public class MicuRobertEmaCrossStrategy : Strategy
{
	private readonly StrategyParam<bool> _useTradingSession;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _buyEntryPrice;
	private decimal _sellEntryPrice;
	private decimal _buyApex;
	private decimal _sellApex;
	private decimal _tpOffset;
	private decimal _slOffset;
	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevPrice;
	private bool _isInitialized;

	/// <summary>
	/// Enable trading only within session.
	/// </summary>
	public bool UseTradingSession
	{
		get => _useTradingSession.Value;
		set => _useTradingSession.Value = value;
	}

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Session start time.
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// Session end time.
	/// </summary>
	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	/// <summary>
	/// Take profit in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Fast ZLEMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow ZLEMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes the strategy.
	/// </summary>
	public MicuRobertEmaCrossStrategy()
	{
		_useTradingSession = Param(nameof(UseTradingSession), true)
			.SetDisplay("Use Trading Session", "Enable session filter", "General");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing Stop", "Enable trailing stop", "General");

		_sessionStart = Param(nameof(SessionStart), TimeSpan.FromHours(4))
			.SetDisplay("Session Start", "Trading session start", "Session");

		_sessionEnd = Param(nameof(SessionEnd), TimeSpan.FromHours(15))
			.SetDisplay("Session End", "Trading session end", "Session");

		_takeProfitPips = Param(nameof(TakeProfitPips), 55m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Pips", "Take profit in pips", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 22m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Pips", "Stop loss in pips", "Risk");

		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast ZLEMA Length", "Period of fast ZLEMA", "Indicators");

		_slowLength = Param(nameof(SlowLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("Slow ZLEMA Length", "Period of slow ZLEMA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles for calculations", "General");
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

		_buyEntryPrice = 0m;
		_sellEntryPrice = 0m;
		_buyApex = 0m;
		_sellApex = 0m;
		_tpOffset = 0m;
		_slOffset = 0m;
		_prevFast = 0m;
		_prevSlow = 0m;
		_prevPrice = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tpOffset = TakeProfitPips * (Security?.PriceStep ?? 1m) * 10m;
		_slOffset = StopLossPips * (Security?.PriceStep ?? 1m) * 10m;

		var fastMa = new ZeroLagExponentialMovingAverage
		{
			Length = FastLength,
			CandlePrice = CandlePrice.Open,
		};

		var slowMa = new ZeroLagExponentialMovingAverage
		{
			Length = SlowLength,
			CandlePrice = CandlePrice.Open,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private bool InSession(TimeSpan time)
	{
		return time >= SessionStart && time <= SessionEnd;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.OpenPrice;

		if (Position > 0)
		{
			if (candle.HighPrice > _buyApex)
				_buyApex = candle.HighPrice;

			var stopLevel = UseTrailingStop ? _buyApex - _slOffset : _buyEntryPrice - _slOffset;
			var takeProfit = _buyEntryPrice + _tpOffset;

			if (candle.LowPrice <= stopLevel || candle.HighPrice >= takeProfit)
			{
				SellMarket(Math.Abs(Position));
				_buyEntryPrice = 0m;
				_buyApex = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.LowPrice < _sellApex)
				_sellApex = candle.LowPrice;

			var stopLevel = UseTrailingStop ? _sellApex + _slOffset : _sellEntryPrice + _slOffset;
			var takeProfit = _sellEntryPrice - _tpOffset;

			if (candle.HighPrice >= stopLevel || candle.LowPrice <= takeProfit)
			{
				BuyMarket(Math.Abs(Position));
				_sellEntryPrice = 0m;
				_sellApex = 0m;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_prevPrice = price;
			_isInitialized = true;
			return;
		}

		var inSession = !UseTradingSession || InSession(candle.OpenTime.TimeOfDay);

		if (inSession)
		{
			var crossFastAboveSlow = _prevFast <= _prevSlow && fast > slow;
			var crossPriceAboveFast = _prevPrice <= _prevFast && price > fast && fast > slow;
			var crossFastBelowSlow = _prevFast >= _prevSlow && fast < slow;
			var crossPriceBelowFast = _prevPrice >= _prevFast && price < fast && fast < slow;

			if ((crossFastAboveSlow || crossPriceAboveFast) && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_buyEntryPrice = price;
				_buyApex = candle.HighPrice;
			}
			else if ((crossFastBelowSlow || crossPriceBelowFast) && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_sellEntryPrice = price;
				_sellApex = candle.LowPrice;
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
		_prevPrice = price;
	}
}
