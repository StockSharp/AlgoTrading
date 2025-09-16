using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Coensio Swing Trader based on Donchian channel breakouts with optional trailing stop and break-even.
/// </summary>
public class CoensioSwingTraderV06Strategy : Strategy
{
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _breakEvenPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<DataType> _candleType;

	private DonchianChannels _donchian;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _entryPrice;
	private bool _breakEvenSet;

	/// <summary>
	/// Initializes parameters with defaults similar to the original MQL strategy.
	/// </summary>
	public CoensioSwingTraderV06Strategy()
	{
		_channelPeriod = Param(nameof(ChannelPeriod), 20)
		.SetDisplay("Channel Period", "Period for Donchian Channel", "Indicators");

		_entryThreshold = Param(nameof(EntryThreshold), 15m)
		.SetDisplay("Entry Threshold", "Breakout threshold in pips", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 50)
		.SetDisplay("Stop Loss (pips)", "Initial stop loss in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 80)
		.SetDisplay("Take Profit (pips)", "Initial take profit in pips", "Risk");

		_breakEvenPips = Param(nameof(BreakEvenPips), 25)
		.SetDisplay("Break Even (pips)", "Move stop to entry after profit", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetDisplay("Trailing Step (pips)", "Trailing stop step", "Risk");

		_enableTrailing = Param(nameof(EnableTrailing), false)
		.SetDisplay("Enable Trailing", "Use trailing stop after break even", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <summary>Period for Donchian channel.</summary>
	public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }

	/// <summary>Threshold in pips above/below channel boundaries to trigger entries.</summary>
	public decimal EntryThreshold { get => _entryThreshold.Value; set => _entryThreshold.Value = value; }

	/// <summary>Initial stop loss in pips.</summary>
	public int StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }

	/// <summary>Initial take profit in pips.</summary>
	public int TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }

	/// <summary>Profit in pips required to move stop to entry.</summary>
	public int BreakEvenPips { get => _breakEvenPips.Value; set => _breakEvenPips.Value = value; }

	/// <summary>Step in pips for trailing stop after break-even.</summary>
	public int TrailingStepPips { get => _trailingStepPips.Value; set => _trailingStepPips.Value = value; }

	/// <summary>Enable or disable trailing stop.</summary>
	public bool EnableTrailing { get => _enableTrailing.Value; set => _enableTrailing.Value = value; }

	/// <summary>Candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }


	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_stopPrice = default;
		_takePrice = default;
		_entryPrice = default;
		_breakEvenSet = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_donchian = new DonchianChannels { Length = ChannelPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_donchian, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _donchian);
			DrawOwnTrades(area);
		}
	}


	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var dc = (DonchianChannelsValue)donchianValue;
		if (dc.UpperBand is not decimal upper || dc.LowerBand is not decimal lower)
		return;

		var step = Security.PriceStep ?? 1m;
		var threshold = EntryThreshold * step;

		var price = candle.ClosePrice;

		if (Position <= 0 && price > upper + threshold)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entryPrice = price;
			_stopPrice = price - StopLossPips * step;
			_takePrice = price + TakeProfitPips * step;
			_breakEvenSet = false;
		}
		else if (Position >= 0 && price < lower - threshold)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_entryPrice = price;
			_stopPrice = price + StopLossPips * step;
			_takePrice = price - TakeProfitPips * step;
			_breakEvenSet = false;
		}
		else if (Position > 0)
		{
			if (!_breakEvenSet && price - _entryPrice >= BreakEvenPips * step)
			{
				_stopPrice = _entryPrice;
				_breakEvenSet = true;
			}

			if (_breakEvenSet && EnableTrailing)
			{
				var newStop = price - TrailingStepPips * step;
				if (_stopPrice == null || newStop > _stopPrice)
				_stopPrice = newStop;
			}

			if (_stopPrice != null && price <= _stopPrice)
			{
				SellMarket(Position);
			}
			else if (_takePrice != null && price >= _takePrice)
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			if (!_breakEvenSet && _entryPrice - price >= BreakEvenPips * step)
			{
				_stopPrice = _entryPrice;
				_breakEvenSet = true;
			}

			if (_breakEvenSet && EnableTrailing)
			{
				var newStop = price + TrailingStepPips * step;
				if (_stopPrice == null || newStop < _stopPrice)
				_stopPrice = newStop;
			}

			if (_stopPrice != null && price >= _stopPrice)
			{
				BuyMarket(-Position);
			}
			else if (_takePrice != null && price <= _takePrice)
			{
				BuyMarket(-Position);
			}
		}
	}
}

