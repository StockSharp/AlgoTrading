using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Balance of Power histogram strategy that trades confirmed zero-line reversals.
/// </summary>
public class BalanceOfPowerHistogramStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _signalLevel;
	private readonly StrategyParam<int> _cooldownCandles;

	private int _barsSinceSignal;
	private decimal? _prevBop;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum Balance of Power value required for a signal.
	/// </summary>
	public decimal SignalLevel
	{
		get => _signalLevel.Value;
		set => _signalLevel.Value = value;
	}

	/// <summary>
	/// Minimum number of finished candles between entries.
	/// </summary>
	public int CooldownCandles
	{
		get => _cooldownCandles.Value;
		set => _cooldownCandles.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BalanceOfPowerHistogramStrategy"/>.
	/// </summary>
	public BalanceOfPowerHistogramStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_signalLevel = Param(nameof(SignalLevel), 0.30m)
			.SetDisplay("Signal Level", "Minimum BOP value for confirmed reversals", "Signal");
		_cooldownCandles = Param(nameof(CooldownCandles), 3)
			.SetDisplay("Cooldown Candles", "Minimum finished candles between entries", "Signal");
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

		_barsSinceSignal = CooldownCandles;
		_prevBop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_barsSinceSignal = CooldownCandles;
		_prevBop = null;

		var bop = new BalanceOfPower();
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(bop, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal bop)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevBop = _prevBop;
		_prevBop = bop;
		_barsSinceSignal++;

		if (!IsFormedAndOnlineAndAllowTrading() || prevBop is null)
			return;

		if (_barsSinceSignal < CooldownCandles)
			return;

		var turnedUp = prevBop <= -SignalLevel && bop >= SignalLevel;
		var turnedDown = prevBop >= SignalLevel && bop <= -SignalLevel;

		if (turnedUp && Position <= 0)
		{
			BuyMarket();
			_barsSinceSignal = 0;
		}
		else if (turnedDown && Position >= 0)
		{
			SellMarket();
			_barsSinceSignal = 0;
		}
	}
}
