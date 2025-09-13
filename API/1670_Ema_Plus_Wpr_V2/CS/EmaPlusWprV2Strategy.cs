using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R and EMA based strategy with trend filter, trailing stop and optional exit rules.
/// </summary>
public class EmaPlusWprV2Strategy : Strategy
{
	private readonly StrategyParam<bool> _useEmaTrend;
	private readonly StrategyParam<int> _barsInTrend;
	private readonly StrategyParam<int> _emaTrendPeriod;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _wprRetracement;
	private readonly StrategyParam<bool> _useWprExit;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<bool> _useUnprofitExit;
	private readonly StrategyParam<int> _maxUnprofitBars;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _emaHistory = new();
	private bool _buyAllowed;
	private bool _sellAllowed;
	private int _unprofitBars;
	private decimal _entryPrice;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;

	/// <summary>
	/// Use EMA trend filter.
	/// </summary>
	public bool UseEmaTrend { get => _useEmaTrend.Value; set => _useEmaTrend.Value = value; }

	/// <summary>
	/// Bars required to confirm trend.
	/// </summary>
	public int BarsInTrend { get => _barsInTrend.Value; set => _barsInTrend.Value = value; }

	/// <summary>
	/// EMA period used for trend detection.
	/// </summary>
	public int EmaTrendPeriod { get => _emaTrendPeriod.Value; set => _emaTrendPeriod.Value = value; }

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WprPeriod { get => _wprPeriod.Value; set => _wprPeriod.Value = value; }

	/// <summary>
	/// Required retracement for a new trade.
	/// </summary>
	public decimal WprRetracement { get => _wprRetracement.Value; set => _wprRetracement.Value = value; }

	/// <summary>
	/// Use Williams %R exit rule.
	/// </summary>
	public bool UseWprExit { get => _useWprExit.Value; set => _useWprExit.Value = value; }

	/// <summary>
	/// Maximum number of trades in one direction.
	/// </summary>
	public int MaxTrades { get => _maxTrades.Value; set => _maxTrades.Value = value; }

	/// <summary>
	/// Stop loss distance.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take profit distance.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailingStop { get => _useTrailingStop.Value; set => _useTrailingStop.Value = value; }

