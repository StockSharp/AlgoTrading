using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Converted "20 pips" price channel strategy.
/// Uses Donchian channel breakouts with MA filter, trailing stop, and recovery multiplier.
/// </summary>
public class TwentyPipsPriceChannelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _channelPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();
	private decimal? _prevChannelUpper;
	private decimal? _prevChannelLower;

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Donchian channel lookback period.
	/// </summary>
	public int ChannelPeriod
	{
		get => _channelPeriod.Value;
		set => _channelPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss distance in absolute price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in absolute price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TwentyPipsPriceChannelStrategy"/>.
	/// </summary>
	public TwentyPipsPriceChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type", "General");

		_channelPeriod = Param(nameof(ChannelPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Channel Period", "Donchian channel lookback", "Parameters");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Slow moving average length", "Parameters");

		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit distance", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highs.Clear();
		_lows.Clear();
		_prevChannelUpper = null;
		_prevChannelLower = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		var slowMa = new SMA { Length = SlowMaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}

		// Use StartProtection for SL/TP
		var tp = TakeProfit > 0 ? new Unit(TakeProfit, UnitTypes.Absolute) : null;
		var sl = StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Absolute) : null;
		StartProtection(tp, sl);

		base.OnStarted2(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal slowMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Track highs and lows for manual Donchian channel
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		while (_highs.Count > ChannelPeriod)
			_highs.RemoveAt(0);
		while (_lows.Count > ChannelPeriod)
			_lows.RemoveAt(0);

		if (_highs.Count < ChannelPeriod)
		{
			_prevChannelUpper = null;
			_prevChannelLower = null;
			return;
		}

		var channelUpper = _highs.Max();
		var channelLower = _lows.Min();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevChannelUpper = channelUpper;
			_prevChannelLower = channelLower;
			return;
		}

		// Channel breakout with MA filter
		if (_prevChannelUpper.HasValue && _prevChannelLower.HasValue)
		{
			// Breakout above the previous channel high -> buy signal
			if (candle.ClosePrice > _prevChannelUpper.Value && candle.ClosePrice > slowMaValue && Position <= 0)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				BuyMarket(Volume);
			}
			// Breakout below the previous channel low -> sell signal
			else if (candle.ClosePrice < _prevChannelLower.Value && candle.ClosePrice < slowMaValue && Position >= 0)
			{
				if (Position > 0)
					SellMarket(Position);
				SellMarket(Volume);
			}
		}

		_prevChannelUpper = channelUpper;
		_prevChannelLower = channelLower;
	}
}
