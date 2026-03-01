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

	private decimal _entryUpper;
	private decimal _entryLower;
	private decimal _exitLower;
	private decimal _exitUpper;
	private decimal _prevEntryUpper;
	private decimal _prevEntryLower;
	private bool _hasValues;
	private decimal _entryPrice;

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

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for Donchian calculations", "General");
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

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasValues = false;

		var entryChannel = new DonchianChannels { Length = EntryPeriod };
		var exitChannel = new DonchianChannels { Length = ExitPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(entryChannel, OnEntryChannel);

		subscription
			.BindEx(exitChannel, OnExitChannel)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, entryChannel);
			DrawOwnTrades(area);
		}
	}

	private void OnEntryChannel(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (value is not DonchianChannelsValue bands)
			return;

		if (bands.UpperBand is not decimal upper || bands.LowerBand is not decimal lower)
			return;

		_prevEntryUpper = _entryUpper;
		_prevEntryLower = _entryLower;
		_entryUpper = upper;
		_entryLower = lower;
	}

	private void OnExitChannel(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (value is not DonchianChannelsValue bands)
			return;

		if (bands.UpperBand is not decimal upper || bands.LowerBand is not decimal lower)
			return;

		_exitUpper = upper;
		_exitLower = lower;

		if (!_hasValues)
		{
			_hasValues = true;
			return;
		}

		ProcessSignal(candle);
	}

	private void ProcessSignal(ICandleMessage candle)
	{
		var close = candle.ClosePrice;

		// Exit logic
		if (Position > 0 && close < _exitLower)
		{
			SellMarket();
			return;
		}
		else if (Position < 0 && close > _exitUpper)
		{
			BuyMarket();
			return;
		}

		// Entry logic - breakout above previous entry channel upper
		if (_prevEntryUpper > 0 && close > _prevEntryUpper && Position <= 0)
		{
			BuyMarket();
			_entryPrice = close;
		}
		else if (_prevEntryLower > 0 && close < _prevEntryLower && Position >= 0)
		{
			SellMarket();
			_entryPrice = close;
		}
	}
}
