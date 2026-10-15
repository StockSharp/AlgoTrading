using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gap filling strategy.
/// A session is a UTC calendar day. When its first candle opens away from the previous session's last close, the strategy fades the gap
/// (short an up gap, long a down gap) with the previous close as the profit target. With Invert it trades in the gap direction instead
/// and the previous close becomes the stop. With CloseWhen set to NewSession an open position is also closed when the next session
/// starts; with GapLevel it stays open until the target or stop is reached.
/// </summary>
public class GapFillingStrategy : Strategy
{
	/// <summary>
	/// When a position is closed besides the gap level.
	/// </summary>
	public enum CloseModes
	{
		/// <summary>
		/// Close at the start of the next session or at the gap level.
		/// </summary>
		NewSession,

		/// <summary>
		/// Close only at the gap level.
		/// </summary>
		GapLevel,
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _invert;
	private readonly StrategyParam<CloseModes> _closeWhen;

	private DateTime? _sessionDay;
	private decimal? _lastClose;
	private decimal _gapLevel;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trade in the gap direction with a stop at the gap level instead of fading it.
	/// </summary>
	public bool Invert
	{
		get => _invert.Value;
		set => _invert.Value = value;
	}

	/// <summary>
	/// When the position is closed besides the gap level.
	/// </summary>
	public CloseModes CloseWhen
	{
		get => _closeWhen.Value;
		set => _closeWhen.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public GapFillingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_invert = Param(nameof(Invert), false)
			.SetDisplay("Invert", "Trade in the gap direction with a stop at the gap level", "Trading");

		_closeWhen = Param(nameof(CloseWhen), CloseModes.NewSession)
			.SetDisplay("Close When", "When the position is closed besides the gap level", "Trading");
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
		_sessionDay = null;
		_lastClose = null;
		_gapLevel = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

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

		var day = candle.OpenTime.Date;
		var newSession = _sessionDay is DateTime current && current != day;
		var previousClose = _lastClose;

		_sessionDay = day;
		_lastClose = candle.ClosePrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (newSession)
		{
			if (Position != 0 && CloseWhen != CloseModes.NewSession)
				return;

			var target = 0m;

			if (previousClose is decimal gapLevel && candle.OpenPrice != gapLevel)
			{
				_gapLevel = gapLevel;
				var gapUp = candle.OpenPrice > gapLevel;
				target = gapUp != Invert ? -Volume : Volume;
			}

			// One order both closes the previous session's position and opens the new one.
			var diff = target - Position;

			if (diff > 0)
				BuyMarket(diff);
			else if (diff < 0)
				SellMarket(-diff);

			return;
		}

		if (Position == 0 || _gapLevel <= 0)
			return;

		// A faded gap targets the gap level; an inverted trade is stopped there.
		if (Position > 0)
		{
			var hit = Invert ? candle.LowPrice <= _gapLevel : candle.HighPrice >= _gapLevel;
			if (hit)
				ClosePosition();
		}
		else
		{
			var hit = Invert ? candle.HighPrice >= _gapLevel : candle.LowPrice <= _gapLevel;
			if (hit)
				ClosePosition();
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));
	}
}
