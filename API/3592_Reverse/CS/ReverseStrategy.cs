namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class ReverseStrategy : Strategy
{
	// Candle subscription parameter using the high level API.
	private readonly StrategyParam<DataType> _candleType;
	// Bollinger Bands moving average length.
	private readonly StrategyParam<int> _bollingerPeriod;
	// Bollinger Bands width expressed in standard deviations.
	private readonly StrategyParam<decimal> _bollingerWidth;
	// Relative Strength Index length.
	private readonly StrategyParam<int> _rsiPeriod;
	// RSI threshold used to detect overbought conditions.
	private readonly StrategyParam<decimal> _rsiOverbought;
	// RSI threshold used to detect oversold conditions.
	private readonly StrategyParam<decimal> _rsiOversold;

	// Cached indicator instances for reuse between runs.
	private BollingerBands _bollinger;
	private RelativeStrengthIndex _rsi;

	// Previous candle context required to detect band and RSI crossovers.
	private decimal _previousClose;
	private decimal _previousLowerBand;
	private decimal _previousUpperBand;
	private decimal _previousRsi;
	private bool _hasPrevious;

	// Tracking of protective stop and take-profit prices.
	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;

	public ReverseStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for signals", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Moving average length for Bollinger Bands", "Indicators");

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Width", "Standard deviation multiplier for the bands", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of the RSI oscillator", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Upper threshold used for short signals", "Signals");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "Lower threshold used for long signals", "Signals");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();

		_bollinger = new()
		{
			Length = BollingerPeriod,
			Width = BollingerWidth,
		};

		_rsi = new()
		{
			Length = RsiPeriod,
		};

		_previousClose = 0m;
		_previousLowerBand = 0m;
		_previousUpperBand = 0m;
		_previousRsi = 0m;
		_hasPrevious = false;
		ResetTargets();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bollinger, _rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bollinger.IsFormed || !_rsi.IsFormed)
		{
			// Indicator values are not ready yet, remember the latest context only.
			StorePrevious(candle.ClosePrice, lowerBand, upperBand, rsiValue, false);
			return;
		}

		if (!_hasPrevious)
		{
			// Wait for one fully formed candle to use as historical reference.
			StorePrevious(candle.ClosePrice, lowerBand, upperBand, rsiValue, true);
			return;
		}

		if (BollingerWidth <= 0m)
			return;

		var standardDeviation = (upperBand - middleBand) / BollingerWidth;
		if (standardDeviation <= 0m)
			return;

		// Enter long when price and RSI cross up from oversold near the lower band.
		var longSignal = Position <= 0
			&& _previousClose < _previousLowerBand
			&& candle.ClosePrice >= lowerBand
			&& _previousRsi < RsiOversold
			&& rsiValue >= RsiOversold;

		// Enter short when price and RSI cross down from overbought near the upper band.
		var shortSignal = Position >= 0
			&& _previousClose > _previousUpperBand
			&& candle.ClosePrice <= upperBand
			&& _previousRsi > RsiOverbought
			&& rsiValue <= RsiOverbought;

		if (longSignal)
		{
			// Place protective stop one deviation below and target two deviations above.
			_longStop = candle.ClosePrice - standardDeviation;
			_longTarget = candle.ClosePrice + (standardDeviation * 2m);
			_shortStop = null;
			_shortTarget = null;
			BuyMarket();
		}
		else if (shortSignal)
		{
			// Place protective stop one deviation above and target two deviations below.
			_shortStop = candle.ClosePrice + standardDeviation;
			_shortTarget = candle.ClosePrice - (standardDeviation * 2m);
			_longStop = null;
			_longTarget = null;
			SellMarket();
		}

		if (Position > 0)
		{
			// Exit long positions on an upper band touch or when stop/target are reached.
			if (candle.ClosePrice >= upperBand)
			{
				SellMarket();
				ResetTargets();
			}
			else if ((_longStop.HasValue && candle.ClosePrice <= _longStop) || (_longTarget.HasValue && candle.ClosePrice >= _longTarget))
			{
				SellMarket();
				ResetTargets();
			}
		}
		else if (Position < 0)
		{
			// Exit short positions on a lower band touch or when stop/target are reached.
			if (candle.ClosePrice <= lowerBand)
			{
				BuyMarket();
				ResetTargets();
			}
			else if ((_shortStop.HasValue && candle.ClosePrice >= _shortStop) || (_shortTarget.HasValue && candle.ClosePrice <= _shortTarget))
			{
				BuyMarket();
				ResetTargets();
			}
		}

		StorePrevious(candle.ClosePrice, lowerBand, upperBand, rsiValue, true);
	}

	private void StorePrevious(decimal closePrice, decimal lowerBand, decimal upperBand, decimal rsiValue, bool hasPrevious)
	{
		_previousClose = closePrice;
		_previousLowerBand = lowerBand;
		_previousUpperBand = upperBand;
		_previousRsi = rsiValue;
		_hasPrevious = hasPrevious;
	}

	private void ResetTargets()
	{
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
	}
}
