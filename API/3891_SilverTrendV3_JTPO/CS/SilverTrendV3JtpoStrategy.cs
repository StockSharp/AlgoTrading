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
/// SilverTrend v3 strategy converted from MetaTrader 4.
/// </summary>
public class SilverTrendV3JtpoStrategy : Strategy
{
	private readonly StrategyParam<int> _silverTrendLookback;
	private readonly StrategyParam<int> _silverTrendWindow;
	private readonly StrategyParam<int> _defaultRisk;
	private readonly StrategyParam<int> _jtpoLength;

	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _initialStopPoints;
	private readonly StrategyParam<decimal> _fridayHour;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _candles = new();

	private decimal _point;
	private decimal _trailingDistance;
	private decimal _takeProfitDistance;
	private decimal _initialStopDistance;

	private int _previousSignal;
	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal? _longTake;
	private decimal? _shortTake;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;


	/// <summary>
	/// Number of candles used for SilverTrend calculations.
	/// </summary>
	public int SilverTrendLookback
	{
		get => _silverTrendLookback.Value;
		set => _silverTrendLookback.Value = value;
	}

	/// <summary>
	/// Window size used to compute SilverTrend extrema.
	/// </summary>
	public int SilverTrendWindow
	{
		get => _silverTrendWindow.Value;
		set => _silverTrendWindow.Value = value;
	}

	/// <summary>
	/// Risk coefficient applied in SilverTrend formulas.
	/// </summary>
	public int DefaultRisk
	{
		get => _defaultRisk.Value;
		set => _defaultRisk.Value = value;
	}

