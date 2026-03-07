using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that recreates the MetaTrader "Prop Firm Helper" expert advisor.
/// Uses Donchian channel breakouts for entry signals with market orders.
/// </summary>
public class PropFirmHelperStrategy : Strategy
{
	private readonly StrategyParam<int> _entryPeriod;
	private readonly StrategyParam<int> _exitPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalCooldownBars;

	private DonchianChannels _entryChannel;
	private DonchianChannels _exitChannel;

	private decimal _entryUpper;
	private decimal _entryLower;
	private decimal _exitLower;
	private decimal _exitUpper;
	private decimal _prevEntryUpper;
	private decimal _prevEntryLower;
	private bool _hasValues;
	private decimal _entryPrice;
	private int _cooldownRemaining;

	/// <summary>
	/// Initializes a new instance of <see cref="PropFirmHelperStrategy"/>.
	/// </summary>
	public PropFirmHelperStrategy()
	{
		_entryPeriod = Param(nameof(EntryPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Entry Period", "Number of candles used for breakout Donchian channel", "Entries");

		_exitPeriod = Param(nameof(ExitPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Exit Period", "Number of candles used for trailing Donchian channel", "Exits");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for Donchian calculations", "General");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 4)
			.SetNotNegative()
			.SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new breakout entry", "General");
	}

	/// <summary>
	/// Donchian breakout lookback length.
	/// </summary>
	public int EntryPeriod
	{
		get => _entryPeriod.Value;
		set => _entryPeriod.Value = value;
	}

	/// <summary>
	/// Donchian trailing lookback length.
	/// </summary>
	public int ExitPeriod
	{
		get => _exitPeriod.Value;
		set => _exitPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator subscriptions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryChannel = null;
		_exitChannel = null;
		_entryUpper = 0m;
		_entryLower = 0m;
		_exitLower = 0m;
		_exitUpper = 0m;
		_prevEntryUpper = 0m;
		_prevEntryLower = 0m;
		_hasValues = false;
		_entryPrice = 0m;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasValues = false;
		_cooldownRemaining = 0;

		_entryChannel = new DonchianChannels { Length = EntryPeriod };
		_exitChannel = new DonchianChannels { Length = ExitPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _entryChannel);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var entryValue = _entryChannel.Process(new CandleIndicatorValue(_entryChannel, candle));
		var exitValue = _exitChannel.Process(new CandleIndicatorValue(_exitChannel, candle));

		if (!_entryChannel.IsFormed || !_exitChannel.IsFormed)
			return;

		if (entryValue is not DonchianChannelsValue entryBands || exitValue is not DonchianChannelsValue exitBands)
			return;

		if (entryBands.UpperBand is not decimal entryUpper || entryBands.LowerBand is not decimal entryLower)
			return;

		if (exitBands.UpperBand is not decimal exitUpper || exitBands.LowerBand is not decimal exitLower)
			return;

		_prevEntryUpper = _entryUpper;
		_prevEntryLower = _entryLower;
		_entryUpper = entryUpper;
		_entryLower = entryLower;
		_exitUpper = exitUpper;
		_exitLower = exitLower;

		if (!_hasValues)
		{
			_hasValues = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ProcessSignal(candle);
	}

	private void ProcessSignal(ICandleMessage candle)
	{
		var close = candle.ClosePrice;

		// Exit logic
		if (Position > 0 && close < _exitLower)
		{
			SellMarket(Position);
			_cooldownRemaining = SignalCooldownBars;
			return;
		}
		else if (Position < 0 && close > _exitUpper)
		{
			BuyMarket(-Position);
			_cooldownRemaining = SignalCooldownBars;
			return;
		}

		// Entry logic - breakout above previous entry channel upper
		if (_cooldownRemaining == 0 && _prevEntryUpper > 0 && close > _prevEntryUpper && Position <= 0)
		{
			BuyMarket(Volume + (Position < 0 ? -Position : 0m));
			_entryPrice = close;
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 && _prevEntryLower > 0 && close < _prevEntryLower && Position >= 0)
		{
			SellMarket(Volume + (Position > 0 ? Position : 0m));
			_entryPrice = close;
			_cooldownRemaining = SignalCooldownBars;
		}
	}
}
