using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the SilverTrend indicator to trade trend reversals.
/// </summary>
public class SilverTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _ssp;
	private readonly StrategyParam<int> _risk;
	private readonly StrategyParam<DataType> _candleType;

	private SilverTrendSignalIndicator _indicator = null!;

	/// <summary>
	/// Indicator lookback period.
	/// </summary>
	public int Ssp
	{
		get => _ssp.Value;
		set => _ssp.Value = value;
	}

	/// <summary>
	/// Risk factor controlling channel width.
	/// </summary>
	public int Risk
	{
		get => _risk.Value;
		set => _risk.Value = value;
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
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SilverTrendStrategy()
	{
		_ssp = Param(nameof(Ssp), 9)
			.SetGreaterThanZero()
			.SetDisplay("SSP", "Lookback length for price channel", "Indicator");

		_risk = Param(nameof(Risk), 3)
			.SetGreaterThanZero()
			.SetDisplay("Risk", "Risk factor used to tighten the channel", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator", "General");
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

		_indicator = new SilverTrendSignalIndicator
		{
			Ssp = Ssp,
			Risk = Risk
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_indicator, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal signal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (signal > 0m && Position <= 0)
		{
			// Reverse to long when buy signal appears
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (signal < 0m && Position >= 0)
		{
			// Reverse to short when sell signal appears
			SellMarket(Volume + Math.Abs(Position));
		}
	}

	private class SilverTrendSignalIndicator : Indicator<ICandleMessage>
	{
		public int Ssp { get; set; } = 9;
		public int Risk { get; set; } = 3;

		private readonly SimpleMovingAverage _range = new();
		private readonly Highest _highest = new();
		private readonly Lowest _lowest = new();
		private bool? _uptrend;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();

			_range.Length = Ssp + 1;
			_highest.Length = Ssp;
			_lowest.Length = Ssp;

			var range = _range.Process(candle.HighPrice - candle.LowPrice).GetValue<decimal>();
			var maxHigh = _highest.Process(candle.HighPrice).GetValue<decimal>();
			var minLow = _lowest.Process(candle.LowPrice).GetValue<decimal>();

			var k = 33 - Risk;
			var smin = minLow + (maxHigh - minLow) * k / 100m;
			var smax = maxHigh - (maxHigh - minLow) * k / 100m;

			var uptrend = _uptrend ?? false;

			if (candle.ClosePrice < smin)
				uptrend = false;
			else if (candle.ClosePrice > smax)
				uptrend = true;

			decimal signal = 0m;
			if (_uptrend is not null && uptrend != _uptrend)
				signal = uptrend ? 1m : -1m;

			_uptrend = uptrend;
			return new DecimalIndicatorValue(this, signal, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_range.Reset();
			_highest.Reset();
			_lowest.Reset();
			_uptrend = null;
		}
	}
}
