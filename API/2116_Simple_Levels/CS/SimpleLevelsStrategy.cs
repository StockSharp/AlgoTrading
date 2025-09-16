namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trades when price crosses user defined trend lines.
/// </summary>
public class SimpleLevelsStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<TrendLine> _lines = new();
	private ICandleMessage? _prevCandle;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="SimpleLevelsStrategy"/>.
	/// </summary>
	public SimpleLevelsStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 300).SetDisplay("Stop Loss", "Stop loss in steps", "General");
		_takeProfit = Param(nameof(TakeProfit), 900).SetDisplay("Take Profit", "Take profit in steps", "General");
		_volume = Param(nameof(Volume), 1m).SetDisplay("Volume", "Order volume", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Source candle timeframe", "General");
	}

	/// <summary>
	/// Adds a new trend line.
	/// </summary>
	public void AddLine(DateTimeOffset time1, decimal price1, DateTimeOffset time2, decimal price2, LineDirection direction)
	{
		_lines.Add(new TrendLine(time1, price1, time2, price2, direction));
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
		_prevCandle = null;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			foreach (var line in _lines)
				DrawLine(line.Time1, line.Price1, line.Time2, line.Price2);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevCandle == null)
		{
			_prevCandle = candle;
			return;
		}

		foreach (var line in _lines)
		{
			if (!line.IsActive)
				continue;

			var linePricePrev = line.GetPrice(_prevCandle.OpenTime);
			var linePriceCurr = line.GetPrice(candle.OpenTime);

			var crossedUp = _prevCandle.ClosePrice < linePricePrev && candle.ClosePrice > linePriceCurr;
			var crossedDown = _prevCandle.ClosePrice > linePricePrev && candle.ClosePrice < linePriceCurr;

			if (crossedUp && (line.Direction == LineDirection.Buy || line.Direction == LineDirection.Both))
			{
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - StopLoss * Security.PriceStep;
				_takePrice = _entryPrice + TakeProfit * Security.PriceStep;
				BuyMarket(Volume);
				line.IsActive = false;
			}
			else if (crossedDown && (line.Direction == LineDirection.Sell || line.Direction == LineDirection.Both))
			{
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + StopLoss * Security.PriceStep;
				_takePrice = _entryPrice - TakeProfit * Security.PriceStep;
				SellMarket(Volume);
				line.IsActive = false;
			}
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				BuyMarket(-Position);
		}

		_prevCandle = candle;
	}

	private sealed class TrendLine
	{
		public TrendLine(DateTimeOffset time1, decimal price1, DateTimeOffset time2, decimal price2, LineDirection direction)
		{
			Time1 = time1;
			Price1 = price1;
			Time2 = time2;
			Price2 = price2;
			Direction = direction;
		}

		public DateTimeOffset Time1 { get; }
		public decimal Price1 { get; }
		public DateTimeOffset Time2 { get; }
		public decimal Price2 { get; }
		public LineDirection Direction { get; }
		public bool IsActive { get; set; } = true;

		public decimal GetPrice(DateTimeOffset time)
		{
			var total = (Time2 - Time1).TotalSeconds;
			if (total == 0)
				return Price1;
			var ratio = (time - Time1).TotalSeconds / total;
			return Price1 + (Price2 - Price1) * (decimal)ratio;
		}
	}

	/// <summary>
	/// Trade direction for the level.
	/// </summary>
	public enum LineDirection
	{
		/// <summary>
		/// Buy when price crosses upward.
		/// </summary>
		Buy,

		/// <summary>
		/// Sell when price crosses downward.
		/// </summary>
		Sell,

		/// <summary>
		/// Trade in both directions depending on cross.
		/// </summary>
		Both
	}}