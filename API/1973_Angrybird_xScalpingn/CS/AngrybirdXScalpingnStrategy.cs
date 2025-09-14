namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class AngrybirdXScalpingnStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<bool> _dynamicPips;
	private readonly StrategyParam<int> _defaultPips;
	private readonly StrategyParam<int> _depth;
	private readonly StrategyParam<int> _del;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _drop;
	private readonly StrategyParam<decimal> _rsiMinimum;
	private readonly StrategyParam<decimal> _rsiMaximum;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _highs = new();
	private readonly Queue<decimal> _lows = new();
	private decimal? _lastBuyPrice;
	private decimal? _lastSellPrice;
	private int _tradeCount;
	private decimal _pipStep;
	private decimal _prevClose;

	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }
	public decimal LotExponent { get => _lotExponent.Value; set => _lotExponent.Value = value; }
	public bool DynamicPips { get => _dynamicPips.Value; set => _dynamicPips.Value = value; }
	public int DefaultPips { get => _defaultPips.Value; set => _defaultPips.Value = value; }
	public int Depth { get => _depth.Value; set => _depth.Value = value; }
	public int Del { get => _del.Value; set => _del.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal Drop { get => _drop.Value; set => _drop.Value = value; }
	public decimal RsiMinimum { get => _rsiMinimum.Value; set => _rsiMinimum.Value = value; }
	public decimal RsiMaximum { get => _rsiMaximum.Value; set => _rsiMaximum.Value = value; }
	public int MaxTrades { get => _maxTrades.Value; set => _maxTrades.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AngrybirdXScalpingnStrategy()
	{
		_volume = Param(nameof(Volume), 0.01m)
		.SetDisplay("Volume", "Base trade volume", "General")
		.SetGreaterThanZero();

		_lotExponent = Param(nameof(LotExponent), 2m)
		.SetDisplay("Lot Exponent", "Volume multiplier for additional trades", "General")
		.SetGreaterThanZero();

		_dynamicPips = Param(nameof(DynamicPips), true)
		.SetDisplay("Dynamic Pips", "Use dynamic grid step", "Parameters");

		_defaultPips = Param(nameof(DefaultPips), 12)
		.SetDisplay("Default Pips", "Base grid step in ticks", "Parameters")
		.SetGreaterThanZero();

		_depth = Param(nameof(Depth), 24)
		.SetDisplay("Depth", "Bars lookback for dynamic step", "Parameters")
		.SetGreaterThanZero();

		_del = Param(nameof(Del), 3)
		.SetDisplay("Del", "Divider for range calculation", "Parameters")
		.SetGreaterThanZero();

		_takeProfit = Param(nameof(TakeProfit), 20m)
		.SetDisplay("Take Profit", "Take profit in ticks", "Risk")
		.SetNotNegative();

		_stopLoss = Param(nameof(StopLoss), 500m)
		.SetDisplay("Stop Loss", "Stop loss in ticks", "Risk")
		.SetNotNegative();

		_drop = Param(nameof(Drop), 500m)
		.SetDisplay("CCI Drop", "CCI threshold for exit", "Parameters")
		.SetNotNegative();

		_rsiMinimum = Param(nameof(RsiMinimum), 30m)
		.SetDisplay("RSI Minimum", "Minimum RSI to allow short", "Parameters")
		.SetNotNegative();

		_rsiMaximum = Param(nameof(RsiMaximum), 70m)
		.SetDisplay("RSI Maximum", "Maximum RSI to allow long", "Parameters")
		.SetNotNegative();

		_maxTrades = Param(nameof(MaxTrades), 10)
		.SetDisplay("Max Trades", "Maximum number of open trades", "Risk")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "Parameters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highs.Clear();
		_lows.Clear();
		_lastBuyPrice = _lastSellPrice = null;
		_tradeCount = 0;
		_pipStep = 0m;
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = 14 };
		var cci = new CommodityChannelIndex { Length = 55 };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(rsi, cci, ProcessCandle)
		.Start();

		StartProtection(new Unit(TakeProfit, UnitTypes.Step), new Unit(StopLoss, UnitTypes.Step));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, rsi);
			DrawIndicator(area, cci);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal cci)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		// calculate dynamic grid step
		if (DynamicPips)
		{
			_highs.Enqueue(high);
			_lows.Enqueue(low);

			if (_highs.Count > Depth)
			{
				_highs.Dequeue();
				_lows.Dequeue();
			}

			if (_highs.Count == Depth)
			{
				var highest = _highs.Max();
				var lowest = _lows.Min();
				var step = (highest - lowest) / Del;
				var stepSize = Security?.Step ?? 1m;
				var minStep = (DefaultPips / (decimal)Del) * stepSize;
				var maxStep = (DefaultPips * Del) * stepSize;
				step = Math.Min(Math.Max(step, minStep), maxStep);
				_pipStep = step;
			}
		}
		else
		{
			_pipStep = DefaultPips * (Security?.Step ?? 1m);
		}

		if (Position == 0)
		{
			_lastBuyPrice = _lastSellPrice = null;
			_tradeCount = 0;
		}

		// close all on CCI reversal
		if (Position > 0 && cci < -Drop)
		{
			ClosePosition();
			return;
		}

		if (Position < 0 && cci > Drop)
		{
			ClosePosition();
			return;
		}

		if (Position != 0)
		{
			if (_tradeCount < MaxTrades)
			{
				if (Position > 0 && _lastBuyPrice is decimal lb && lb - close >= _pipStep)
				{
					var vol = Volume * (decimal)Math.Pow((double)LotExponent, _tradeCount);
					BuyMarket(vol);
					_lastBuyPrice = close;
					_tradeCount++;
				}
				else if (Position < 0 && _lastSellPrice is decimal ls && close - ls >= _pipStep)
				{
					var vol = Volume * (decimal)Math.Pow((double)LotExponent, _tradeCount);
					SellMarket(vol);
					_lastSellPrice = close;
					_tradeCount++;
				}
			}

			_prevClose = close;
			return;
		}

		// first trade decision
		if (_prevClose != 0m)
		{
			if (_prevClose > close && rsi > RsiMinimum)
			{
				SellMarket(Volume);
				_lastSellPrice = close;
				_tradeCount = 1;
			}
			else if (_prevClose <= close && rsi < RsiMaximum)
			{
				BuyMarket(Volume);
				_lastBuyPrice = close;
				_tradeCount = 1;
			}
		}

		_prevClose = close;
	}
}
