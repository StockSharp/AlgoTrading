namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Intraday strategy based on moving average slope changes and RSI.
/// </summary>
public class IntradayBetaStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _volatilityThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _ma10;
	private SimpleMovingAverage _ma20;
	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;

	private decimal _prevMa10;
	private decimal _prevSlope;
	private decimal _prevCandleDiff;
	private decimal _longStop;
	private decimal _shortStop;
	private decimal _entryPrice;

	/// <summary>
	/// Constructor.
	/// </summary>
	public IntradayBetaStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period of the RSI indicator", "Parameters");

		_trailingStop = Param(nameof(TrailingStop), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop size in price units", "Parameters");

		_volatilityThreshold = Param(nameof(VolatilityThreshold), 20m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Threshold", "Maximum ATR value to allow trading", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <summary>
	/// Period of the RSI indicator.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Trailing stop size in price units.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Maximum ATR value to allow trading.
	/// </summary>
	public decimal VolatilityThreshold
	{
		get => _volatilityThreshold.Value;
		set => _volatilityThreshold.Value = value;
	}

	/// <summary>
	/// Type of candles to analyze.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		StartProtection();

		_ma10 = new SimpleMovingAverage { Length = 10 };
		_ma20 = new SimpleMovingAverage { Length = 20 };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_atr = new AverageTrueRange { Length = 30 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma10, _ma20, _rsi, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma10);
			DrawIndicator(area, _ma20);
			DrawIndicator(area, _rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma10Value, decimal ma20Value, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ma10.IsFormed || !_ma20.IsFormed || !_rsi.IsFormed || !_atr.IsFormed)
			return;

		// Skip trading when volatility is too high
		if (atrValue > VolatilityThreshold)
			return;

		var ma10Slope = ma10Value - _prevMa10;
		var candleDiff = candle.ClosePrice - candle.OpenPrice;

		var sellSignal = ma10Slope < 0 && _prevSlope > 0 && rsiValue >= 30m && _prevCandleDiff < 0m;
		var buySignal = ma10Slope > 0 && _prevSlope < 0 && rsiValue <= 70m && _prevCandleDiff > 0m;

		if (sellSignal && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_shortStop = _entryPrice + TrailingStop;
		}
		else if (buySignal && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_longStop = _entryPrice - TrailingStop;
		}

		if (Position > 0)
		{
			// Update trailing stop for long position
			var newStop = candle.ClosePrice - TrailingStop;
			if (newStop > _longStop && candle.ClosePrice > _entryPrice)
				_longStop = newStop;

			if (candle.LowPrice <= _longStop)
				SellMarket();
		}
		else if (Position < 0)
		{
			// Update trailing stop for short position
			var newStop = candle.ClosePrice + TrailingStop;
			if (newStop < _shortStop && candle.ClosePrice < _entryPrice)
				_shortStop = newStop;

			if (candle.HighPrice >= _shortStop)
				BuyMarket();
		}

		_prevMa10 = ma10Value;
		_prevSlope = ma10Slope;
		_prevCandleDiff = candleDiff;
	}
}
