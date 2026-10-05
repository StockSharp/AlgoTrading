using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bounce Reversal strategy.
/// A close back above the lower Bollinger band after a close below it, with MACD above its signal line and volume at least
/// VolumeFactor times its VolumePeriod average, goes long; a close back below the upper band after a close above it, with MACD
/// below the signal and the same volume condition, goes short, reversing an opposite position. At most MaxTradesPerDay entries
/// are made per UTC day, and percent stop loss and take profit close the position.
/// </summary>
public class BollingerBounceReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bbStdDev;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<int> _volumePeriod;
	private readonly StrategyParam<decimal> _volumeFactor;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<int> _maxTradesPerDay;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeAverage;
	private decimal? _prevClose;
	private decimal? _prevUpper;
	private decimal? _prevLower;
	private DateTime _tradeDay;
	private int _tradesToday;

	/// <summary>
	/// Bollinger period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger standard deviation multiplier.
	/// </summary>
	public decimal BbStdDev
	{
		get => _bbStdDev.Value;
		set => _bbStdDev.Value = value;
	}

	/// <summary>
	/// Fast EMA period of MACD.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period of MACD.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal line period of MACD.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Candles of the average volume.
	/// </summary>
	public int VolumePeriod
	{
		get => _volumePeriod.Value;
		set => _volumePeriod.Value = value;
	}

	/// <summary>
	/// Multiple of the average volume the candle volume has to reach.
	/// </summary>
	public decimal VolumeFactor
	{
		get => _volumeFactor.Value;
		set => _volumeFactor.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Maximum entries per UTC day.
	/// </summary>
	public int MaxTradesPerDay
	{
		get => _maxTradesPerDay.Value;
		set => _maxTradesPerDay.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BollingerBounceReversalStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Bollinger period", "Bollinger");

		_bbStdDev = Param(nameof(BbStdDev), 2m)
			.SetGreaterThanZero()
			.SetDisplay("BB StdDev", "Bollinger standard deviation multiplier", "Bollinger");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period of MACD", "MACD");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period of MACD", "MACD");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line period of MACD", "MACD");

		_volumePeriod = Param(nameof(VolumePeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume Period", "Candles of the average volume", "Volume");

		_volumeFactor = Param(nameof(VolumeFactor), 1m)
			.SetNotNegative()
			.SetDisplay("Volume Factor", "Multiple of the average volume the candle volume has to reach", "Volume");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk");

		_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades Per Day", "Maximum entries per UTC day", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	private void ResetState()
	{
		_volumeAverage = null;
		_prevClose = null;
		_prevUpper = null;
		_prevLower = null;
		_tradeDay = default;
		_tradesToday = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		_volumeAverage = new SimpleMovingAverage { Length = VolumePeriod };
		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = BbStdDev };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
			},
			SignalMa = { Length = MacdSignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, macd, ProcessCandle)
			.Start();

		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, macd);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeValue = _volumeAverage.Process(new DecimalIndicatorValue(_volumeAverage, candle.TotalVolume, candle.OpenTime) { IsFinal = true });

		if (!bollingerValue.IsFormed)
			return;

		var bands = (BollingerBandsValue)bollingerValue;

		if (bands.UpBand is not decimal upper || bands.LowBand is not decimal lower)
			return;

		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		var prevUpper = _prevUpper;
		var prevLower = _prevLower;
		_prevClose = close;
		_prevUpper = upper;
		_prevLower = lower;

		if (!macdValue.IsFormed || !volumeValue.IsFormed || prevClose is not decimal pc || prevUpper is not decimal pu || prevLower is not decimal pl)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var day = candle.OpenTime.Date;

		if (day != _tradeDay)
		{
			_tradeDay = day;
			_tradesToday = 0;
		}

		if (_tradesToday >= MaxTradesPerDay)
			return;

		var volumeOk = candle.TotalVolume >= volumeValue.GetValue<decimal>() * VolumeFactor;

		if (pc < pl && close > lower && macd > signal && volumeOk && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_tradesToday++;
		}
		else if (pc > pu && close < upper && macd < signal && volumeOk && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_tradesToday++;
		}
	}
}
