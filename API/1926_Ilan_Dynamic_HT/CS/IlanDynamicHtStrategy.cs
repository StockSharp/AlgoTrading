namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Ilan Dynamic HT Strategy.
/// </summary>
public class IlanDynamicHtStrategy : Strategy
{
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<bool> _dynamicPips;
	private readonly StrategyParam<int> _defaultPips;
	private readonly StrategyParam<int> _depth;
	private readonly StrategyParam<int> _del;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiMin;
	private readonly StrategyParam<decimal> _rsiMax;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private Highest _highest;
	private Lowest _lowest;

	private decimal _avgPrice;
	private decimal _lastPrice;
	private decimal _totalVolume;
	private int _tradeCount;
	private decimal _step;

	public IlanDynamicHtStrategy()
	{
		_lotExponent = Param(nameof(LotExponent), 1.4m)
			.SetDisplay("Lot Exponent", "Multiplier for next position volume", "General");

		_maxTrades = Param(nameof(MaxTrades), 10)
			.SetDisplay("Max Trades", "Maximum simultaneous trades", "General");

		_dynamicPips = Param(nameof(DynamicPips), true)
			.SetDisplay("Dynamic Range", "Use dynamic price range", "General");

		_defaultPips = Param(nameof(DefaultPips), 120)
			.SetDisplay("Default Range", "Static price range in points", "General");

		_depth = Param(nameof(Depth), 24)
			.SetDisplay("Depth", "Number of bars for range calculation", "General");

		_del = Param(nameof(Del), 3)
			.SetDisplay("Divider", "Range divider factor", "General");

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetDisplay("Base Volume", "Initial trade volume", "Trading");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "Period for RSI indicator", "Signals");

		_rsiMin = Param(nameof(RsiMinimum), 30m)
			.SetDisplay("RSI Minimum", "Lower RSI bound", "Signals");

		_rsiMax = Param(nameof(RsiMaximum), 70m)
			.SetDisplay("RSI Maximum", "Upper RSI bound", "Signals");

		_takeProfit = Param(nameof(TakeProfit), 100m)
			.SetDisplay("Take Profit", "Take profit in points", "Risk");

		_stopLoss = Param(nameof(StopLoss), 500m)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for processing", "General");
	}
	public decimal LotExponent { get => _lotExponent.Value; set => _lotExponent.Value = value; }
	public int MaxTrades { get => _maxTrades.Value; set => _maxTrades.Value = value; }
	public bool DynamicPips { get => _dynamicPips.Value; set => _dynamicPips.Value = value; }
	public int DefaultPips { get => _defaultPips.Value; set => _defaultPips.Value = value; }
	public int Depth { get => _depth.Value; set => _depth.Value = value; }
	public int Del { get => _del.Value; set => _del.Value = value; }
	public decimal BaseVolume { get => _baseVolume.Value; set => _baseVolume.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiMinimum { get => _rsiMin.Value; set => _rsiMin.Value = value; }
	public decimal RsiMaximum { get => _rsiMax.Value; set => _rsiMax.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_avgPrice = 0m;
		_lastPrice = 0m;
		_totalVolume = 0m;
		_tradeCount = 0;
		_step = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_highest = new Highest { Length = Depth };
		_lowest = new Lowest { Length = Depth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _highest, _lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal highestValue, decimal lowestValue)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.State != CandleStates.Finished)
			return;

		if (DynamicPips)
		{
			var range = highestValue - lowestValue;
			_step = range / Del;
		}
		else
		{
			_step = DefaultPips * (Security?.PriceStep ?? 1m);
		}

		// Entry signals
		if (Position == 0)
		{
			if (rsiValue <= RsiMinimum)
			{
				OpenPosition(true, candle.ClosePrice);
			}
			else if (rsiValue >= RsiMaximum)
			{
				OpenPosition(false, candle.ClosePrice);
			}

			return;
		}

		// Add positions when price moves against us
		if (_tradeCount < MaxTrades)
		{
			if (Position > 0 && candle.ClosePrice <= _lastPrice - _step)
			{
				AddPosition(true, candle.ClosePrice);
			}
			else if (Position < 0 && candle.ClosePrice >= _lastPrice + _step)
			{
				AddPosition(false, candle.ClosePrice);
			}
		}

		var profit = Position > 0 ? candle.ClosePrice - _avgPrice : _avgPrice - candle.ClosePrice;

		var takeProfit = TakeProfit * (Security?.PriceStep ?? 1m);
		var stopLoss = StopLoss * (Security?.PriceStep ?? 1m);

		if (profit >= takeProfit)
		{
			CloseAll();
		}
		else if (profit <= -stopLoss)
		{
			CloseAll();
		}
	}

	private void OpenPosition(bool isLong, decimal price)
	{
		var volume = BaseVolume;

		if (isLong)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_avgPrice = price;
		_lastPrice = price;
		_totalVolume = volume;
		_tradeCount = 1;
	}

	private void AddPosition(bool isLong, decimal price)
	{
		var volume = BaseVolume * (decimal)Math.Pow((double)LotExponent, _tradeCount);

		if (isLong)
			BuyMarket(volume);
		else
			SellMarket(volume);

		_avgPrice = (_avgPrice * _totalVolume + price * volume) / (_totalVolume + volume);
		_totalVolume += volume;
		_lastPrice = price;
		_tradeCount++;
	}

	private void CloseAll()
	{
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
		}

		_avgPrice = 0m;
		_lastPrice = 0m;
		_totalVolume = 0m;
		_tradeCount = 0;
	}
}
