using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on TrendEnvelopes indicator with ATR-based signals.
/// </summary>
public class TrendEnvelopesStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _deviation;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrSensitivity;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyEntry;
	private readonly StrategyParam<bool> _sellEntry;
	private readonly StrategyParam<bool> _buyExit;
	private readonly StrategyParam<bool> _sellExit;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;

	private ExponentialMovingAverage _ma;
	private AverageTrueRange _atr;

	private decimal _prevSmax;
	private decimal _prevSmin;
	private int _prevTrend;
	private bool _initialized;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Percentage deviation for envelopes.
	/// </summary>
	public decimal Deviation
	{
		get => _deviation.Value;
		set => _deviation.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR shift sensitivity.
	/// </summary>
	public decimal AtrSensitivity
	{
		get => _atrSensitivity.Value;
		set => _atrSensitivity.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Whether long entries are allowed.
	/// </summary>
	public bool BuyEntry
	{
		get => _buyEntry.Value;
		set => _buyEntry.Value = value;
	}

	/// <summary>
	/// Whether short entries are allowed.
	/// </summary>
	public bool SellEntry
	{
		get => _sellEntry.Value;
		set => _sellEntry.Value = value;
	}

	/// <summary>
	/// Whether long positions can be closed.
	/// </summary>
	public bool BuyExit
	{
		get => _buyExit.Value;
		set => _buyExit.Value = value;
	}

	/// <summary>
	/// Whether short positions can be closed.
	/// </summary>
	public bool SellExit
	{
		get => _sellExit.Value;
		set => _sellExit.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TrendEnvelopesStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Moving average length", "Indicator");

		_deviation = Param(nameof(Deviation), 0.2m)
		.SetGreaterThanZero()
		.SetDisplay("Deviation", "Percent offset for envelopes", "Indicator");

		_atrPeriod = Param(nameof(AtrPeriod), 15)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR calculation length", "Indicator");

		_atrSensitivity = Param(nameof(AtrSensitivity), 0.5m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Sensitivity", "Multiplier for signal shift", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for strategy", "General");

		_buyEntry = Param(nameof(BuyEntry), true)
		.SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading");

		_sellEntry = Param(nameof(SellEntry), true)
		.SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading");

		_buyExit = Param(nameof(BuyExit), true)
		.SetDisplay("Enable Long Exit", "Allow closing long positions", "Trading");

		_sellExit = Param(nameof(SellExit), true)
		.SetDisplay("Enable Short Exit", "Allow closing short positions", "Trading");

		_takeProfit = Param(nameof(TakeProfit), 2000)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Target in points", "Protection");

		_stopLoss = Param(nameof(StopLoss), 1000)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Loss limit in points", "Protection");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = new ExponentialMovingAverage { Length = MaPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_ma, _atr, ProcessCandle)
		.Start();

		var step = Security.PriceStep ?? 1m;
		StartProtection(
		takeProfit: new Unit(TakeProfit * step, UnitTypes.Point),
		stopLoss: new Unit(StopLoss * step, UnitTypes.Point));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var smax = (1m + Deviation / 100m) * maValue;
		var smin = (1m - Deviation / 100m) * maValue;
		var trend = _prevTrend;

		if (_initialized)
		{
			if (candle.ClosePrice > _prevSmax)
			trend = 1;
			if (candle.ClosePrice < _prevSmin)
			trend = -1;
		}

		decimal? upSignal = null;
		decimal? downSignal = null;
		var upTrend = false;
		var downTrend = false;

		if (!_initialized)
		{
			_prevSmax = smax;
			_prevSmin = smin;
			_prevTrend = 0;
			_initialized = true;
			return;
		}

		if (trend > 0)
		{
			if (smin < _prevSmin)
			smin = _prevSmin;

			upTrend = true;

			if (_prevTrend <= 0)
			upSignal = smin - AtrSensitivity * atrValue;
		}
		else if (trend < 0)
		{
			if (smax > _prevSmax)
			smax = _prevSmax;

			downTrend = true;

			if (_prevTrend >= 0)
			downSignal = smax + AtrSensitivity * atrValue;
		}

		_prevSmax = smax;
		_prevSmin = smin;
		_prevTrend = trend;

		if (BuyExit && downTrend && Position > 0)
		SellMarket(Position);

		if (SellExit && upTrend && Position < 0)
		BuyMarket(-Position);

		if (BuyEntry && upSignal.HasValue && Position <= 0)
		BuyMarket(Volume + Math.Abs(Position));

		if (SellEntry && downSignal.HasValue && Position >= 0)
		SellMarket(Volume + Math.Abs(Position));
	}
}

