using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SilverTrend v3 momentum strategy ported from MetaTrader 5.
/// </summary>
public class SilverTrendV3Strategy : Strategy
{
	private const int CountBars = 350;
	private const int Ssp = 9;
	private const int JtpoLength = 14;
	private const int HistoryCapacity = 400;
	private const int Risk = 3;

	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _initialStopLossPoints;
	private readonly StrategyParam<int> _fridayCutoffHour;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closeHistory = new();
	private readonly List<decimal> _highHistory = new();
	private readonly List<decimal> _lowHistory = new();

	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private decimal _entryPrice;
	private int _previousSignal;
	private decimal _pointValue;

	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initial stop loss distance expressed in price steps.
	/// </summary>
	public decimal InitialStopLossPoints
	{
		get => _initialStopLossPoints.Value;
		set => _initialStopLossPoints.Value = value;
	}

	/// <summary>
	/// Hour after which no new trades are allowed on Friday (exchange time).
	/// </summary>
	public int FridayCutoffHour
	{
		get => _fridayCutoffHour.Value;
		set => _fridayCutoffHour.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize default parameters.
	/// </summary>
	public SilverTrendV3Strategy()
	{
		_trailingStopPoints = Param(nameof(TrailingStopPoints), 50m)
			.SetDisplay("Trailing Stop", "Trailing distance in price steps", "Risk")
			.SetGreaterThanOrEqualToZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetDisplay("Take Profit", "Take profit distance in price steps", "Risk")
			.SetGreaterThanOrEqualToZero();

		_initialStopLossPoints = Param(nameof(InitialStopLossPoints), 0m)
			.SetDisplay("Initial Stop Loss", "Initial stop loss in price steps", "Risk")
			.SetGreaterThanOrEqualToZero();

		_fridayCutoffHour = Param(nameof(FridayCutoffHour), 16)
			.SetDisplay("Friday Cutoff Hour", "Disable new entries after this hour on Friday", "Sessions")
			.SetGreaterThanOrEqualToZero()
			.SetLessThan(24);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for signal calculations", "General");

		Volume = 1m;
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

		_closeHistory.Clear();
		_highHistory.Clear();
		_lowHistory.Clear();
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_entryPrice = 0m;
		_previousSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = Security?.PriceStep ?? 1m;
		if (_pointValue <= 0m)
		{
			_pointValue = 1m;
		}

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

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		UpdateHistory(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		if (_closeHistory.Count < CountBars + Ssp + 1)
		{
			return;
		}

		var jtpo = CalculateJtpo(JtpoLength);
		var signal = CalculateSilverTrendSignal();

		var longSignal = _previousSignal != signal && signal > 0 && jtpo > 0m;
		var shortSignal = _previousSignal != signal && signal < 0 && jtpo < 0m;

		var exitLong = _previousSignal < 0;
		var exitShort = _previousSignal > 0;

		ManageOpenPosition(candle, exitLong, exitShort);

		if (Position <= 0 && longSignal && !IsFridayBlocked(candle))
		{
			EnterLong(candle);
		}
		else if (Position >= 0 && shortSignal && !IsFridayBlocked(candle))
		{
			EnterShort(candle);
		}

		_previousSignal = signal;
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		_closeHistory.Add(candle.ClosePrice);
		_highHistory.Add(candle.HighPrice);
		_lowHistory.Add(candle.LowPrice);

		if (_closeHistory.Count > HistoryCapacity)
		{
			_closeHistory.RemoveAt(0);
			_highHistory.RemoveAt(0);
			_lowHistory.RemoveAt(0);
		}
	}

	private void ManageOpenPosition(ICandleMessage candle, bool exitLongSignal, bool exitShortSignal)
	{
		if (Position > 0)
		{
			UpdateLongTrailing(candle);

			var initialStop = InitialStopLossPoints > 0m ? _entryPrice - GetDistance(InitialStopLossPoints) : (decimal?)null;
			var trailingStop = _longTrailingStop;
			var stop = CombineLongStops(initialStop, trailingStop);
			var takeProfit = TakeProfitPoints > 0m ? _entryPrice + GetDistance(TakeProfitPoints) : (decimal?)null;

			if (exitLongSignal ||
				(takeProfit.HasValue && candle.HighPrice >= takeProfit.Value) ||
				(stop.HasValue && candle.LowPrice <= stop.Value))
			{
				SellMarket(Position);
				ResetStops();
			}
		}
		else if (Position < 0)
		{
			UpdateShortTrailing(candle);

			var initialStop = InitialStopLossPoints > 0m ? _entryPrice + GetDistance(InitialStopLossPoints) : (decimal?)null;
			var trailingStop = _shortTrailingStop;
			var stop = CombineShortStops(initialStop, trailingStop);
			var takeProfit = TakeProfitPoints > 0m ? _entryPrice - GetDistance(TakeProfitPoints) : (decimal?)null;

			if (exitShortSignal ||
				(takeProfit.HasValue && candle.LowPrice <= takeProfit.Value) ||
				(stop.HasValue && candle.HighPrice >= stop.Value))
			{
				BuyMarket(Math.Abs(Position));
				ResetStops();
			}
		}
		else
		{
			ResetStops();
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		var volume = Volume;
		if (Position < 0)
		{
			volume += Math.Abs(Position);
		}

		BuyMarket(volume);

		_entryPrice = candle.ClosePrice;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	private void EnterShort(ICandleMessage candle)
	{
		var volume = Volume;
		if (Position > 0)
		{
			volume += Position;
		}

		SellMarket(volume);

		_entryPrice = candle.ClosePrice;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0m)
		{
			return;
		}

		var distance = GetDistance(TrailingStopPoints);
		var trigger = _entryPrice + distance;

		if (candle.ClosePrice > trigger)
		{
			var newStop = candle.ClosePrice - distance;
			if (!_longTrailingStop.HasValue || newStop > _longTrailingStop.Value)
			{
				_longTrailingStop = newStop;
			}
		}
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0m)
		{
			return;
		}

		var distance = GetDistance(TrailingStopPoints);
		var trigger = _entryPrice - distance;

		if (candle.ClosePrice < trigger)
		{
			var newStop = candle.ClosePrice + distance;
			if (!_shortTrailingStop.HasValue || newStop < _shortTrailingStop.Value)
			{
				_shortTrailingStop = newStop;
			}
		}
	}

	private decimal? CombineLongStops(decimal? initialStop, decimal? trailingStop)
	{
		if (initialStop == null && trailingStop == null)
		{
			return null;
		}

		if (initialStop == null)
		{
			return trailingStop;
		}

		if (trailingStop == null)
		{
			return initialStop;
		}

		return Math.Max(initialStop.Value, trailingStop.Value);
	}

	private decimal? CombineShortStops(decimal? initialStop, decimal? trailingStop)
	{
		if (initialStop == null && trailingStop == null)
		{
			return null;
		}

		if (initialStop == null)
		{
			return trailingStop;
		}

		if (trailingStop == null)
		{
			return initialStop;
		}

		return Math.Min(initialStop.Value, trailingStop.Value);
	}

	private void ResetStops()
	{
		if (Position == 0)
		{
			_entryPrice = 0m;
		}

		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	private bool IsFridayBlocked(ICandleMessage candle)
	{
		if (FridayCutoffHour <= 0)
		{
			return false;
		}

		var time = candle.OpenTime.LocalDateTime;
		return time.DayOfWeek == DayOfWeek.Friday && time.Hour > FridayCutoffHour;
	}

	private int CalculateSilverTrendSignal()
	{
		var k = 33 - Risk;
		var uptrend = false;
		var val = 0;

		for (var i = CountBars - Ssp; i >= 0; i--)
		{
			var ssMax = GetHigh(i);
			var ssMin = GetLow(i);

			for (var i2 = i; i2 <= i + Ssp - 1; i2++)
			{
				var priceHigh = GetHigh(i2);
				if (ssMax < priceHigh)
				{
					ssMax = priceHigh;
				}

				var priceLow = GetLow(i2);
				if (ssMin >= priceLow)
				{
					ssMin = priceLow;
				}
			}

			var smin = ssMin + (ssMax - ssMin) * k / 100m;
			var smax = ssMax - (ssMax - ssMin) * k / 100m;

			if (GetClose(i) < smin)
			{
				uptrend = false;
			}

			if (GetClose(i) > smax)
			{
				uptrend = true;
			}

			val = uptrend ? 1 : -1;
		}

		return val;
	}

	private decimal CalculateJtpo(int len)
	{
		if (_closeHistory.Count < 200)
		{
			return 0m;
		}

		decimal f8 = 0m;
		decimal f10 = 0m;
		decimal f18 = 0m;
		decimal f20 = 0m;
		decimal f30 = 0m;
		decimal f40 = 0m;
		decimal k = 0m;
		decimal var14 = 0m;
		decimal var18 = 0m;
		decimal var1C = 0m;
		decimal var20 = 0m;
		decimal var24 = 0m;
		decimal value = 0m;
		var f38 = 0;
		var f48 = 0;
		var arr0 = new decimal[400];
		var arr1 = new decimal[400];
		var arr2 = new decimal[400];
		var arr3 = new decimal[400];

		for (var i = 200 - len - 100; i >= 0; i--)
		{
			var14 = 0m;
			var1C = 0m;

			if (f38 == 0)
			{
				f38 = 1;
				f40 = 0m;
				f30 = len - 1 >= 2 ? len - 1 : 2;
				f48 = (int)f30 + 1;
				f10 = GetClose(i);
				arr0[f38] = f10;
				k = f48;
				f18 = 12m / (k * (k - 1) * (k + 1));
				f20 = (f48 + 1) * 0.5m;
			}
			else
			{
				if (f38 <= f48)
				{
					f38 += 1;
				}
				else
				{
					f38 = f48 + 1;
				}

				f8 = f10;
				f10 = GetClose(i);

				if (f38 > f48)
				{
					for (var var6 = 2; var6 <= f48; var6++)
					{
						arr0[var6 - 1] = arr0[var6];
					}

					arr0[f48] = f10;
				}
				else
				{
					arr0[f38] = f10;
				}

				if (f30 >= f38 && f8 != f10)
				{
					f40 = 1m;
				}

				if (f30 == f38 && f40 == 0m)
				{
					f38 = 0;
				}
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
					var24 = arr1[varA];
					var var12 = varA;

					for (var var6 = varA + 1; var6 <= f48; var6++)
					{
						if (arr1[var6] < var24)
						{
							var24 = arr1[var6];
							var12 = var6;
						}
					}

					var20 = arr1[varA];
					arr1[varA] = arr1[var12];
					arr1[var12] = var20;

					var20 = arr2[varA];
					arr2[varA] = arr2[var12];
					arr2[var12] = var20;
				}

				var varIndex = 1;
				while (f48 > varIndex)
				{
					var var6 = varIndex + 1;
					var14 = 1m;
					var1C = arr3[varIndex];

					while (var14 != 0m && var6 < arr3.Length)
					{
						if (arr1[varIndex] != arr1[var6])
						{
							if ((var6 - varIndex) > 1)
							{
								var1C /= (var6 - varIndex);

								for (var varE = varIndex; varE <= var6 - 1; varE++)
								{
									arr3[varE] = var1C;
								}
							}

							var14 = 0m;
						}
						else
						{
							var1C += arr3[var6];
							var6 += 1;

							if (var6 > f48 + 1)
							{
								break;
							}
						}
					}

					varIndex = var6;
				}

				var1C = 0m;
				for (var varA = 1; varA <= f48; varA++)
				{
					var1C += (arr3[varA] - f20) * (arr2[varA] - f20);
				}

				var18 = f18 * var1C;
			}
			else
			{
				var18 = 0m;
			}

			value = var18;

			if (value == 0m)
			{
				value = 0.00001m;
			}
		}

		return value;
	}

	private decimal GetClose(int shift)
	{
		var index = _closeHistory.Count - 1 - shift;
		if (index < 0)
		{
			index = 0;
		}

		return _closeHistory[index];
	}

	private decimal GetHigh(int shift)
	{
		var index = _highHistory.Count - 1 - shift;
		if (index < 0)
		{
			index = 0;
		}

		return _highHistory[index];
	}

	private decimal GetLow(int shift)
	{
		var index = _lowHistory.Count - 1 - shift;
		if (index < 0)
		{
			index = 0;
		}

		return _lowHistory[index];
	}

	private decimal GetDistance(decimal points)
	{
		return points * _pointValue;
	}
}
