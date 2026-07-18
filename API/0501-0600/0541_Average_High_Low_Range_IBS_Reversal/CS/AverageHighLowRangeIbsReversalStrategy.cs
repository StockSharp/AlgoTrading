using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Average high-low range with IBS reversal strategy.
/// Uses Highest/Lowest channel with EMA filter and cooldown.
/// Buys on breakout above channel with uptrend, sells on breakdown below with downtrend.
/// </summary>
public class AverageHighLowRangeIbsReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHighest;
	private decimal _prevLowest;
	private int _barIndex;
	private int _lastTradeBar;

	/// <summary>
	/// Channel lookback length.
	/// </summary>
	public int ChannelLength
	{
		get => _channelLength.Value;
		set => _channelLength.Value = value;
	}

	/// <summary>
	/// EMA trend filter period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AverageHighLowRangeIbsReversalStrategy()
	{
		_channelLength = Param(nameof(ChannelLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Channel Length", "Lookback for Highest/Lowest", "Indicators");

		_emaLength = Param(nameof(EmaLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA trend filter period", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 350)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevHighest = 0;
		_prevLowest = 0;
		_barIndex = 0;
		_lastTradeBar = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = ChannelLength };
		var lowest = new Lowest { Length = ChannelLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var cooldownOk = _barIndex - _lastTradeBar > CooldownBars;

		// Breakout above previous highest with uptrend
		var breakUp = _prevHighest > 0 && candle.ClosePrice > _prevHighest && candle.ClosePrice > emaValue;
		// Breakdown below previous lowest with downtrend
		var breakDown = _prevLowest > 0 && candle.ClosePrice < _prevLowest && candle.ClosePrice < emaValue;

		if (breakUp && Position <= 0 && cooldownOk)
		{
			BuyMarket();
			_lastTradeBar = _barIndex;
		}
		else if (breakDown && Position >= 0 && cooldownOk)
		{
			SellMarket();
			_lastTradeBar = _barIndex;
		}

		_prevHighest = highestValue;
		_prevLowest = lowestValue;
	}
}
