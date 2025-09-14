using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Envelope reversion strategy with trailing stop.
/// </summary>
public class ForexFrausSloggerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _envelopePercent;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _trailingStep;
	private readonly StrategyParam<bool> _profitTrailing;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma = null!;

	private bool _wasAboveUpper;
	private bool _wasBelowLower;
	private decimal _entryPrice;
	private decimal _peakPrice;
	private decimal _troughPrice;
	private decimal _currentStop;

	/// <summary>
	/// Envelope percent.
	/// </summary>
	public decimal EnvelopePercent
	{
		get => _envelopePercent.Value;
		set => _envelopePercent.Value = value;
	}

	/// <summary>
	/// Trailing stop distance.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Trailing step for stop adjustment.
	/// </summary>
	public decimal TrailingStep
	{
		get => _trailingStep.Value;
		set => _trailingStep.Value = value;
	}

	/// <summary>
	/// Enable trailing only after profit.
	/// </summary>
	public bool ProfitTrailing
	{
		get => _profitTrailing.Value;
		set => _profitTrailing.Value = value;
	}

	/// <summary>
	/// Use time filter.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Trading start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading stop hour.
	/// </summary>
	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = value;
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
	/// Initializes a new instance of <see cref="ForexFrausSloggerStrategy"/>.
	/// </summary>
	public ForexFrausSloggerStrategy()
	{
		_envelopePercent = Param(nameof(EnvelopePercent), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Envelope %", "Envelope percent", "Parameters");

		_trailingStop = Param(nameof(TrailingStop), 0.001m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk");

		_trailingStep = Param(nameof(TrailingStep), 0.0001m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step", "Minimum stop move", "Risk");

		_profitTrailing = Param(nameof(ProfitTrailing), true)
			.SetDisplay("Profit Trailing", "Trail only after profit", "Risk");

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
			.SetDisplay("Use Time Filter", "Enable trading hours filter", "Parameters");

		_startHour = Param(nameof(StartHour), 7)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Trading start hour", "Parameters");

		_stopHour = Param(nameof(StopHour), 17)
			.SetRange(0, 23)
			.SetDisplay("Stop Hour", "Trading stop hour", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "Parameters");
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
		_sma = null!;
		_wasAboveUpper = false;
		_wasBelowLower = false;
		_entryPrice = 0m;
		_peakPrice = 0m;
		_troughPrice = 0m;
		_currentStop = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		_sma = new SimpleMovingAverage { Length = 1 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal basis)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (UseTimeFilter && !IsWithinTradingHours(candle.OpenTime))
			return;

		var percent = EnvelopePercent / 100m;
		var upper = basis * (1m + percent);
		var lower = basis * (1m - percent);
		var close = candle.ClosePrice;

		if (close > upper)
			_wasAboveUpper = true;

		if (close < lower)
			_wasBelowLower = true;

		if (_wasAboveUpper && close <= upper)
		{
			SellMarket(Position + Volume);
			_entryPrice = close;
			_troughPrice = close;
			_currentStop = close + TrailingStop;
			_wasAboveUpper = false;
			return;
		}

		if (_wasBelowLower && close >= lower)
		{
			BuyMarket(-Position + Volume);
			_entryPrice = close;
			_peakPrice = close;
			_currentStop = close - TrailingStop;
			_wasBelowLower = false;
			return;
		}

		if (Position > 0)
		{
			if (candle.HighPrice >= _peakPrice + TrailingStep)
			{
				_peakPrice = candle.HighPrice;
				var newStop = _peakPrice - TrailingStop;
				if (!ProfitTrailing || newStop > _entryPrice)
					_currentStop = Math.Max(_currentStop, newStop);
			}

			if (close <= _currentStop)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (_troughPrice == 0m || candle.LowPrice <= _troughPrice - TrailingStep)
			{
				_troughPrice = _troughPrice == 0m ? candle.LowPrice : Math.Min(_troughPrice, candle.LowPrice);
				var newStop = _troughPrice + TrailingStop;
				if (!ProfitTrailing || newStop < _entryPrice)
					_currentStop = _currentStop == 0m ? newStop : Math.Min(_currentStop, newStop);
			}

			if (_currentStop != 0m && close >= _currentStop)
				BuyMarket(-Position);
		}
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var hour = time.Hour;
		if (StartHour < StopHour)
			return hour >= StartHour && hour < StopHour;
		return hour >= StartHour || hour < StopHour;
	}
}
