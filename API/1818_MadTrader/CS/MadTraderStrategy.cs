using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mad Trader strategy ported from MQL. Uses ATR and RSI to enter trades with trailing stop and basket profit management.
/// </summary>
public class MadTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _trendBars;
	private readonly StrategyParam<decimal> _minCandle;
	private readonly StrategyParam<decimal> _maxCandle;
	private readonly StrategyParam<decimal> _maxAtr;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _rsiLower;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<TimeSpan> _tradeInterval;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _basketProfit;
	private readonly StrategyParam<decimal> _basketBoost;
	private readonly StrategyParam<int> _refreshHours;
	private readonly StrategyParam<decimal> _exponentialGrowth;

	private DateTimeOffset _lastTradeTime;
	private DateTimeOffset _lastExponentUpdate;

	private decimal _prevAtr;
	private decimal _prevRsi;
	private decimal _rsiTrend;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private bool _isLongPosition;
	private bool _trailingActive;

	private decimal _startBalance;
	private decimal _expectedEquity;
	private int _profitType;

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Number of bars to estimate RSI trend.
	/// </summary>
	public int TrendBars
	{
		get => _trendBars.Value;
		set => _trendBars.Value = value;
	}

	/// <summary>
	/// Minimum candle body size to allow entry.
	/// </summary>
	public decimal MinCandle
	{
		get => _minCandle.Value;
		set => _minCandle.Value = value;
	}

	/// <summary>
	/// Maximum candle body size to allow entry.
	/// </summary>
	public decimal MaxCandle
	{
		get => _maxCandle.Value;
		set => _maxCandle.Value = value;
	}

	/// <summary>
	/// Maximum ATR value in price points.
	/// </summary>
	public decimal MaxAtr
	{
		get => _maxAtr.Value;
		set => _maxAtr.Value = value;
	}

	/// <summary>
	/// Upper RSI threshold for sell signals.
	/// </summary>
	public decimal RsiUpperLevel
	{
		get => _rsiUpper.Value;
		set => _rsiUpper.Value = value;
	}

	/// <summary>
	/// Lower RSI threshold for buy signals.
	/// </summary>
	public decimal RsiLowerLevel
	{
		get => _rsiLower.Value;
		set => _rsiLower.Value = value;
	}

	/// <summary>
	/// Start hour for trading window.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// End hour for trading window.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Minimal time between two trades.
	/// </summary>
	public TimeSpan TradeInterval
	{
		get => _tradeInterval.Value;
		set => _tradeInterval.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price points.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Basket profit threshold relative to balance.
	/// </summary>
	public decimal BasketProfit
	{
		get => _basketProfit.Value;
		set => _basketProfit.Value = value;
	}

	/// <summary>
	/// Boost factor for basket profit when equity is below exponent.
	/// </summary>
	public decimal BasketBoost
	{
		get => _basketBoost.Value;
		set => _basketBoost.Value = value;
	}

	/// <summary>
	/// Hours interval to refresh exponent target.
	/// </summary>
	public int RefreshHours
	{
		get => _refreshHours.Value;
		set => _refreshHours.Value = value;
	}

	/// <summary>
	/// Expected growth of balance per refresh period.
	/// </summary>
	public decimal ExponentialGrowth
	{
		get => _exponentialGrowth.Value;
		set => _exponentialGrowth.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public MadTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR indicator period", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI indicator period", "Indicators");

		_trendBars = Param(nameof(TrendBars), 60)
			.SetGreaterThanZero()
			.SetDisplay("Trend Bars", "Bars for RSI trend estimation", "Indicators");

		_minCandle = Param(nameof(MinCandle), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Min Candle", "Minimal candle body", "Filters");

		_maxCandle = Param(nameof(MaxCandle), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Max Candle", "Maximal candle body", "Filters");

		_maxAtr = Param(nameof(MaxAtr), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Max ATR", "ATR must be below this", "Filters");

		_rsiUpper = Param(nameof(RsiUpperLevel), 50m)
			.SetDisplay("RSI Upper", "RSI upper level", "Indicators");

		_rsiLower = Param(nameof(RsiLowerLevel), 50m)
			.SetDisplay("RSI Lower", "RSI lower level", "Indicators");

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Trading window start hour", "General");

		_endHour = Param(nameof(EndHour), 23)
			.SetDisplay("End Hour", "Trading window end hour", "General");

		_tradeInterval = Param(nameof(TradeInterval), TimeSpan.FromMinutes(30))
			.SetDisplay("Trade Interval", "Minimal time between trades", "General");

		_trailingStop = Param(nameof(TrailingStop), 7m)
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk");

		_basketProfit = Param(nameof(BasketProfit), 1.05m)
			.SetDisplay("Basket Profit", "Equity ratio to close basket", "Risk");

		_basketBoost = Param(nameof(BasketBoost), 1.1m)
			.SetDisplay("Basket Boost", "Multiplier when equity below target", "Risk");

		_refreshHours = Param(nameof(RefreshHours), 24)
			.SetGreaterThanZero()
			.SetDisplay("Refresh Hours", "Hours to refresh exponent", "Risk");

		_exponentialGrowth = Param(nameof(ExponentialGrowth), 0.01m)
			.SetDisplay("Exponent Growth", "Expected growth per period", "Risk");
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

		_lastTradeTime = DateTimeOffset.MinValue;
		_lastExponentUpdate = DateTimeOffset.MinValue;

		_prevAtr = 0m;
		_prevRsi = 0m;
		_rsiTrend = 50m;

		_entryPrice = 0m;
		_stopPrice = 0m;
		_isLongPosition = false;
		_trailingActive = false;

		_startBalance = 0m;
		_expectedEquity = 0m;
		_profitType = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_startBalance = Portfolio?.CurrentValue ?? 0m;
		_expectedEquity = _startBalance;

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(atr, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateExponent(candle.CloseTime);

		var hour = candle.OpenTime.Hour;
		var start = StartHour;
		var end = EndHour;
		var inWindow = start < end ? hour >= start && hour < end : hour >= start || hour < end;
		if (!inWindow)
			return;

		var body = Math.Abs(candle.OpenPrice - candle.ClosePrice);
		if (body < MinCandle || body > MaxCandle)
			return;

		if (atrValue >= MaxAtr || atrValue <= _prevAtr)
			return;

		var now = candle.CloseTime;
		if (now - _lastTradeTime < TradeInterval)
			return;

		_rsiTrend += (rsiValue - _rsiTrend) / TrendBars;
		var rsiBull = _rsiTrend > 50;
		var rsiBear = _rsiTrend < 50;

		if (rsiBull && rsiValue > _prevRsi && rsiValue < RsiLowerLevel && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_isLongPosition = true;
			_lastTradeTime = now;
			_trailingActive = false;
		}
		else if (rsiBear && rsiValue < _prevRsi && rsiValue > RsiUpperLevel && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_isLongPosition = false;
			_lastTradeTime = now;
			_trailingActive = false;
		}

		_prevAtr = atrValue;
		_prevRsi = rsiValue;

		ManageTrailingStop(candle);
		ManageBasket();
	}

	private void ManageTrailingStop(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var newStop = candle.ClosePrice - TrailingStop;
			if (!_trailingActive || newStop > _stopPrice)
			{
				_stopPrice = newStop;
				_trailingActive = true;
			}

			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				_trailingActive = false;
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			var newStop = candle.ClosePrice + TrailingStop;
			if (!_trailingActive || newStop < _stopPrice)
			{
				_stopPrice = newStop;
				_trailingActive = true;
			}

			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				_trailingActive = false;
				_entryPrice = 0;
			}
		}
		else
		{
			_trailingActive = false;
			_entryPrice = 0;
			_stopPrice = 0;
		}
	}

	private void UpdateExponent(DateTimeOffset time)
	{
		if (time - _lastExponentUpdate >= TimeSpan.FromHours(RefreshHours))
		{
			_expectedEquity = (Portfolio?.CurrentValue ?? _expectedEquity) * (1 + ExponentialGrowth);
			_lastExponentUpdate = time;
		}

		var equity = Portfolio?.CurrentValue ?? 0m;
		_profitType = equity < _expectedEquity ? 1 : 2;
	}

	private void ManageBasket()
	{
		var balance = Portfolio?.CurrentValue ?? 0m;
		if (balance == 0 || _startBalance == 0)
			return;

		var equityRatio = balance / _startBalance;

		if (_profitType == 1)
		{
			if (balance / _expectedEquity > 1 || equityRatio > BasketProfit * BasketBoost)
				ClosePositions();
		}
		else
		{
			if (equityRatio > BasketProfit)
				ClosePositions();
		}
	}

	private void ClosePositions()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));
	}
}
