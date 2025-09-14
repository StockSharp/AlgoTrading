using System;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily breakout strategy based on the previous bar size and daily open.
/// </summary>
public class DailyBreakpointStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _breakPoint;
	private readonly StrategyParam<int> _lastBarMin;
	private readonly StrategyParam<int> _lastBarMax;
	private readonly StrategyParam<int> _trailingStart;
	private readonly StrategyParam<int> _trailingStop;
	private readonly StrategyParam<int> _trailingStep;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;

	private ICandleMessage _prev;
	private decimal _dayOpen;
	private decimal _entry;
	private decimal _trailLevel;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BreakPoint { get => _breakPoint.Value; set => _breakPoint.Value = value; }
	public int LastBarMin { get => _lastBarMin.Value; set => _lastBarMin.Value = value; }
	public int LastBarMax { get => _lastBarMax.Value; set => _lastBarMax.Value = value; }
	public int TrailingStart { get => _trailingStart.Value; set => _trailingStart.Value = value; }
	public int TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public int TrailingStep { get => _trailingStep.Value; set => _trailingStep.Value = value; }
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	public DailyBreakpointStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
		_breakPoint = Param(nameof(BreakPoint), 20).SetGreaterThanZero().SetDisplay("Break Point", "Offset in points", "General");
		_lastBarMin = Param(nameof(LastBarMin), 5).SetGreaterThanZero().SetDisplay("Last Bar Min", "Minimal bar size", "Filter");
		_lastBarMax = Param(nameof(LastBarMax), 50).SetGreaterThanZero().SetDisplay("Last Bar Max", "Maximum bar size", "Filter");
		_trailingStart = Param(nameof(TrailingStart), 5).SetGreaterThanZero().SetDisplay("Trailing Start", "Trailing activation", "Risk");
		_trailingStop = Param(nameof(TrailingStop), 2).SetGreaterThanZero().SetDisplay("Trailing Stop", "Trailing distance", "Risk");
		_trailingStep = Param(nameof(TrailingStep), 2).SetGreaterThanZero().SetDisplay("Trailing Step", "Trailing step", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 30).SetGreaterThanZero().SetDisplay("Take Profit", "Take profit in points", "Risk");
		_stopLoss = Param(nameof(StopLoss), 0).SetDisplay("Stop Loss", "Stop loss in points", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prev = null;
		_dayOpen = 0m;
		_entry = 0m;
		_trailLevel = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var sub = SubscribeCandles(CandleType);
		sub.Bind(Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage c)
	{
		if (c.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;

		var step = Security.PriceStep ?? 1m;

		if (_prev == null || c.OpenTime.Date != _prev.OpenTime.Date)
			_dayOpen = c.OpenPrice;

		if (Position == 0 && _prev != null)
		{
			var lastSize = Math.Abs(_prev.ClosePrice - _prev.OpenPrice);
			var minSize = LastBarMin * step;
			var maxSize = LastBarMax * step;
			var offset = BreakPoint * step;
			var breakBuy = _dayOpen + offset;
			var breakSell = _dayOpen - offset;

			if (_prev.ClosePrice > _prev.OpenPrice && c.ClosePrice - _dayOpen >= offset &&
				lastSize >= minSize && lastSize <= maxSize &&
				breakBuy >= _prev.OpenPrice && breakBuy <= _prev.ClosePrice)
			{
				BuyMarket();
				_entry = c.ClosePrice;
				_trailLevel = 0m;
			}
			else if (_prev.ClosePrice < _prev.OpenPrice && _dayOpen - c.ClosePrice >= offset &&
				lastSize >= minSize && lastSize <= maxSize &&
				breakSell <= _prev.OpenPrice && breakSell >= _prev.ClosePrice)
			{
				SellMarket();
				_entry = c.ClosePrice;
				_trailLevel = 0m;
			}
		}
		else if (Position > 0)
		{
			ManageLong(c, step);
		}
		else if (Position < 0)
		{
			ManageShort(c, step);
		}

		_prev = c;
	}

	private void ManageLong(ICandleMessage c, decimal step)
	{
		var tp = TakeProfit * step;
		var sl = StopLoss * step;
		var ts = TrailingStart * step;
		var tStop = TrailingStop * step;
		var tStep = TrailingStep * step;

		if (TakeProfit > 0 && c.HighPrice - _entry >= tp)
		{
			SellMarket();
			_entry = 0m;
			_trailLevel = 0m;
			return;
		}

		if (StopLoss > 0 && _entry - c.LowPrice >= sl)
		{
			SellMarket();
			_entry = 0m;
			_trailLevel = 0m;
			return;
		}

		if (TrailingStart > 0)
		{
			if (_trailLevel == 0m)
			{
				if (c.ClosePrice - _entry >= ts)
					_trailLevel = _entry + tStop;
			}
			else
			{
				if (c.ClosePrice - _trailLevel >= tStep)
					_trailLevel += tStop;

				if (c.LowPrice <= _trailLevel)
				{
					SellMarket();
					_entry = 0m;
					_trailLevel = 0m;
				}
			}
		}
	}

	private void ManageShort(ICandleMessage c, decimal step)
	{
		var tp = TakeProfit * step;
		var sl = StopLoss * step;
		var ts = TrailingStart * step;
		var tStop = TrailingStop * step;
		var tStep = TrailingStep * step;

		if (TakeProfit > 0 && _entry - c.LowPrice >= tp)
		{
			BuyMarket();
			_entry = 0m;
			_trailLevel = 0m;
			return;
		}

		if (StopLoss > 0 && c.HighPrice - _entry >= sl)
		{
			BuyMarket();
			_entry = 0m;
			_trailLevel = 0m;
			return;
		}

		if (TrailingStart > 0)
		{
			if (_trailLevel == 0m)
			{
				if (_entry - c.ClosePrice >= ts)
					_trailLevel = _entry - tStop;
			}
			else
			{
				if (_trailLevel - c.ClosePrice >= tStep)
					_trailLevel -= tStop;

				if (c.HighPrice >= _trailLevel)
				{
					BuyMarket();
					_entry = 0m;
					_trailLevel = 0m;
				}
			}
		}
	}
}
