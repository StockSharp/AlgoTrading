using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI cross strategy with fixed take profit.
/// </summary>
public class RsiProPlusBearMarketStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLevel;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private decimal _entryPrice;
	private decimal _takeProfitLevel;
	private decimal _previousRsi;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level to trigger long entry.
	/// </summary>
	public decimal RsiLevel
	{
		get => _rsiLevel.Value;
		set => _rsiLevel.Value = value;
	}

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
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
	/// Initializes a new instance of <see cref="RsiProPlusBearMarketStrategy"/>.
	/// </summary>
	public RsiProPlusBearMarketStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "RSI Settings")
			.SetCanOptimize(true)
			.SetOptimize(2, 30, 1);

		_rsiLevel = Param(nameof(RsiLevel), 8m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Level", "RSI level to trigger long entry", "RSI Settings")
			.SetCanOptimize(true)
			.SetOptimize(0m, 50m, 1m);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.11m)
			.SetRange(0m, 100m)
			.SetDisplay("Take Profit %", "Percentage of entry price for take profit", "Exit Settings")
			.SetCanOptimize(true)
			.SetOptimize(0.01m, 5m, 0.01m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_entryPrice = 0m;
		_takeProfitLevel = 0m;
		_previousRsi = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_rsi, ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed)
		{
			_previousRsi = rsiValue;
			return;
		}

		if (Position > 0)
		{
			if (candle.ClosePrice >= _takeProfitLevel)
			{
				SellMarket(Position);
				_entryPrice = 0m;
				_takeProfitLevel = 0m;
			}
		}
		else if (_previousRsi < RsiLevel && rsiValue >= RsiLevel && IsFormedAndOnlineAndAllowTrading())
		{
			BuyMarket(Volume);
			_entryPrice = candle.ClosePrice;
			_takeProfitLevel = _entryPrice * (1m + TakeProfitPercent / 100m);
		}

		_previousRsi = rsiValue;
	}
}
