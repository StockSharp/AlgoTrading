using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class RandomCoinTossStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _slMultiplier;
	private readonly StrategyParam<decimal> _tpMultiplier;
	private readonly StrategyParam<int> _entryFrequency;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Random _random = new();
	private AverageTrueRange _atr;

	private int _barIndex;
	private bool _isLong;
	private decimal _entryPrice;
	private decimal _stopLoss;
	private decimal _takeProfit;

	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal SlMultiplier { get => _slMultiplier.Value; set => _slMultiplier.Value = value; }
	public decimal TpMultiplier { get => _tpMultiplier.Value; set => _tpMultiplier.Value = value; }
	public int EntryFrequency { get => _entryFrequency.Value; set => _entryFrequency.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RandomCoinTossStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_slMultiplier = Param(nameof(SlMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("SL Multiplier", "Stop loss = ATR * multiplier", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_tpMultiplier = Param(nameof(TpMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("TP Multiplier", "Take profit = ATR * multiplier", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_entryFrequency = Param(nameof(EntryFrequency), 10)
			.SetGreaterThanZero()
			.SetDisplay("Entry Frequency", "Number of bars between coin tosses", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_barIndex = 0;
		_isLong = false;
		_entryPrice = 0m;
		_stopLoss = 0m;
		_takeProfit = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(_atr, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		if (!_atr.IsFormed)
			return;

		if (_barIndex % EntryFrequency == 0 && Position == 0 && IsFormedAndOnlineAndAllowTrading())
		{
			var isBuy = _random.NextDouble() > 0.5;

			_entryPrice = candle.ClosePrice;
			_stopLoss = isBuy ? _entryPrice - atrValue * SlMultiplier : _entryPrice + atrValue * SlMultiplier;
			_takeProfit = isBuy ? _entryPrice + atrValue * TpMultiplier : _entryPrice - atrValue * TpMultiplier;
			_isLong = isBuy;

			if (isBuy)
				BuyMarket();
			else
				SellMarket();
		}

		if (_isLong && Position > 0)
		{
			if (candle.LowPrice <= _stopLoss || candle.HighPrice >= _takeProfit)
				SellMarket(Position);
		}
		else if (!_isLong && Position < 0)
		{
			if (candle.HighPrice >= _stopLoss || candle.LowPrice <= _takeProfit)
				BuyMarket(-Position);
		}
	}
}

