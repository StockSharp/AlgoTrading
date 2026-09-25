using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hedge Average strategy comparing open/close SMAs over a fast and a slow period.
/// </summary>
public class HedgeAverageStrategy : Strategy
{
	private readonly StrategyParam<int> _period1;
	private readonly StrategyParam<int> _period2;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<bool> _useTrailing;

	private readonly List<decimal> _opens = [];
	private readonly List<decimal> _closes = [];
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal? _bestPrice;
	private DateTimeOffset? _entryCandleTime;

	public int Period1 { get => _period1.Value; set => _period1.Value = value; }
	public int Period2 { get => _period2.Value; set => _period2.Value = value; }
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public bool UseTrailing { get => _useTrailing.Value; set => _useTrailing.Value = value; }

	public HedgeAverageStrategy()
	{
		_period1 = Param(nameof(Period1), 5).SetGreaterThanZero();
		_period2 = Param(nameof(Period2), 20).SetGreaterThanZero();
		_startHour = Param(nameof(StartHour), 0).SetRange(0, 23);
		_endHour = Param(nameof(EndHour), 23).SetRange(0, 23);
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame());
		_takeProfit = Param(nameof(TakeProfit), 0m).SetNotNegative();
		_stopLoss = Param(nameof(StopLoss), 0m).SetNotNegative();
		_useTrailing = Param(nameof(UseTrailing), false);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_opens.Clear();
		_closes.Clear();
		ResetProtection();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_opens.Add(candle.OpenPrice);
		_closes.Add(candle.ClosePrice);

		var keep = Math.Max(Period1, Period2);
		if (_opens.Count > keep)
		{
			_opens.RemoveRange(0, _opens.Count - keep);
			_closes.RemoveRange(0, _closes.Count - keep);
		}

		if (Position != 0 && ApplyProtection(candle))
			return;

		if (_opens.Count < keep || Position != 0 || !IsTradingHour(candle.OpenTime.Hour))
			return;

		var fastOpen = AverageTail(_opens, Period1);
		var fastClose = AverageTail(_closes, Period1);
		var slowOpen = AverageTail(_opens, Period2);
		var slowClose = AverageTail(_closes, Period2);

		if (slowOpen > slowClose && fastOpen < fastClose)
			Enter(Sides.Buy, candle.ClosePrice, candle.OpenTime);
		else if (slowOpen < slowClose && fastOpen > fastClose)
			Enter(Sides.Sell, candle.ClosePrice, candle.OpenTime);
	}

	private void Enter(Sides side, decimal price, DateTimeOffset candleTime)
	{
		if (side == Sides.Buy)
			BuyMarket();
		else
			SellMarket();

		_entryPrice = price;
		_entryCandleTime = candleTime;
		_bestPrice = price;
		_stopPrice = StopLoss > 0m ? (side == Sides.Buy ? price - StopLoss : price + StopLoss) : null;
		_takePrice = TakeProfit > 0m ? (side == Sides.Buy ? price + TakeProfit : price - TakeProfit) : null;
	}

	private bool ApplyProtection(ICandleMessage candle)
	{
		if (_entryCandleTime is DateTimeOffset entryTime && candle.OpenTime <= entryTime)
			return false;

		if (Position > 0)
		{
			_bestPrice = _bestPrice is decimal best ? Math.Max(best, candle.HighPrice) : candle.HighPrice;

			if (UseTrailing && StopLoss > 0m)
			{
				var candidate = _bestPrice.Value - StopLoss;
				if (_stopPrice is null || candidate > _stopPrice)
					_stopPrice = candidate;
			}

			if ((_stopPrice is decimal stop && candle.LowPrice <= stop) ||
				(_takePrice is decimal take && candle.HighPrice >= take))
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}
		}
		else if (Position < 0)
		{
			_bestPrice = _bestPrice is decimal best ? Math.Min(best, candle.LowPrice) : candle.LowPrice;

			if (UseTrailing && StopLoss > 0m)
			{
				var candidate = _bestPrice.Value + StopLoss;
				if (_stopPrice is null || candidate < _stopPrice)
					_stopPrice = candidate;
			}

			if ((_stopPrice is decimal stop && candle.HighPrice >= stop) ||
				(_takePrice is decimal take && candle.LowPrice <= take))
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}
		}

		return false;
	}

	private bool IsTradingHour(int hour)
		=> StartHour <= EndHour
			? hour >= StartHour && hour <= EndHour
			: hour >= StartHour || hour <= EndHour;

	private static decimal AverageTail(List<decimal> values, int length)
		=> values.Skip(values.Count - length).Average();

	private void ResetProtection()
	{
		_entryPrice = 0m;
		_stopPrice = null;
		_takePrice = null;
		_bestPrice = null;
		_entryCandleTime = null;
	}
}
