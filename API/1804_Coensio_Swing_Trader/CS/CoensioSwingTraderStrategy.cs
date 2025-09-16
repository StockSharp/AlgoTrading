using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Coensio Swing Trader based on trend line breakouts.
/// </summary>
public class CoensioSwingTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<int> _trailingStepTicks;
	private readonly StrategyParam<bool> _falseBreakClose;
	private readonly StrategyParam<decimal> _buyLineSlope;
	private readonly StrategyParam<decimal> _buyLineIntercept;
	private readonly StrategyParam<decimal> _sellLineSlope;
	private readonly StrategyParam<decimal> _sellLineIntercept;

	private decimal _tick;
	private int _index;
	private decimal _entryPrice;
	private int _entryIndex;
	private decimal _trailingStopPrice;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Entry threshold in ticks.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take profit in ticks.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Enable trailing stop logic.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Trailing step in ticks.
	/// </summary>
	public int TrailingStepTicks
	{
		get => _trailingStepTicks.Value;
		set => _trailingStepTicks.Value = value;
	}

	/// <summary>
	/// Close trade if breakout fails on next candle.
	/// </summary>
	public bool FalseBreakClose
	{
		get => _falseBreakClose.Value;
		set => _falseBreakClose.Value = value;
	}

	/// <summary>
	/// Slope of buy trend line.
	/// </summary>
	public decimal BuyLineSlope
	{
		get => _buyLineSlope.Value;
		set => _buyLineSlope.Value = value;
	}

	/// <summary>
	/// Intercept of buy trend line.
	/// </summary>
	public decimal BuyLineIntercept
	{
		get => _buyLineIntercept.Value;
		set => _buyLineIntercept.Value = value;
	}

	/// <summary>
	/// Slope of sell trend line.
	/// </summary>
	public decimal SellLineSlope
	{
		get => _sellLineSlope.Value;
		set => _sellLineSlope.Value = value;
	}

	/// <summary>
	/// Intercept of sell trend line.
	/// </summary>
	public decimal SellLineIntercept
	{
		get => _sellLineIntercept.Value;
		set => _sellLineIntercept.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public CoensioSwingTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for processing", "General");

		_entryThreshold = Param(nameof(EntryThreshold), 15m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Threshold", "Extra distance beyond trend line", "Trading");

		_stopLossTicks = Param(nameof(StopLossTicks), 50)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Ticks", "Distance to stop loss", "Risk");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 100)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Ticks", "Distance to take profit", "Risk");

		_enableTrailing = Param(nameof(EnableTrailing), false)
			.SetDisplay("Enable Trailing", "Use trailing stop", "Risk");

		_trailingStepTicks = Param(nameof(TrailingStepTicks), 5)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step Ticks", "Trailing stop step", "Risk");

		_falseBreakClose = Param(nameof(FalseBreakClose), true)
			.SetDisplay("False Break Close", "Close trade on failed breakout", "Risk");

		_buyLineSlope = Param(nameof(BuyLineSlope), 0m)
			.SetDisplay("Buy Line Slope", "Slope for long trend line", "Trading");

		_buyLineIntercept = Param(nameof(BuyLineIntercept), 0m)
			.SetDisplay("Buy Line Intercept", "Intercept for long trend line", "Trading");

		_sellLineSlope = Param(nameof(SellLineSlope), 0m)
			.SetDisplay("Sell Line Slope", "Slope for short trend line", "Trading");

		_sellLineIntercept = Param(nameof(SellLineIntercept), 0m)
			.SetDisplay("Sell Line Intercept", "Intercept for short trend line", "Trading");
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

		_index = 0;
		_entryPrice = 0m;
		_entryIndex = -1;
		_trailingStopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tick = Security?.PriceStep ?? 1m;

		var stop = StopLossTicks * _tick;
		var take = TakeProfitTicks * _tick;

		StartProtection(new Unit(take), new Unit(stop), isStopTrailing: EnableTrailing);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_index++;

		var upper = BuyLineSlope * _index + BuyLineIntercept;
		var lower = SellLineSlope * _index + SellLineIntercept;
		var threshold = EntryThreshold * _tick;

		if (Position == 0 && IsFormedAndOnlineAndAllowTrading())
		{
			if (candle.ClosePrice > upper + threshold)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_entryIndex = _index;
				_trailingStopPrice = candle.ClosePrice;
			}
			else if (candle.ClosePrice < lower - threshold)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_entryIndex = _index;
				_trailingStopPrice = candle.ClosePrice;
			}
		}
		else if (Position > 0)
		{
			if (EnableTrailing)
			{
				if (candle.HighPrice > _trailingStopPrice)
					_trailingStopPrice = candle.HighPrice;

				var stop = _trailingStopPrice - TrailingStepTicks * _tick;
				if (candle.LowPrice <= stop)
				{
					SellMarket(Position);
					return;
				}
			}

			if (FalseBreakClose && _index == _entryIndex + 1 && candle.ClosePrice < _entryPrice - threshold)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (EnableTrailing)
			{
				if (_trailingStopPrice == 0m || candle.LowPrice < _trailingStopPrice)
					_trailingStopPrice = candle.LowPrice;

				var stop = _trailingStopPrice + TrailingStepTicks * _tick;
				if (candle.HighPrice >= stop)
				{
					BuyMarket(Math.Abs(Position));
					return;
				}
			}

			if (FalseBreakClose && _index == _entryIndex + 1 && candle.ClosePrice > _entryPrice + threshold)
				BuyMarket(Math.Abs(Position));
		}
	}
}

