using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// G-Channel with EMA strategy.
/// The G-Channel upper band is max(close, upper) minus (upper - lower) / ChannelLength and the lower band min(close, lower) plus the
/// same amount. As in the original G-Channel, an upward cross is the close moving from above the lower band to below it and a
/// downward cross the close moving from above the upper band to below it. With the last cross downward and the close below the EMA
/// the strategy goes long; with the last cross upward and the close above the EMA it goes short. An opposite signal reverses the position.
/// </summary>
public class GChannelEmaStrategy : Strategy
{
	private readonly StrategyParam<int> _channelLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _upper;
	private decimal? _lower;
	private decimal? _prevClose;
	private int _lastCross;

	/// <summary>
	/// G-Channel length.
	/// </summary>
	public int ChannelLength
	{
		get => _channelLength.Value;
		set => _channelLength.Value = value;
	}

	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
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
	public GChannelEmaStrategy()
	{
		_channelLength = Param(nameof(ChannelLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Channel Length", "G-Channel length", "Indicators");

		_emaLength = Param(nameof(EmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA length", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_upper = null;
		_lower = null;
		_prevClose = null;
		_lastCross = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (_upper is not decimal prevUpper || _lower is not decimal prevLower || _prevClose is not decimal prevClose)
		{
			_upper = close;
			_lower = close;
			_prevClose = close;
			return;
		}

		var step = (prevUpper - prevLower) / ChannelLength;
		var upper = Math.Max(close, prevUpper) - step;
		var lower = Math.Min(close, prevLower) + step;

		if (prevLower < prevClose && lower > close)
			_lastCross = 1;
		else if (prevUpper < prevClose && upper > close)
			_lastCross = -1;

		_upper = upper;
		_lower = lower;
		_prevClose = close;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_lastCross < 0 && close < ema && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (_lastCross > 0 && close > ema && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
