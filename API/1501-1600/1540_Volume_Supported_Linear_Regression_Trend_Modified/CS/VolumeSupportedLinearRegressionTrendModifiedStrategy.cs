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
/// Strategy enters long when RSI is above entry level and exits when RSI exceeds exit level.
/// </summary>
public class VolumeSupportedLinearRegressionTrendModifiedStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _entryLevel;
	private readonly StrategyParam<decimal> _exitLevel;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi = null!;
	private decimal? _prevRsi;
	private int _cooldownRemaining;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Entry threshold.
	/// </summary>
	public decimal EntryLevel
	{
		get => _entryLevel.Value;
		set => _entryLevel.Value = value;
	}

	/// <summary>
	/// Exit threshold.
	/// </summary>
	public decimal ExitLevel
	{
		get => _exitLevel.Value;
		set => _exitLevel.Value = value;
	}

	/// <summary>
	/// Minimum number of closed candles between new entries.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="VolumeSupportedLinearRegressionTrendModifiedStrategy"/>.
	/// </summary>
	public VolumeSupportedLinearRegressionTrendModifiedStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "General")
			;

		_entryLevel = Param(nameof(EntryLevel), 60m)
			.SetDisplay("Entry Level", "RSI value to enter long", "General")
			;

		_exitLevel = Param(nameof(ExitLevel), 45m)
			.SetDisplay("Exit Level", "RSI value to close long", "General")
			;

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 8)
			.SetNotNegative()
			.SetDisplay("Signal Cooldown Bars", "Closed candles to wait before re-entering", "General")
			;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevRsi = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_prevRsi = null;
		_cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (_prevRsi is null)
		{
			_prevRsi = rsi;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevRsi = rsi;
			return;
		}

		var previousRsi = _prevRsi.Value;
		var longEntry = previousRsi <= EntryLevel && rsi > EntryLevel;
		var longExit = previousRsi >= ExitLevel && rsi < ExitLevel;

		if (longExit && Position > 0)
		{
			SellMarket(Position);
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 && longEntry && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
			_cooldownRemaining = SignalCooldownBars;
		}

		_prevRsi = rsi;
	}
}
