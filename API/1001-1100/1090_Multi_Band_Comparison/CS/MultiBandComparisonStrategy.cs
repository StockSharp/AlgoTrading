namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Multi-band comparison strategy that enters on an upper volatility band breakout
/// and exits on a return to the middle band.
/// </summary>
public class MultiBandComparisonStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _entryConfirmBars;
	private readonly StrategyParam<int> _exitConfirmBars;
	private readonly StrategyParam<int> _signalCooldownBars;

	private int _entryCounter;
	private int _exitCounter;
	private int _cooldownRemaining;
	private bool _wasAboveEntryLevel;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// SMA period.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Volatility multiplier used for the breakout band.
	/// </summary>
	public decimal BollingerMultiplier
	{
		get => _bollingerMultiplier.Value;
		set => _bollingerMultiplier.Value = value;
	}

	/// <summary>
	/// Bars required for entry confirmation.
	/// </summary>
	public int EntryConfirmBars
	{
		get => _entryConfirmBars.Value;
		set => _entryConfirmBars.Value = value;
	}

	/// <summary>
	/// Bars required for exit confirmation.
	/// </summary>
	public int ExitConfirmBars
	{
		get => _exitConfirmBars.Value;
		set => _exitConfirmBars.Value = value;
	}

	/// <summary>
	/// Bars to wait before accepting a new signal.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MultiBandComparisonStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_length = Param(nameof(Length), 40)
			.SetDisplay("Length", "SMA period", "Bands")
			.SetGreaterThanZero();

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 1.1m)
			.SetDisplay("BB Mult", "Volatility multiplier for the breakout band", "Bands")
			.SetGreaterThanZero();

		_entryConfirmBars = Param(nameof(EntryConfirmBars), 1)
			.SetDisplay("Entry Confirm Bars", "Bars for entry confirmation", "Trading")
			.SetGreaterThanZero();

		_exitConfirmBars = Param(nameof(ExitConfirmBars), 1)
			.SetDisplay("Exit Confirm Bars", "Bars for exit confirmation", "Trading")
			.SetGreaterThanZero();

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 8)
			.SetDisplay("Signal Cooldown", "Bars to wait before accepting a new signal", "Trading")
			.SetGreaterThanZero();
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

		_entryCounter = 0;
		_exitCounter = 0;
		_cooldownRemaining = 0;
		_wasAboveEntryLevel = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SMA { Length = Length };
		var std = new StandardDeviation { Length = Length };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(sma, std, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (stdValue <= 0m)
			return;

		var entryLevel = smaValue + stdValue * BollingerMultiplier;
		var exitLevel = smaValue;
		var isAboveEntryLevel = candle.ClosePrice > entryLevel;
		var crossedUp = !_wasAboveEntryLevel && isAboveEntryLevel;
		var crossedDown = _wasAboveEntryLevel && candle.ClosePrice < exitLevel;

		_entryCounter = crossedUp ? _entryCounter + 1 : 0;
		_exitCounter = crossedDown ? _exitCounter + 1 : 0;

		if (Position <= 0 && _cooldownRemaining == 0 && _entryCounter >= EntryConfirmBars)
		{
			BuyMarket();
			_entryCounter = 0;
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (Position > 0 && _exitCounter >= ExitConfirmBars)
		{
			SellMarket(Position);
			_exitCounter = 0;
			_cooldownRemaining = SignalCooldownBars;
		}

		_wasAboveEntryLevel = isAboveEntryLevel;
	}
}
