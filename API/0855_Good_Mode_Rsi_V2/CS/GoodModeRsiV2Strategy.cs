using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on RSI extremes with trailing stop.
/// </summary>
public class GoodModeRsiV2Strategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _sellLevel;
	private readonly StrategyParam<decimal> _buyLevel;
	private readonly StrategyParam<decimal> _tpSellLevel;
	private readonly StrategyParam<decimal> _tpBuyLevel;
	private readonly StrategyParam<decimal> _trailingStopOffset;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _trailingStopPrice;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level to open short.
	/// </summary>
	public decimal SellLevel
	{
		get => _sellLevel.Value;
		set => _sellLevel.Value = value;
	}

	/// <summary>
	/// RSI level to open long.
	/// </summary>
	public decimal BuyLevel
	{
		get => _buyLevel.Value;
		set => _buyLevel.Value = value;
	}

	/// <summary>
	/// RSI level to close short.
	/// </summary>
	public decimal TakeProfitLevelSell
	{
		get => _tpSellLevel.Value;
		set => _tpSellLevel.Value = value;
	}

	/// <summary>
	/// RSI level to close long.
	/// </summary>
	public decimal TakeProfitLevelBuy
	{
		get => _tpBuyLevel.Value;
		set => _tpBuyLevel.Value = value;
	}

	/// <summary>
	/// Trailing stop offset in ticks.
	/// </summary>
	public decimal TrailingStopOffset
	{
		get => _trailingStopOffset.Value;
		set => _trailingStopOffset.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="GoodModeRsiV2Strategy"/>.
	/// </summary>
	public GoodModeRsiV2Strategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_sellLevel = Param(nameof(SellLevel), 96m)
			.SetRange(0m, 100m)
			.SetDisplay("Sell Level", "RSI level to open short", "General")
			.SetCanOptimize(true)
			.SetOptimize(60m, 100m, 5m);

		_buyLevel = Param(nameof(BuyLevel), 4m)
			.SetRange(0m, 100m)
			.SetDisplay("Buy Level", "RSI level to open long", "General")
			.SetCanOptimize(true)
			.SetOptimize(0m, 40m, 5m);

		_tpSellLevel = Param(nameof(TakeProfitLevelSell), 20m)
			.SetRange(0m, 100m)
			.SetDisplay("TP Sell Level", "RSI level to close short", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0m, 60m, 5m);

		_tpBuyLevel = Param(nameof(TakeProfitLevelBuy), 80m)
			.SetRange(0m, 100m)
			.SetDisplay("TP Buy Level", "RSI level to close long", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(40m, 100m, 5m);

		_trailingStopOffset = Param(nameof(TrailingStopOffset), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop Offset", "Offset in ticks for trailing stop", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 10m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_trailingStopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = Security.PriceStep ?? 1m;

		if (Position > 0)
		{
			if (candle.HighPrice > _trailingStopPrice)
				_trailingStopPrice = candle.HighPrice;

			var stop = _trailingStopPrice - TrailingStopOffset * step;

			if (candle.LowPrice <= stop || rsi > TakeProfitLevelBuy)
			{
				SellMarket(Position);
				_trailingStopPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (_trailingStopPrice == 0m || candle.LowPrice < _trailingStopPrice)
				_trailingStopPrice = candle.LowPrice;

			var stop = _trailingStopPrice + TrailingStopOffset * step;

			if (candle.HighPrice >= stop || rsi < TakeProfitLevelSell)
			{
				BuyMarket(Math.Abs(Position));
				_trailingStopPrice = 0m;
			}
		}

		if (rsi > SellLevel && Position >= 0 && IsFormedAndOnlineAndAllowTrading())
		{
			SellMarket(Volume + Position);
			_trailingStopPrice = candle.HighPrice;
		}
		else if (rsi < BuyLevel && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
		{
			BuyMarket(Volume + Math.Abs(Position));
			_trailingStopPrice = candle.LowPrice;
		}
	}
}
