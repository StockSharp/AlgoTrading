using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trade management strategy converted from the MQL5 "EA framework layout" expert advisor.
/// It does not create new entries by itself and only manages manually opened positions.
/// </summary>
public class EAFrameworkLayoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _lotAmplifier;
	private readonly StrategyParam<decimal> _tradeInitLots;
	private readonly StrategyParam<int> _autoCloseAfterXH1;
	private readonly StrategyParam<bool> _isReverse;
	private readonly StrategyParam<DealDirectionAllow> _dealDirectionAllow;
	private readonly StrategyParam<double> _usTimeLeftBound;
	private readonly StrategyParam<double> _usTimeRightBound;
	private readonly StrategyParam<double> _nonUsTimeLeftBound;
	private readonly StrategyParam<double> _nonUsTimeRightBound;
	private readonly StrategyParam<DataType> _candleType;

	private readonly decimal[] _emaShift0 = new decimal[6];
	private readonly decimal[] _emaShift1 = new decimal[6];
	private readonly decimal[] _emaShift2 = new decimal[6];

	private bool _hasShift1;
	private bool _hasShift2;
	private decimal _previousPosition;
	private int _barsSinceEntry;
	private int _entryDirection;

	/// <summary>
	/// Deal direction permissions.
	/// </summary>
	public enum DealDirectionAllow
	{
		Both,
		Up,
		Down,
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="EAFrameworkLayoutStrategy"/> class.
	/// </summary>
	public EAFrameworkLayoutStrategy()
	{
		_lotAmplifier = Param(nameof(LotAmplifier), 1m)
			.SetDisplay("Lot amplifier", "Multiplier applied to the base position size", "General");

		_tradeInitLots = Param(nameof(TradeInitLots), 0.01m)
			.SetDisplay("Initial lot", "Base volume used when the strategy adds to an existing position", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 0.1m, 0.01m);

		_autoCloseAfterXH1 = Param(nameof(AutoCloseAfterXH1), 4)
			.SetDisplay("Auto close (H1 bars)", "Number of completed H1 candles after which the position is closed", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1, 12, 1);

		_isReverse = Param(nameof(IsReverse), false)
			.SetDisplay("Reverse", "Whether the strategy should attempt to reverse positions", "General");

		_dealDirectionAllow = Param(nameof(DirectionAllow), DealDirectionAllow.Both)
			.SetDisplay("Direction", "Allowed trade direction when managing positions", "General");

		_usTimeLeftBound = Param(nameof(UsTimeLeftBound), 2.0)
			.SetDisplay("US session start", "Hour (fractional) that opens the active US session window", "Trading hours");

		_usTimeRightBound = Param(nameof(UsTimeRightBound), 23.0)
			.SetDisplay("US session end", "Hour (fractional) that closes the active US session window", "Trading hours");

		_nonUsTimeLeftBound = Param(nameof(NonUsTimeLeftBound), 0.0)
			.SetDisplay("Non-US session start", "Hour (fractional) that opens the additional session window", "Trading hours");

		_nonUsTimeRightBound = Param(nameof(NonUsTimeRightBound), 0.0)
			.SetDisplay("Non-US session end", "Hour (fractional) that closes the additional session window", "Trading hours");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle type", "Primary candle series used for management logic", "Data");
	}

	/// <summary>
	/// Multiplier applied to the base position size.
	/// </summary>
	public decimal LotAmplifier
	{
		get => _lotAmplifier.Value;
		set => _lotAmplifier.Value = value;
	}

	/// <summary>
	/// Base volume used when the strategy adds to an existing position.
	/// </summary>
	public decimal TradeInitLots
	{
		get => _tradeInitLots.Value;
		set => _tradeInitLots.Value = value;
	}

	/// <summary>
	/// Number of completed H1 candles after which the position is closed.
	/// </summary>
	public int AutoCloseAfterXH1
	{
		get => _autoCloseAfterXH1.Value;
		set => _autoCloseAfterXH1.Value = value;
	}

	/// <summary>
	/// Whether the strategy should attempt to reverse positions.
	/// </summary>
	public bool IsReverse
	{
		get => _isReverse.Value;
		set => _isReverse.Value = value;
	}

	/// <summary>
	/// Allowed trade direction when managing positions.
	/// </summary>
	public DealDirectionAllow DirectionAllow
	{
		get => _dealDirectionAllow.Value;
		set => _dealDirectionAllow.Value = value;
	}

	/// <summary>
	/// Hour (fractional) that opens the active US session window.
	/// </summary>
	public double UsTimeLeftBound
	{
		get => _usTimeLeftBound.Value;
		set => _usTimeLeftBound.Value = value;
	}

	/// <summary>
	/// Hour (fractional) that closes the active US session window.
	/// </summary>
	public double UsTimeRightBound
	{
		get => _usTimeRightBound.Value;
		set => _usTimeRightBound.Value = value;
	}

	/// <summary>
	/// Hour (fractional) that opens the additional session window.
	/// </summary>
	public double NonUsTimeLeftBound
	{
		get => _nonUsTimeLeftBound.Value;
		set => _nonUsTimeLeftBound.Value = value;
	}

	/// <summary>
	/// Hour (fractional) that closes the additional session window.
	/// </summary>
	public double NonUsTimeRightBound
	{
		get => _nonUsTimeRightBound.Value;
		set => _nonUsTimeRightBound.Value = value;
	}

	/// <summary>
	/// Primary candle series used for management logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		Array.Clear(_emaShift0);
		Array.Clear(_emaShift1);
		Array.Clear(_emaShift2);

		_hasShift1 = false;
		_hasShift2 = false;
		_previousPosition = 0;
		_barsSinceEntry = 0;
		_entryDirection = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeInitLots;

		var ema2 = new ExponentialMovingAverage { Length = 2 };
		var ema4 = new ExponentialMovingAverage { Length = 4 };
		var ema6 = new ExponentialMovingAverage { Length = 6 };
		var ema8 = new ExponentialMovingAverage { Length = 8 };
		var ema12 = new ExponentialMovingAverage { Length = 12 };
		var ema16 = new ExponentialMovingAverage { Length = 16 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema2, ema4, ema6, ema8, ema12, ema16, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema2);
			DrawIndicator(area, ema4);
			DrawIndicator(area, ema6);
			DrawIndicator(area, ema8);
			DrawIndicator(area, ema12);
			DrawIndicator(area, ema16);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema2, decimal ema4, decimal ema6, decimal ema8, decimal ema12, decimal ema16)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateEmaHistory(ema2, ema4, ema6, ema8, ema12, ema16);

		var position = Position;
		var positionSign = position > 0 ? 1 : position < 0 ? -1 : 0;

		if (positionSign != 0 && _previousPosition == 0)
		{
			_barsSinceEntry = 0;
			_entryDirection = positionSign;
		}
		else if (positionSign == 0)
		{
			_barsSinceEntry = 0;
			_entryDirection = 0;
		}
		else if (positionSign != _entryDirection)
		{
			_barsSinceEntry = 0;
			_entryDirection = positionSign;
		}

		_previousPosition = position;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (positionSign == 0)
			return;

		if (!IsDirectionAllowed(positionSign))
		{
			CloseCurrentPosition(positionSign);
			return;
		}

		if (_hasShift2)
		{
			var maDirection = CalculateDirection(2, 1);
			if (maDirection * positionSign != 1)
			{
				CloseCurrentPosition(positionSign);
				return;
			}
		}

		if (AutoCloseAfterXH1 > 0)
		{
			_barsSinceEntry++;
			if (_barsSinceEntry >= AutoCloseAfterXH1)
			{
				CloseCurrentPosition(positionSign);
			}
		}
	}

	private bool IsDirectionAllowed(int positionSign)
	{
		return DirectionAllow switch
		{
			DealDirectionAllow.Both => true,
			DealDirectionAllow.Up => positionSign > 0,
			DealDirectionAllow.Down => positionSign < 0,
			_ => true,
		};
	}

	private void CloseCurrentPosition(int positionSign)
	{
		var volume = Math.Abs(Position);
		if (volume == 0)
			return;

		if (positionSign > 0)
		{
			SellMarket(volume);
		}
		else
		{
			BuyMarket(volume);
		}
	}

	private void UpdateEmaHistory(decimal ema2, decimal ema4, decimal ema6, decimal ema8, decimal ema12, decimal ema16)
	{
		for (var i = 0; i < _emaShift2.Length; i++)
		{
			_emaShift2[i] = _emaShift1[i];
			_emaShift1[i] = _emaShift0[i];
		}

		_emaShift0[0] = ema2;
		_emaShift0[1] = ema4;
		_emaShift0[2] = ema6;
		_emaShift0[3] = ema8;
		_emaShift0[4] = ema12;
		_emaShift0[5] = ema16;

		_hasShift2 = _hasShift1;
		_hasShift1 = true;
	}

	private int CalculateDirection(int confirmTimes, int fromShift)
	{
		var direction = 0;

		for (var shift = fromShift; shift < fromShift + confirmTimes; shift++)
		{
			var values = GetShiftValues(shift);
			if (values == null)
				return 0;

			var tmpDirection = 0;
			if (values[0] > values[1])
				tmpDirection = 1;
			else if (values[0] < values[1])
				tmpDirection = -1;

			if (tmpDirection == 0)
				return 0;

			for (var i = 1; i < values.Length - 1; i++)
			{
				if ((values[i] - values[i + 1]) * tmpDirection < 0)
					return 0;
			}

			if (direction == 0)
				direction = tmpDirection;
			else if (direction * tmpDirection != 1)
				return 0;
		}

		var previousValues = GetShiftValues(fromShift);
		var olderValues = GetShiftValues(fromShift + 1);
		if (previousValues == null || olderValues == null)
			return 0;

		for (var i = 0; i < previousValues.Length; i++)
		{
			if ((previousValues[i] - olderValues[i]) * direction <= 0)
				return 0;
		}

		return direction;
	}

	private decimal[] GetShiftValues(int shift)
	{
		return shift switch
		{
			0 => _emaShift0,
			1 => _hasShift1 ? _emaShift1 : null,
			2 => _hasShift2 ? _emaShift2 : null,
			_ => null,
		};
	}

	private bool IsInDealTime(DateTimeOffset time)
	{
		if (IsWithinWindow(time, UsTimeLeftBound, UsTimeRightBound))
			return true;

		if (IsWithinWindow(time, NonUsTimeLeftBound, NonUsTimeRightBound))
			return true;

		return false;
	}

	private static bool IsWithinWindow(DateTimeOffset time, double leftBound, double rightBound)
	{
		var hour = time.Hour;
		var minute = time.Minute;

		var leftHour = (int)leftBound;
		var rightHour = (int)rightBound;
		var leftMinute = (int)Math.Round((leftBound - leftHour) * 60, MidpointRounding.AwayFromZero);
		var rightMinute = (int)Math.Round((rightBound - rightHour) * 60, MidpointRounding.AwayFromZero);

		if (hour == leftHour && minute >= leftMinute)
			return true;

		if (hour == rightHour && minute > 0 && minute < rightHour)
			return true;

		if (hour >= leftHour + 1 && hour < rightHour)
			return true;

		if (rightMinute == 0 && hour == rightHour)
			return true;

		return false;
	}
}

