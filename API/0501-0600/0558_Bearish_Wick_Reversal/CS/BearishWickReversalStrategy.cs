namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Long-only bearish lower-wick reversal with independent previous-high exits.</summary>
public class BearishWickReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<bool> _useEmaFilter;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTime> _startTime;
	private readonly StrategyParam<DateTime> _endTime;
	private EMA _ema;
	private decimal _previousHigh;
	private Order _pending;
	private DateTime _lastTime;

	/// <summary>Negative lower wick percentage threshold.</summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>Require close above the formed EMA.</summary>
	public bool UseEmaFilter
	{
		get => _useEmaFilter.Value;
		set => _useEmaFilter.Value = value;
	}

	/// <summary>Optional EMA length.</summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>Finished signal candle interval.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>First eligible UTC entry time.</summary>
	public DateTime StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>Last eligible UTC entry time.</summary>
	public DateTime EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>Initializes the published wick, optional trend and UTC entry-window parameters.</summary>
	public BearishWickReversalStrategy()
	{
		_threshold = Param(nameof(Threshold), -1m).SetRange(-100m, 0m)
			.SetDisplay("Threshold", "Negative lower wick percentage threshold", "Parameters");
		_useEmaFilter = Param(nameof(UseEmaFilter), false)
			.SetDisplay("UseEmaFilter", "Require close above the formed EMA", "Parameters");
		_emaPeriod = Param(nameof(EmaPeriod), 200).SetGreaterThanZero()
			.SetDisplay("EmaPeriod", "Optional EMA length", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("CandleType", "Finished signal candle interval", "Parameters");
		_startTime = Param(nameof(StartTime), new DateTime(2014, 1, 1, 0, 0, 0, DateTimeKind.Utc))
			.SetDisplay("StartTime", "First eligible UTC entry time", "Parameters");
		_endTime = Param(nameof(EndTime), new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc))
			.SetDisplay("EndTime", "Last eligible UTC entry time", "Parameters");
		OrderRegistering += order => _pending = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_ema = null;
		_previousHigh = 0m;
		_pending = null;
		_lastTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (StartTime.Kind != DateTimeKind.Utc || EndTime.Kind != DateTimeKind.Utc || StartTime > EndTime)
			throw new InvalidOperationException("StartTime and EndTime must form an ordered UTC entry window.");
		base.OnStarted2(time);
		var candles = SubscribeCandles(CandleType);
		if (UseEmaFilter)
		{
			_ema = new EMA { Length = EmaPeriod };
			candles.Bind(_ema, ProcessFilteredCandle).Start();
		}
		else candles.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, candles);
			if (_ema is not null) DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessFilteredCandle(ICandleMessage candle, decimal average) => ProcessCandle(candle);

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || candle.ClosePrice <= 0m || candle.OpenTime <= _lastTime)
			return;
		var previousHigh = _previousHigh;
		_previousHigh = candle.HighPrice;
		_lastTime = candle.OpenTime;
		if (!IsFormedAndOnlineAndAllowTrading() || _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position > 0m)
		{
			if (previousHigh > 0m && candle.ClosePrice > previousHigh)
				SellMarket(Position);
			return;
		}
		if (Position != 0m || candle.OpenTime < StartTime || candle.OpenTime > EndTime
			|| candle.ClosePrice >= candle.OpenPrice || 100m * (candle.LowPrice - candle.ClosePrice) / candle.ClosePrice > Threshold)
			return;
		if (_ema is not null && (!_ema.IsFormed || candle.ClosePrice <= _ema.GetCurrentValue()))
			return;
		BuyMarket(Volume);
	}
}