	/// <summary>
	/// Minimal price move to shift trailing stop.
	/// </summary>
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }

	/// <summary>
	/// Exit if position is not profitable for a number of bars.
	/// </summary>
	public bool UseUnprofitExit { get => _useUnprofitExit.Value; set => _useUnprofitExit.Value = value; }

	/// <summary>
	/// Maximum consecutive unprofitable bars.
	/// </summary>
	public int MaxUnprofitBars { get => _maxUnprofitBars.Value; set => _maxUnprofitBars.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EmaPlusWprV2Strategy()
	{
		_useEmaTrend = Param(nameof(UseEmaTrend), true).SetDisplay("Use EMA Trend", "Enable EMA trend filter", "Filters");
		_barsInTrend = Param(nameof(BarsInTrend), 1).SetGreaterThanZero().SetDisplay("Trend Bars", "Bars required in trend", "Filters");
		_emaTrendPeriod = Param(nameof(EmaTrendPeriod), 144).SetGreaterThanZero().SetDisplay("EMA Period", "EMA period for trend", "Indicators");
		_wprPeriod = Param(nameof(WprPeriod), 46).SetGreaterThanZero().SetDisplay("WPR Period", "Williams %R period", "Indicators");
		_wprRetracement = Param(nameof(WprRetracement), 30m).SetGreaterThanZero().SetDisplay("WPR Retracement", "Retracement to allow new trade", "Indicators");
		_useWprExit = Param(nameof(UseWprExit), true).SetDisplay("Use WPR Exit", "Exit when WPR crosses opposite level", "Risk");
		_maxTrades = Param(nameof(MaxTrades), 2).SetGreaterThanZero().SetDisplay("Max Trades", "Maximum trades for pyramiding", "Trading");
		_stopLoss = Param(nameof(StopLoss), 50m).SetGreaterThanZero().SetDisplay("Stop Loss", "Stop loss in price units", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 200m).SetGreaterThanZero().SetDisplay("Take Profit", "Take profit in price units", "Risk");
		_useTrailingStop = Param(nameof(UseTrailingStop), false).SetDisplay("Use Trailing", "Enable trailing stop", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 10m).SetGreaterThanZero().SetDisplay("Trailing Step", "Minimum move to shift stop", "Risk");
		_useUnprofitExit = Param(nameof(UseUnprofitExit), false).SetDisplay("Use Unprofit Exit", "Exit if trade not in profit", "Risk");
		_maxUnprofitBars = Param(nameof(MaxUnprofitBars), 5).SetGreaterThanZero().SetDisplay("Max Unprofit Bars", "Bars without profit", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var wpr = new WilliamsPercentRange { Length = WprPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaTrendPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wpr, ema, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal wprValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_emaHistory.Enqueue(emaValue);
		while (_emaHistory.Count > BarsInTrend + 1)
			_emaHistory.Dequeue();

		bool trendUp = false;
		bool trendDown = false;

		if (_emaHistory.Count == BarsInTrend + 1)
		{
			trendUp = true;
			trendDown = true;

			var arr = _emaHistory.ToArray();
			for (int i = 0; i < BarsInTrend; i++)
			{
				if (arr[i + 1] <= arr[i])
					trendUp = false;
				if (arr[i + 1] >= arr[i])
					trendDown = false;
			}
		}

		if (wprValue > -100m + WprRetracement)
			_buyAllowed = true;

		if (wprValue < -WprRetracement)
			_sellAllowed = true;

		var longLimit = Volume * MaxTrades;
		var shortLimit = -Volume * MaxTrades;

		bool canLong = (!UseEmaTrend || trendUp) && wprValue <= -99.99m && _buyAllowed && Position < longLimit;
		bool canShort = (!UseEmaTrend || trendDown) && wprValue >= -0.01m && _sellAllowed && Position > shortLimit;

		if (Position != 0)
		{
			bool inProfit = Position > 0 ? candle.ClosePrice > _entryPrice : candle.ClosePrice < _entryPrice;

			if (inProfit)
				_unprofitBars = 0;
			else
				_unprofitBars++;

			if (UseUnprofitExit && _unprofitBars > MaxUnprofitBars)
			{
				ClosePosition();
				return;
			}
		}

		if (canLong)
		{
			BuyMarket(Volume);
			_buyAllowed = false;
			_entryPrice = candle.ClosePrice;
			_longStop = candle.ClosePrice - StopLoss;
			_longTake = candle.ClosePrice + TakeProfit;
		}
		else if (canShort)
		{
			SellMarket(Volume);
			_sellAllowed = false;
			_entryPrice = candle.ClosePrice;
			_shortStop = candle.ClosePrice + StopLoss;
			_shortTake = candle.ClosePrice - TakeProfit;
		}
		else
		{
			if (Position > 0)
			{
				if (UseWprExit && wprValue >= -0.01m)
					ClosePosition();
				else if (candle.LowPrice <= _longStop || candle.HighPrice >= _longTake)
					ClosePosition();
				else if (UseTrailingStop)
				{
					var newStop = candle.ClosePrice - StopLoss;
					if (newStop - TrailingStop > _longStop)
						_longStop = newStop;
				}
			}
			else if (Position < 0)
			{
				if (UseWprExit && wprValue <= -99.99m)
					ClosePosition();
				else if (candle.HighPrice >= _shortStop || candle.LowPrice <= _shortTake)
					ClosePosition();
				else if (UseTrailingStop)
				{
					var newStop = candle.ClosePrice + StopLoss;
					if (newStop + TrailingStop < _shortStop)
						_shortStop = newStop;
				}
			}
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Math.Abs(Position));
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));

		_unprofitBars = 0;
	}
}