	/// <summary>
	/// Length parameter used in the J_TPO filter.
	/// </summary>
	public int JtpoDefaultLength
	{
		get => _jtpoLength.Value;
		set => _jtpoLength.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initial stop loss distance expressed in points.
	/// </summary>
	public decimal InitialStopPoints
	{
		get => _initialStopPoints.Value;
		set => _initialStopPoints.Value = value;
	}

	/// <summary>
	/// Hour after which the strategy stops opening new trades on Fridays.
	/// </summary>
	public decimal FridayCutoffHour
	{
		get => _fridayHour.Value;
		set => _fridayHour.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SilverTrendV3JtpoStrategy"/> class.
	/// </summary>
	public SilverTrendV3JtpoStrategy()
	{

		_silverTrendLookback = Param(nameof(SilverTrendLookback), 350)
		.SetGreaterThanZero()
		.SetDisplay("SilverTrend Lookback", "Number of candles for SilverTrend calculations", "SilverTrend")
		.SetCanOptimize(true)
		.SetOptimize(100, 600, 50);

		_silverTrendWindow = Param(nameof(SilverTrendWindow), 9)
		.SetGreaterThanZero()
		.SetDisplay("SilverTrend Window", "Sliding window size for extrema", "SilverTrend")
		.SetCanOptimize(true)
		.SetOptimize(3, 21, 2);

		_defaultRisk = Param(nameof(DefaultRisk), 3)
		.SetNotNegative()
		.SetDisplay("Risk Coefficient", "Risk coefficient used in SilverTrend", "SilverTrend")
		.SetCanOptimize(true)
		.SetOptimize(0, 10, 1);

		_jtpoLength = Param(nameof(JtpoDefaultLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("J TPO Length", "Lookback for J_TPO filter", "SilverTrend")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 0m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in points", "Risk Management")
		.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Take Profit", "Take profit distance in points", "Risk Management")
		.SetCanOptimize(true);

		_initialStopPoints = Param(nameof(InitialStopPoints), 0m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Initial Stop", "Initial protective stop distance in points", "Risk Management")
		.SetCanOptimize(true);

		_fridayHour = Param(nameof(FridayCutoffHour), 16m)
		.SetGreaterThanOrEqual(0m)
		.SetDisplay("Friday Cutoff", "Hour after which no new trades are allowed on Friday", "Risk Management")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe", "General");
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

		_candles.Clear();
		_point = 0m;
		_trailingDistance = 0m;
		_takeProfitDistance = 0m;
		_initialStopDistance = 0m;
		_previousSignal = 0;
		_longStop = null;
		_shortStop = null;
		_longTake = null;
		_shortTake = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_point = Security.PriceStep ?? 0.0001m;
		_trailingDistance = TrailingStopPoints * _point;
		_takeProfitDistance = TakeProfitPoints * _point;
		_initialStopDistance = InitialStopPoints * _point;

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		// Keep a sliding window of recent candles for indicator calculations.
		_candles.Add(candle);
		if (_candles.Count > SilverTrendLookback + SilverTrendWindow + 110)
		_candles.RemoveAt(0);

		// Compute both SilverTrend direction and J_TPO filter values.
		var signal = CalculateSilverTrendSignal();
		var jtpo = CalculateJtpo(JtpoDefaultLength);

		var prevSignal = _previousSignal;
		var canTrade = IsFormedAndOnlineAndAllowTrading();

		var longSignal = prevSignal != signal && signal > 0 && jtpo > 0m;
		var shortSignal = prevSignal != signal && signal < 0 && jtpo < 0m;
		var exitLong = prevSignal > 0;
		var exitShort = prevSignal < 0;

		if (!canTrade)
		{
			_previousSignal = signal;
			return;
		}

		if (Position == 0)
		{
			ResetStops();

			// Respect the original restriction that forbids new trades late on Fridays.
			if (IsFridayRestricted(candle.OpenTime))
			{
				_previousSignal = signal;
				return;
			}

			if (longSignal)
			{
				// Open a new long position when SilverTrend flips upward with positive J_TPO.
				OpenLong(candle);
			}
			else if (shortSignal)
			{
				// Open a new short position when SilverTrend flips downward with negative J_TPO.
				OpenShort(candle);
			}
		}
		else if (Position > 0)
		{
			if (exitLong)
			{
				ClosePosition();
				ResetStops();
			}
			else
			{
				// Manage protective logic for an active long position.
				ManageLong(candle);
			}
		}
		else if (Position < 0)
		{
			if (exitShort)
			{
				ClosePosition();
				ResetStops();
			}
			else
			{
				// Manage protective logic for an active short position.
				ManageShort(candle);
			}
		}

		_previousSignal = signal;
	}

	private void OpenLong(ICandleMessage candle)
	{
		BuyMarket(Volume);
		_longEntryPrice = candle.ClosePrice;
		_longStop = _initialStopDistance > 0m ? candle.ClosePrice - _initialStopDistance : null;
		_longTake = _takeProfitDistance > 0m ? candle.ClosePrice + _takeProfitDistance : null;
	}

	private void OpenShort(ICandleMessage candle)
	{
		SellMarket(Volume);
		_shortEntryPrice = candle.ClosePrice;
		_shortStop = _initialStopDistance > 0m ? candle.ClosePrice + _initialStopDistance : null;
		_shortTake = _takeProfitDistance > 0m ? candle.ClosePrice - _takeProfitDistance : null;
	}

	private void ManageLong(ICandleMessage candle)
	{
		// Update trailing stop for the long position once price moves in profit.
		if (_longEntryPrice.HasValue && _trailingDistance > 0m)
		{
			var move = candle.ClosePrice - _longEntryPrice.Value;
			if (move > _trailingDistance)
			{
				var newStop = candle.ClosePrice - _trailingDistance;
				if (_longStop is null || newStop > _longStop)
				_longStop = newStop;
			}
		}

		// Check take-profit before stop loss to mimic the MQL execution priority.
		if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
		{
			ClosePosition();
			ResetStops();
			return;
		}

		// Close the long position if the price crosses the protective stop.
		if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
		{
			ClosePosition();
			ResetStops();
		}
	}

	private void ManageShort(ICandleMessage candle)
	{
		// Update trailing stop for the short position once price moves in profit.
		if (_shortEntryPrice.HasValue && _trailingDistance > 0m)
		{
			var move = _shortEntryPrice.Value - candle.ClosePrice;
			if (move > _trailingDistance)
			{
				var newStop = candle.ClosePrice + _trailingDistance;
				if (_shortStop is null || newStop < _shortStop)
				_shortStop = newStop;
			}
		}

		// Check take-profit before stop loss to mimic the MQL execution priority.
		if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
		{
			ClosePosition();
			ResetStops();
			return;
		}

		// Close the short position if the price crosses the protective stop.
		if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
		{
			ClosePosition();
			ResetStops();
		}
	}

	private bool IsFridayRestricted(DateTimeOffset time)
	{
		if (FridayCutoffHour <= 0m)
		return false;

		if (time.DayOfWeek != DayOfWeek.Friday)
		return false;

		return time.TimeOfDay.TotalHours > (double)FridayCutoffHour;
	}

	private void ResetStops()
	{
		_longStop = null;
		_shortStop = null;
		_longTake = null;
		_shortTake = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	private int CalculateSilverTrendSignal()
	{
		var available = _candles.Count;
		if (available < SilverTrendWindow + 1)
		return 0;

		var maxIndex = Math.Min(available - 1, SilverTrendLookback);
		var k = 33 - DefaultRisk;
		var uptrend = false;

		for (var i = maxIndex - SilverTrendWindow + 1; i >= 0; i--)
		{
			var end = Math.Min(i + SilverTrendWindow - 1, available - 1);
			var ssMax = GetHigh(i);
			var ssMin = GetLow(i);

			for (var j = i; j <= end; j++)
			{
				var high = GetHigh(j);
				if (ssMax < high)
				ssMax = high;

				var low = GetLow(j);
				if (ssMin >= low)
				ssMin = low;
			}

			var smin = ssMin + (ssMax - ssMin) * k / 100m;
			var smax = ssMax - (ssMax - ssMin) * k / 100m;
			var close = GetClose(i);

			if (close < smin)
			uptrend = false;
			if (close > smax)
			uptrend = true;

			if (i == 0)
			return uptrend ? 1 : -1;
		}

		return uptrend ? 1 : -1;
	}

	private decimal CalculateJtpo(int length)
	{
		if (length <= 0)
		return 0m;

		var required = length + 100;
		if (_candles.Count < required)
		return 0m;

		var arr0 = new decimal[301];
		var arr1 = new decimal[301];
		var arr2 = new decimal[301];
		var arr3 = new decimal[301];

		var f38 = 0;
		var f40 = 0m;
		decimal f30;
		var f48 = 0;
		var f10 = 0m;
		var f18 = 0m;
		var f20 = 0m;
		var value = 0m;

		var maxIndex = _candles.Count - length - 1;
		var start = Math.Min(maxIndex, 200 - length - 100);
		if (start < 0)
		start = 0;

		for (var i = start; i >= 0; i--)
		{
			var var14 = 0m;
			var var1C = 0m;

			if (f38 == 0)
			{
				f38 = 1;
				f40 = 0m;
				f30 = length - 1 >= 2 ? length - 1 : 2;
				f48 = (int)(f30 + 1);
				f10 = GetClose(i);
				arr0[f38] = f10;
				var k = f48;
				f18 = 12m / (k * (k - 1m) * (k + 1m));
				f20 = (f48 + 1m) * 0.5m;
			}
			else
			{
				if (f38 <= f48)
				f38 = f38 + 1;
				else
				f38 = f48 + 1;

				var f8 = f10;
				f10 = GetClose(i);

				if (f38 > f48)
				{
					for (var var6 = 2; var6 <= f48; var6++)
					arr0[var6 - 1] = arr0[var6];

					arr0[f48] = f10;
				}
				else
				{
					arr0[f38] = f10;
				}

				if ((length - 1 >= f38) && (f8 != f10))
				f40 = 1m;

				if ((length - 1 == f38) && (f40 == 0m))
				f38 = 0;
			}

			if (f38 >= f48)
			{
				for (var varA = 1; varA <= f48; varA++)
				{
					arr2[varA] = varA;
					arr3[varA] = varA;
					arr1[varA] = arr0[varA];
				}

				for (var varA = 1; varA <= f48 - 1; varA++)
				{
					var var24 = arr1[varA];
					var var12 = varA;
					for (var var6 = varA + 1; var6 <= f48; var6++)
					{
						if (arr1[var6] < var24)
						{
							var24 = arr1[var6];
							var12 = var6;
						}
					}

					var var20 = arr1[varA];
					arr1[varA] = arr1[var12];
					arr1[var12] = var20;

					var20 = arr2[varA];
					arr2[varA] = arr2[var12];
					arr2[var12] = var20;
				}

				var varAIndex = 1;
				while (f48 > varAIndex)
				{
					var var6 = varAIndex + 1;
					var14 = 1m;
					var1C = arr3[varAIndex];

					while (var14 != 0m)
					{
						if (arr1[varAIndex] != arr1[var6])
						{
							if ((var6 - varAIndex) > 1)
							{
								var1C = var1C / (var6 - varAIndex);
								for (var varE = varAIndex; varE <= var6 - 1; varE++)
								arr3[varE] = var1C;
							}

							var14 = 0m;
						}
						else
						{
							var1C = var1C + arr3[var6];
							var6 = var6 + 1;
						}
					}

					varAIndex = var6;
				}

				var1C = 0m;
				for (var varA = 1; varA <= f48; varA++)
				var1C += (arr3[varA] - f20) * (arr2[varA] - f20);

				var var18 = f18 * var1C;
				value = var18;
			}
			else
			{
				value = 0m;
			}

			if (value == 0m)
			value = 0.00001m;
		}

		return value;
	}

	private decimal GetClose(int shift)
	{
		var index = _candles.Count - 1 - shift;
		return index >= 0 ? _candles[index].ClosePrice : 0m;
	}

	private decimal GetHigh(int shift)
	{
		var index = _candles.Count - 1 - shift;
		return index >= 0 ? _candles[index].HighPrice : 0m;
	}

	private decimal GetLow(int shift)
	{
		var index = _candles.Count - 1 - shift;
		return index >= 0 ? _candles[index].LowPrice : 0m;
	}
}

