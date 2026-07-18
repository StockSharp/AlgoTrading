using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Compares upside/downside potential inside a rolling high-low range.
/// </summary>
public class MaxGainStrategy : Strategy
{
	private readonly StrategyParam<int> _periodLength;
	private readonly StrategyParam<decimal> _edgeMultiplier;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;
	private int _barsFromSignal;

	/// <summary>
	/// High/low lookback length.
	/// </summary>
	public int PeriodLength
	{
		get => _periodLength.Value;
		set => _periodLength.Value = value;
	}

	/// <summary>
	/// Minimum upside/downside ratio required for position change.
	/// </summary>
	public decimal EdgeMultiplier
	{
		get => _edgeMultiplier.Value;
		set => _edgeMultiplier.Value = value;
	}

	/// <summary>
	/// Minimum bars between market entries.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Candle timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MaxGainStrategy()
	{
		_periodLength = Param(nameof(PeriodLength), 64)
			.SetGreaterThanZero()
			.SetDisplay("Period Length", "Rolling high-low length", "General");

		_edgeMultiplier = Param(nameof(EdgeMultiplier), 1.25m)
			.SetGreaterThanZero()
			.SetDisplay("Edge Multiplier", "Upside/downside ratio threshold", "General");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 14)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");
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
		_highest = null;
		_lowest = null;
		_barsFromSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(null, null);

		_highest = new() { Length = PeriodLength };
		_lowest = new() { Length = PeriodLength };
		_barsFromSignal = SignalCooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highest, _lowest, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maxHigh, decimal minLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		var close = candle.ClosePrice;
		if (close <= 0m || maxHigh <= minLow)
			return;

		_barsFromSignal++;
		if (_barsFromSignal < SignalCooldownBars)
			return;

		var upside = (maxHigh - close) / close;
		var downside = (close - minLow) / close;

		if (upside > downside * EdgeMultiplier && Position <= 0)
		{
			BuyMarket();
			_barsFromSignal = 0;
		}
		else if (downside > upside * EdgeMultiplier && Position >= 0)
		{
			SellMarket();
			_barsFromSignal = 0;
		}
	}
}
