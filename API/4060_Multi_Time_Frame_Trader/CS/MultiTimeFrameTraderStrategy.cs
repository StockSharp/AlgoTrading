using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi time-frame linear regression channel strategy converted from the MetaTrader expert.
/// </summary>
public class MultiTimeFrameTraderStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableTrading;
	private readonly StrategyParam<int> _barsToCount;
	private readonly StrategyParam<decimal> _volume;

	private RegressionChannelState _m1Channel = null!;
	private RegressionChannelState _m5Channel = null!;
	private RegressionChannelState _m15Channel = null!;
	private RegressionChannelState _m30Channel = null!;
	private RegressionChannelState _h1Channel = null!;
	private RegressionChannelState _h4Channel = null!;
	private RegressionChannelState _d1Channel = null!;
	private RegressionChannelState _w1Channel = null!;
	private RegressionChannelState _mn1Channel = null!;
	private readonly List<RegressionChannelState> _channels = new();

	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;

	private decimal _lastM1High;
	private decimal _lastM1Low;
	private decimal _lastM5High;
	private decimal _lastM5Low;
	private bool _hasM1Data;
	private bool _hasM5Data;

	/// <summary>
	/// Initializes a new instance of the <see cref="MultiTimeFrameTraderStrategy"/> class.
	/// </summary>
	public MultiTimeFrameTraderStrategy()
	{
		_enableTrading = Param(nameof(EnableTrading), true)
		.SetDisplay("Enable Trading", "Allow strategy to send orders", "General");

		_barsToCount = Param(nameof(BarsToCount), 50)
		.SetGreaterThanZero()
		.SetDisplay("Bars To Count", "Regression lookback for every timeframe", "Regression")
		.SetCanOptimize(true)
		.SetOptimize(20, 100, 10);

		_volume = Param(nameof(Volume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume in lots", "Trading");
	}

	/// <summary>
	/// Enable or disable order generation.
	/// </summary>
	public bool EnableTrading
	{
		get => _enableTrading.Value;
		set => _enableTrading.Value = value;
	}

	/// <summary>
	/// Regression length shared by all channel calculations.
	/// </summary>
	public int BarsToCount
	{
		get => _barsToCount.Value;
		set
		{
			_barsToCount.Value = value;
			UpdateChannelLengths();
		}
	}

	/// <summary>
	/// Order volume used for entries and exits.
	/// </summary>
	public decimal TradeVolume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
		(Security, TimeSpan.FromMinutes(1).TimeFrame()),
		(Security, TimeSpan.FromMinutes(5).TimeFrame()),
		(Security, TimeSpan.FromMinutes(15).TimeFrame()),
		(Security, TimeSpan.FromMinutes(30).TimeFrame()),
		(Security, TimeSpan.FromHours(1).TimeFrame()),
		(Security, TimeSpan.FromHours(4).TimeFrame()),
		(Security, TimeSpan.FromDays(1).TimeFrame()),
		(Security, TimeSpan.FromDays(7).TimeFrame()),
		(Security, TimeSpan.FromDays(30).TimeFrame())
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_channels.Clear();
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
		_lastM1High = 0m;
		_lastM1Low = 0m;
		_lastM5High = 0m;
		_lastM5Low = 0m;
		_hasM1Data = false;
		_hasM5Data = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_m1Channel = new RegressionChannelState("M1", TimeSpan.FromMinutes(1));
		_m5Channel = new RegressionChannelState("M5", TimeSpan.FromMinutes(5));
		_m15Channel = new RegressionChannelState("M15", TimeSpan.FromMinutes(15));
		_m30Channel = new RegressionChannelState("M30", TimeSpan.FromMinutes(30));
		_h1Channel = new RegressionChannelState("H1", TimeSpan.FromHours(1));
		_h4Channel = new RegressionChannelState("H4", TimeSpan.FromHours(4));
		_d1Channel = new RegressionChannelState("D1", TimeSpan.FromDays(1));
		_w1Channel = new RegressionChannelState("W1", TimeSpan.FromDays(7));
		_mn1Channel = new RegressionChannelState("MN1", TimeSpan.FromDays(30));

		_channels.AddRange(new[]
		{
			_m1Channel,
			_m5Channel,
			_m15Channel,
			_m30Channel,
			_h1Channel,
			_h4Channel,
			_d1Channel,
			_w1Channel,
			_mn1Channel
		});

		UpdateChannelLengths();

		SubscribeCandles(_m1Channel.CandleType).Bind(ProcessM1Candle).Start();
		SubscribeCandles(_m5Channel.CandleType).Bind(ProcessM5Candle).Start();
		SubscribeCandles(_m15Channel.CandleType).Bind(ProcessHigherTimeframeCandle).Start();
		SubscribeCandles(_m30Channel.CandleType).Bind(ProcessHigherTimeframeCandle).Start();
		SubscribeCandles(_h1Channel.CandleType).Bind(ProcessHigherTimeframeCandle).Start();
		SubscribeCandles(_h4Channel.CandleType).Bind(ProcessHigherTimeframeCandle).Start();
		SubscribeCandles(_d1Channel.CandleType).Bind(ProcessHigherTimeframeCandle).Start();
		SubscribeCandles(_w1Channel.CandleType).Bind(ProcessHigherTimeframeCandle).Start();
		SubscribeCandles(_mn1Channel.CandleType).Bind(ProcessHigherTimeframeCandle).Start();
	}

	private void ProcessM1Candle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_m1Channel.Process(candle, BarsToCount))
		return;

		_lastM1High = candle.HighPrice;
		_lastM1Low = candle.LowPrice;
		_hasM1Data = true;

		ManageOpenPosition(candle);
		TryOpenPosition(candle);
	}

	private void ProcessM5Candle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_m5Channel.Process(candle, BarsToCount))
		return;

		_lastM5High = candle.HighPrice;
		_lastM5Low = candle.LowPrice;
		_hasM5Data = true;
	}

	private void ProcessHigherTimeframeCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		foreach (var channel in _channels)
		{
			if (channel.CandleType != candle.DataType)
			continue;

			channel.Process(candle, BarsToCount);
			break;
		}
	}

	private void TryOpenPosition(ICandleMessage candle)
	{
		if (!EnableTrading || Position != 0m)
		return;

		if (!_hasM1Data || !_hasM5Data)
		return;

		if (!_m1Channel.IsReady || !_m5Channel.IsReady || !_h1Channel.IsReady)
		return;

		var h1Slope = _h1Channel.Slope;
		// A negative slope on the H1 channel points to a higher-timeframe downtrend.

		if (h1Slope < 0m &&
		_lastM5High >= _m5Channel.Upper &&
		_lastM1High >= _m1Channel.Upper)
		{
			// Price is testing resistance on both M5 and M1 within a downtrend.
			var stopBuffer = (_m5Channel.Upper - _m5Channel.Line) / 2m;
			if (stopBuffer <= 0m)
			return;

			var entryPrice = candle.ClosePrice;
			SellMarket(TradeVolume);
			_shortStop = entryPrice + stopBuffer;
			_shortTarget = _m5Channel.Line;
			_longStop = null;
			_longTarget = null;
		}
		else if (h1Slope > 0m &&
		_lastM5Low <= _m5Channel.Lower &&
		_lastM1Low <= _m1Channel.Lower)
		{
			// Price is probing support on both M5 and M1 while the H1 trend is bullish.
			var stopBuffer = (_m5Channel.Line - _m5Channel.Lower) / 2m;
			if (stopBuffer <= 0m)
			return;

			var entryPrice = candle.ClosePrice;
			BuyMarket(TradeVolume);
			_longStop = entryPrice - stopBuffer;
			_longTarget = _m5Channel.Line;
			_shortStop = null;
			_shortTarget = null;
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			// Exit long trades when the protective stop or target is touched on M1 candles.
			if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				ClosePosition();
				ResetStops();
				return;
			}

			if (_longTarget is decimal target && candle.HighPrice >= target)
			{
				ClosePosition();
				ResetStops();
			}
		}
		else if (Position < 0m)
		{
			// Exit short trades when the protective stop or profit target is triggered.
			if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				ClosePosition();
				ResetStops();
				return;
			}

			if (_shortTarget is decimal target && candle.LowPrice <= target)
			{
				ClosePosition();
				ResetStops();
			}
		}
		else
		{
			ResetStops();
		}
	}

	private void ResetStops()
	{
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
	}

	private void UpdateChannelLengths()
	{
		foreach (var channel in _channels)
		channel.UpdateLength(BarsToCount);
	}

	private sealed class RegressionChannelState
	{
		private readonly LinearRegression _regression;
		private readonly Highest _highestDeviation;
		private readonly Lowest _lowestDeviation;

		public RegressionChannelState(string name, TimeSpan frame)
		{
			CandleType = frame.TimeFrame();
			_regression = new LinearRegression();
			_highestDeviation = new Highest();
			_lowestDeviation = new Lowest();
		}

	public DataType CandleType { get; }

	public decimal Upper { get; private set; }

	public decimal Lower { get; private set; }

	public decimal Line { get; private set; }

	public decimal Slope { get; private set; }

	public bool IsReady { get; private set; }

		public bool Process(ICandleMessage candle, int length)
		{
			UpdateLength(length);

			var value = _regression.Process(candle.ClosePrice);

			var typed = (LinearRegressionValue)value;
			if (typed.LinearReg is not decimal line ||
			typed.LinearRegSlope is not decimal slope)
			{
				IsReady = false;
				return false;
			}

			var deviation = candle.ClosePrice - line;

			var highValue = _highestDeviation.Process(deviation);
			var lowValue = _lowestDeviation.Process(deviation);

			if (!_highestDeviation.IsFormed || !_lowestDeviation.IsFormed)
			{
				IsReady = false;
				return false;
			}

			var highDiff = highValue.ToDecimal();
			var lowDiff = lowValue.ToDecimal();
			var maxDeviation = Math.Max(highDiff, Math.Abs(lowDiff));

			Upper = line + maxDeviation;
			Lower = line - maxDeviation;
			Line = line;
			Slope = slope;
			IsReady = true;
			return true;
		}

		public void UpdateLength(int length)
		{
			_regression.Length = length;
			_highestDeviation.Length = length;
			_lowestDeviation.Length = length;
		}
	}
}