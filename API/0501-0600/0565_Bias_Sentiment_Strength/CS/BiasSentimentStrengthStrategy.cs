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

/// <summary>Complete MACD, RSI, smoothed stochastic, AO, Alligator and volume bias with native protection.</summary>
public class BiasSentimentStrengthStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<int> _stochSmooth;
	private readonly StrategyParam<int> _stochD;
	private readonly StrategyParam<int> _aoShort;
	private readonly StrategyParam<int> _aoLong;
	private readonly StrategyParam<int> _volumeLength;
	private readonly StrategyParam<decimal> _bassThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private SimpleMovingAverage _smoothK, _smoothD;
	private Order _pending;

	/// <summary>Fast MACD EMA length.</summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>Slow MACD EMA length.</summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>MACD signal EMA length.</summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>Wilder RSI length.</summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>Raw stochastic high-low window.</summary>
	public int StochK
	{
		get => _stochK.Value;
		set => _stochK.Value = value;
	}

	/// <summary>Stochastic K smoothing length.</summary>
	public int StochSmooth
	{
		get => _stochSmooth.Value;
		set => _stochSmooth.Value = value;
	}

	/// <summary>Smoothed stochastic D length.</summary>
	public int StochD
	{
		get => _stochD.Value;
		set => _stochD.Value = value;
	}

	/// <summary>Short median-price SMA length.</summary>
	public int AoShort
	{
		get => _aoShort.Value;
		set => _aoShort.Value = value;
	}

	/// <summary>Long median-price SMA length.</summary>
	public int AoLong
	{
		get => _aoLong.Value;
		set => _aoLong.Value = value;
	}

	/// <summary>Close VWMA and SMA length.</summary>
	public int VolumeLength
	{
		get => _volumeLength.Value;
		set => _volumeLength.Value = value;
	}

	/// <summary>Minimum absolute aggregate bias.</summary>
	public decimal BassThreshold
	{
		get => _bassThreshold.Value;
		set => _bassThreshold.Value = value;
	}

	/// <summary>Native filled-entry percentage stop.</summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>Finished signal candle interval.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Latest fully formed aggregate bias.</summary>
	public decimal? LastBass { get; private set; }

	/// <summary>Initializes the published indicator lengths, zero bias threshold and percent stop.</summary>
	public BiasSentimentStrengthStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12).SetGreaterThanZero()
			.SetDisplay("MacdFast", "Fast MACD EMA length", "Parameters");
		_macdSlow = Param(nameof(MacdSlow), 26).SetGreaterThanZero()
			.SetDisplay("MacdSlow", "Slow MACD EMA length", "Parameters");
		_macdSignal = Param(nameof(MacdSignal), 9).SetGreaterThanZero()
			.SetDisplay("MacdSignal", "MACD signal EMA length", "Parameters");
		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetGreaterThanZero()
			.SetDisplay("RsiPeriod", "Wilder RSI length", "Parameters");
		_stochK = Param(nameof(StochK), 21).SetGreaterThanZero()
			.SetDisplay("StochK", "Raw stochastic high-low window", "Parameters");
		_stochSmooth = Param(nameof(StochSmooth), 14).SetGreaterThanZero()
			.SetDisplay("StochSmooth", "Stochastic K smoothing length", "Parameters");
		_stochD = Param(nameof(StochD), 14).SetGreaterThanZero()
			.SetDisplay("StochD", "Smoothed stochastic D length", "Parameters");
		_aoShort = Param(nameof(AoShort), 5).SetGreaterThanZero()
			.SetDisplay("AoShort", "Short median-price SMA length", "Parameters");
		_aoLong = Param(nameof(AoLong), 34).SetGreaterThanZero()
			.SetDisplay("AoLong", "Long median-price SMA length", "Parameters");
		_volumeLength = Param(nameof(VolumeLength), 30).SetGreaterThanZero()
			.SetDisplay("VolumeLength", "Close VWMA and SMA length", "Parameters");
		_bassThreshold = Param(nameof(BassThreshold), 0m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("BassThreshold", "Minimum absolute aggregate bias", "Parameters");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetGreaterThanZero()
			.SetDisplay("StopLossPercent", "Native filled-entry percentage stop", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("CandleType", "Finished signal candle interval", "Parameters");
		OrderRegistering += order => _pending = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_smoothK = _smoothD = null;
		_pending = null;
		LastBass = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (MacdFast >= MacdSlow || AoShort >= AoLong)
			throw new InvalidOperationException("Fast MACD and AO lengths must be shorter than their slow lengths.");
		base.OnStarted2(time);
		_smoothK = new SimpleMovingAverage { Length = StochSmooth };
		_smoothD = new SimpleMovingAverage { Length = StochD };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd = { ShortMa = { Length = MacdFast }, LongMa = { Length = MacdSlow } },
			SignalMa = { Length = MacdSignal },
		};
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var stochastic = new StochasticK { Length = StochK };
		var ao = new AwesomeOscillator { ShortMa = { Length = AoShort }, LongMa = { Length = AoLong } };
		var vwma = new VolumeWeightedMovingAverage { Length = VolumeLength };
		var sma = new SimpleMovingAverage { Length = VolumeLength };
		var alligator = new Alligator();
		StartProtection(new Unit(), StopLossPercent.Percents(), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quote = new Subscription(DataType.Level1, Security);
			quote.MarketData.BuildField = field;
			SubscribeLevel1(quote).Start();
		}
		var candles = SubscribeCandles(CandleType);
		candles.BindEx([macd, rsi, stochastic, ao, vwma, sma, alligator], ProcessCandle).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, candles);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished || !values[2].IsFormed || values[2].IsEmpty)
			return;
		var smoothed = _smoothK.Process(new DecimalIndicatorValue(_smoothK, values[2].GetValue<decimal>(), candle.OpenTime) { IsFinal = true });
		if (!_smoothK.IsFormed) return;
		var averageD = _smoothD.Process(new DecimalIndicatorValue(_smoothD, smoothed.GetValue<decimal>(), candle.OpenTime) { IsFinal = true });
		if (!_smoothD.IsFormed || !values.All(v => v.IsFormed && !v.IsEmpty)
			|| values[0] is not IMovingAverageConvergenceDivergenceSignalValue macd
			|| macd.Macd is not decimal line || macd.Signal is not decimal signal
			|| values[6] is not IAlligatorValue gator || gator.Jaw is not decimal jaw
			|| gator.Teeth is not decimal teeth || gator.Lips is not decimal lips)
			return;
		var bass = (line - signal) * 2m + (values[1].GetValue<decimal>() - 50m) / 5m
			+ (smoothed.GetValue<decimal>() - averageD.GetValue<decimal>()) * 0.15m
			+ values[3].GetValue<decimal>() * 0.6m + (lips - teeth) + (teeth - jaw)
			+ values[4].GetValue<decimal>() - values[5].GetValue<decimal>();
		LastBass = bass;
		if (!IsFormedAndOnlineAndAllowTrading() || _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (bass > BassThreshold && Position <= 0m)
			BuyMarket(Volume + Math.Abs(Position));
		else if (bass < -BassThreshold && Position >= 0m)
			SellMarket(Volume + Math.Abs(Position));
	}
}
