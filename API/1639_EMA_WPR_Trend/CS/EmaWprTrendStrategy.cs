using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining EMA trend filter with Williams %R signals.
/// Buys at oversold levels and sells at overbought levels.
/// </summary>
public class EmaWprTrendStrategy : Strategy
{
	private readonly StrategyParam<bool> _useEmaTrend;
	private readonly StrategyParam<int> _barsInTrend;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<decimal> _wprRetracement;
	private readonly StrategyParam<bool> _useWprExit;
	private readonly StrategyParam<bool> _useUnprofitExit;
	private readonly StrategyParam<int> _maxUnprofitBars;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private WilliamsR _wpr;

	private bool _buyAllowed;
	private bool _sellAllowed;
	private decimal? _entryPrice;
	private decimal? _prevEma;
	private int _trendCounter;
	private int _unprofitBars;

	/// <summary>
	/// Enable EMA trend filter.
	/// </summary>
	public bool UseEmaTrend
	{
		get => _useEmaTrend.Value;
		set => _useEmaTrend.Value = value;
	}

	/// <summary>
	/// Required consecutive bars in EMA trend.
	/// </summary>
	public int BarsInTrend
	{
		get => _barsInTrend.Value;
		set => _barsInTrend.Value = value;
	}

	/// <summary>
	/// EMA period for trend calculation.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R retracement required for a new entry.
	/// </summary>
	public decimal WprRetracement
	{
		get => _wprRetracement.Value;
		set => _wprRetracement.Value = value;
	}

	/// <summary>
	/// Exit position using opposite Williams %R extreme.
	/// </summary>
	public bool UseWprExit
	{
		get => _useWprExit.Value;
		set => _useWprExit.Value = value;
	}

	/// <summary>
	/// Exit if position was not profitable for N bars.
	/// </summary>
	public bool UseUnprofitExit
	{
		get => _useUnprofitExit.Value;
		set => _useUnprofitExit.Value = value;
	}

	/// <summary>
	/// Maximum bars without profit before exit.
	/// </summary>
	public int MaxUnprofitBars
	{
		get => _maxUnprofitBars.Value;
		set => _maxUnprofitBars.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EmaWprTrendStrategy"/>.
	/// </summary>
	public EmaWprTrendStrategy()
	{
		_useEmaTrend = Param(nameof(UseEmaTrend), true)
			.SetDisplay("Use EMA Trend", "Enable EMA trend filter", "Filters");

		_barsInTrend = Param(nameof(BarsInTrend), 1)
			.SetGreaterThanZero()
			.SetDisplay("Bars In Trend", "Consecutive bars for trend", "Filters");

		_emaPeriod = Param(nameof(EmaPeriod), 144)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA", "Indicators")
			.SetCanOptimize(true);

		_wprPeriod = Param(nameof(WprPeriod), 46)
			.SetGreaterThanZero()
			.SetDisplay("WPR Period", "%R length", "Indicators")
			.SetCanOptimize(true);

		_wprRetracement = Param(nameof(WprRetracement), 30m)
			.SetGreaterOrEqual(0)
			.SetDisplay("WPR Retracement", "Retracement for next trade", "Signals")
			.SetCanOptimize(true);

		_useWprExit = Param(nameof(UseWprExit), true)
			.SetDisplay("Use WPR Exit", "Exit using Williams %R", "Exits");

		_useUnprofitExit = Param(nameof(UseUnprofitExit), false)
			.SetDisplay("Use Unprofit Exit", "Exit if no profit", "Exits");

		_maxUnprofitBars = Param(nameof(MaxUnprofitBars), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Unprofit Bars", "Bars without profit", "Exits");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		Volume = 1;
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
		_ema = null;
		_wpr = null;
		_buyAllowed = true;
		_sellAllowed = true;
		_entryPrice = null;
		_prevEma = null;
		_trendCounter = 0;
		_unprofitBars = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_wpr = new WilliamsR { Length = WprPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ema, _wpr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _wpr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal wprValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateTrend(emaValue);

		var price = candle.ClosePrice;

		if (Position != 0)
		{
			if (UseUnprofitExit)
			{
				var profitable = Position > 0 ? price > _entryPrice : price < _entryPrice;
				if (profitable)
					_unprofitBars = 0;
				else
					_unprofitBars++;
			}

			if ((Position > 0 && ((UseWprExit && wprValue >= 0) || (UseUnprofitExit && _unprofitBars > MaxUnprofitBars))) ||
				(Position < 0 && ((UseWprExit && wprValue <= -100) || (UseUnprofitExit && _unprofitBars > MaxUnprofitBars))))
			{
				ClosePosition();
				return;
			}
		}

		if (wprValue > -100 + WprRetracement)
			_buyAllowed = true;

		if (wprValue < 0 - WprRetracement)
			_sellAllowed = true;

		var trendUp = UseEmaTrend ? _trendCounter >= BarsInTrend : true;
		var trendDown = UseEmaTrend ? _trendCounter <= -BarsInTrend : true;

		if (Position <= 0 && _buyAllowed && wprValue <= -100 && trendUp)
		{
			BuyMarket();
			_entryPrice = price;
			_buyAllowed = false;
			_unprofitBars = 0;
		}
		else if (Position >= 0 && _sellAllowed && wprValue >= 0 && trendDown)
		{
			SellMarket();
			_entryPrice = price;
			_sellAllowed = false;
			_unprofitBars = 0;
		}
	}

	private void UpdateTrend(decimal emaValue)
	{
		if (_prevEma is null)
		{
			_prevEma = emaValue;
			_trendCounter = 0;
			return;
		}

		if (emaValue > _prevEma)
			_trendCounter = Math.Min(_trendCounter + 1, BarsInTrend);
		else if (emaValue < _prevEma)
			_trendCounter = Math.Max(_trendCounter - 1, -BarsInTrend);
		else
			_trendCounter = 0;

		_prevEma = emaValue;
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
		_entryPrice = null;
		_unprofitBars = 0;
	}
}
