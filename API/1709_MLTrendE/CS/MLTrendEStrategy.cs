namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Weighted moving average trend strategy with position pyramiding.
/// </summary>
public class MLTrendEStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _multiplier1;
	private readonly StrategyParam<decimal> _multiplier2;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<int> _map;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<int> _tradeType;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastEntryPrice;
	private int _tradeCount;

	/// <summary>
	/// Base trade volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Volume multiplier for the second trade.
	/// </summary>
	public decimal Multiplier1
	{
		get => _multiplier1.Value;
		set => _multiplier1.Value = value;
	}

	/// <summary>
	/// Volume multiplier for the third trade.
	/// </summary>
	public decimal Multiplier2
	{
		get => _multiplier2.Value;
		set => _multiplier2.Value = value;
	}

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Weighted moving average length.
	/// </summary>
	public int Map
	{
		get => _map.Value;
		set => _map.Value = value;
	}

	/// <summary>
	/// Maximum number of consecutive trades.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Trade direction control.
	/// 0 - follow trend; 1 - force buy; 2 - force sell.
	/// </summary>
	public int TradeType
	{
		get => _tradeType.Value;
		set => _tradeType.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MLTrendEStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Initial trade volume", "General");

		_multiplier1 = Param(nameof(Multiplier1), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier 1", "Second trade multiplier", "General");

		_multiplier2 = Param(nameof(Multiplier2), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier 2", "Third trade multiplier", "General");

		_takeProfit = Param(nameof(TakeProfit), 600m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Profit in price units for scaling or exit", "General");

		_map = Param(nameof(Map), 34)
			.SetGreaterThanZero()
			.SetDisplay("WMA Length", "Weighted moving average period", "General");

		_maxTrades = Param(nameof(MaxTrades), 4)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum number of sequential trades", "General");

		_tradeType = Param(nameof(TradeType), 0)
			.SetDisplay("Trade Type", "0 - trend; 1 - buy only; 2 - sell only", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1440).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
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
		_lastEntryPrice = 0m;
		_tradeCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var wma = new WeightedMovingAverage { Length = Map };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(wma, ProcessCandle).Start();
	}
	private void ProcessCandle(ICandleMessage candle, decimal wmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (_tradeCount == 0)
		{
			switch (TradeType)
			{
				case 1:
					BuyMarket(Volume);
					_lastEntryPrice = close;
					_tradeCount = 1;
					return;
				case 2:
					SellMarket(Volume);
					_lastEntryPrice = close;
					_tradeCount = 1;
					return;
			}

			if (close > wmaValue)
			{
				BuyMarket(Volume);
				_lastEntryPrice = close;
				_tradeCount = 1;
			}
			else if (close < wmaValue)
			{
				SellMarket(Volume);
				_lastEntryPrice = close;
				_tradeCount = 1;
			}

			return;
		}

		var profit = Position > 0 ? close - _lastEntryPrice : _lastEntryPrice - close;

		if (profit < TakeProfit)
			return;

		if (_tradeCount < MaxTrades)
		{
			var mult = _tradeCount == 1 ? Multiplier1 : Multiplier2;
			var vol = Volume * mult;

			if (Position > 0)
				BuyMarket(vol);
			else
				SellMarket(vol);

			_lastEntryPrice = close;
			_tradeCount++;
		}
		else
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);

			_lastEntryPrice = 0m;
			_tradeCount = 0;
		}
	}
}
