using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// High-Low Breakout ATR Trailing Stop strategy.
/// The first candle opening at or after SessionStartHour:SessionStartMinute (UTC) sets the day's opening range. A close crossing
/// above its high buys and a close crossing below its low sells, within the allowed Direction. The stop trails AtrMultiplier ATRs
/// behind the close and a target sits the same distance from the entry. The position size risks RiskPerTrade percent of
/// AccountSize on the stop distance. Everything is closed at ExitHour:ExitMinute and no new trades open after it.
/// </summary>
public class HighLowBreakoutAtrTrailingStopStrategy : Strategy
{
	/// <summary>
	/// Allowed trade directions.
	/// </summary>
	public enum TradeDirections
	{
		/// <summary>
		/// Long trades only.
		/// </summary>
		Long,

		/// <summary>
		/// Short trades only.
		/// </summary>
		Short,

		/// <summary>
		/// Both directions.
		/// </summary>
		Both,
	}

	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<decimal> _accountSize;
	private readonly StrategyParam<int> _sessionStartHour;
	private readonly StrategyParam<int> _sessionStartMinute;
	private readonly StrategyParam<int> _exitHour;
	private readonly StrategyParam<int> _exitMinute;
	private readonly StrategyParam<TradeDirections> _direction;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _rangeDate;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private decimal? _prevClose;
	private decimal _stopPrice;
	private decimal _targetPrice;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Trailing stop and target distance in ATRs.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Percent of the account risked per trade.
	/// </summary>
	public decimal RiskPerTrade
	{
		get => _riskPerTrade.Value;
		set => _riskPerTrade.Value = value;
	}

	/// <summary>
	/// Account size used for position sizing.
	/// </summary>
	public decimal AccountSize
	{
		get => _accountSize.Value;
		set => _accountSize.Value = value;
	}

	/// <summary>
	/// Session start hour (UTC).
	/// </summary>
	public int SessionStartHour
	{
		get => _sessionStartHour.Value;
		set => _sessionStartHour.Value = value;
	}

	/// <summary>
	/// Session start minute.
	/// </summary>
	public int SessionStartMinute
	{
		get => _sessionStartMinute.Value;
		set => _sessionStartMinute.Value = value;
	}

	/// <summary>
	/// Hour all positions are closed (UTC).
	/// </summary>
	public int ExitHour
	{
		get => _exitHour.Value;
		set => _exitHour.Value = value;
	}

	/// <summary>
	/// Minute all positions are closed.
	/// </summary>
	public int ExitMinute
	{
		get => _exitMinute.Value;
		set => _exitMinute.Value = value;
	}

	/// <summary>
	/// Allowed trade directions.
	/// </summary>
	public TradeDirections Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
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
	public HighLowBreakoutAtrTrailingStopStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 3.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Trailing stop and target distance in ATRs", "Risk");

		_riskPerTrade = Param(nameof(RiskPerTrade), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Per Trade %", "Percent of the account risked per trade", "Risk");

		_accountSize = Param(nameof(AccountSize), 10000m)
			.SetGreaterThanZero()
			.SetDisplay("Account Size", "Account size used for position sizing", "Risk");

		_sessionStartHour = Param(nameof(SessionStartHour), 9)
			.SetRange(0, 23)
			.SetDisplay("Session Start Hour", "Session start hour (UTC)", "Session");

		_sessionStartMinute = Param(nameof(SessionStartMinute), 15)
			.SetRange(0, 59)
			.SetDisplay("Session Start Minute", "Session start minute", "Session");

		_exitHour = Param(nameof(ExitHour), 15)
			.SetRange(0, 23)
			.SetDisplay("Exit Hour", "Hour all positions are closed (UTC)", "Session");

		_exitMinute = Param(nameof(ExitMinute), 15)
			.SetRange(0, 59)
			.SetDisplay("Exit Minute", "Minute all positions are closed", "Session");

		_direction = Param(nameof(Direction), TradeDirections.Both)
			.SetDisplay("Direction", "Allowed trade directions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
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
		_rangeDate = default;
		_rangeHigh = null;
		_rangeLow = null;
		_prevClose = null;
		_stopPrice = 0m;
		_targetPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private decimal GetEntryVolume(decimal stopDistance)
	{
		var volume = stopDistance > 0 ? AccountSize * RiskPerTrade / 100m / stopDistance : 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0)
			volume = Math.Floor(volume / step) * step;

		var min = Security?.MinVolume ?? 0m;
		if (volume <= 0 || volume < min)
			return Volume;

		var max = Security?.MaxVolume;
		if (max is decimal maxVolume && maxVolume > 0 && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openTime = candle.OpenTime;
		var timeOfDay = openTime.TimeOfDay;
		var sessionStart = new TimeSpan(SessionStartHour, SessionStartMinute, 0);
		var exitTime = new TimeSpan(ExitHour, ExitMinute, 0);
		var close = candle.ClosePrice;

		var prevClose = _prevClose;
		_prevClose = close;

		// The first candle of the session sets the opening range.
		if (_rangeDate != openTime.Date && timeOfDay >= sessionStart)
		{
			_rangeDate = openTime.Date;
			_rangeHigh = candle.HighPrice;
			_rangeLow = candle.LowPrice;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (timeOfDay >= exitTime || timeOfDay < sessionStart)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);

			return;
		}

		var distance = AtrMultiplier * atr;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice)
			{
				SellMarket(Position);
				return;
			}

			_stopPrice = Math.Max(_stopPrice, close - distance);
			return;
		}

		if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice)
			{
				BuyMarket(-Position);
				return;
			}

			_stopPrice = Math.Min(_stopPrice, close + distance);
			return;
		}

		if (_rangeDate != openTime.Date || _rangeHigh is not decimal high || _rangeLow is not decimal low || prevClose is not decimal lastClose)
			return;

		if (Direction != TradeDirections.Short && lastClose <= high && close > high)
		{
			BuyMarket(GetEntryVolume(distance));
			_stopPrice = close - distance;
			_targetPrice = close + distance;
		}
		else if (Direction != TradeDirections.Long && lastClose >= low && close < low)
		{
			SellMarket(GetEntryVolume(distance));
			_stopPrice = close + distance;
			_targetPrice = close - distance;
		}
	}
}
