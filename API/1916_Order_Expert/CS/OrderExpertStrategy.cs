using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Order processing strategy that allows opening positions at predefined price levels
/// and manages stop-loss, take-profit and trailing stop rules.
/// </summary>
public class OrderExpertStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPip;
	private readonly StrategyParam<decimal> _stopLossPip;
	private readonly StrategyParam<bool> _enableTrailingStop;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _buyLevel;
	private readonly StrategyParam<decimal> _sellLevel;

	private decimal _entryPrice;
	private decimal _takeProfitPrice;
	private decimal _stopLossPrice;
	private decimal _trailingStopPrice;
	private bool _isLong;
	private decimal _pipValue;

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPip
	{
		get => _takeProfitPip.Value;
		set => _takeProfitPip.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPip
	{
		get => _stopLossPip.Value;
		set => _stopLossPip.Value = value;
	}

	/// <summary>
	/// Enables trailing stop logic.
	/// </summary>
	public bool EnableTrailingStop
	{
		get => _enableTrailingStop.Value;
		set => _enableTrailingStop.Value = value;
	}

	/// <summary>
	/// Candle type used for price tracking.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Price level that triggers a long entry. Set to 0 to disable.
	/// </summary>
	public decimal BuyLevel
	{
		get => _buyLevel.Value;
		set => _buyLevel.Value = value;
	}

	/// <summary>
	/// Price level that triggers a short entry. Set to 0 to disable.
	/// </summary>
	public decimal SellLevel
	{
		get => _sellLevel.Value;
		set => _sellLevel.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="OrderExpertStrategy"/>.
	/// </summary>
	public OrderExpertStrategy()
	{
		_takeProfitPip = Param(nameof(TakeProfitPip), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Distance to take profit in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50m, 200m, 10m);

		_stopLossPip = Param(nameof(StopLossPip), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Distance to stop loss in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 10m);

		_enableTrailingStop = Param(nameof(EnableTrailingStop), true)
			.SetDisplay("Enable Trailing", "Enable trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_buyLevel = Param(nameof(BuyLevel), 0m)
			.SetDisplay("Buy Level", "Price level for long entry", "Trading");

		_sellLevel = Param(nameof(SellLevel), 0m)
			.SetDisplay("Sell Level", "Price level for short entry", "Trading");
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
		_takeProfitPrice = 0m;
		_stopLossPrice = 0m;
		_trailingStopPrice = 0m;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_pipValue = Security.PriceStep ?? 1m;

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
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;

		if (Position == 0)
		{
			TryOpenPosition(price);
		}
		else if (_isLong)
		{
			if (price <= _stopLossPrice || price >= _takeProfitPrice)
			{
				SellMarket(Position);
			}
			else if (EnableTrailingStop)
			{
				var newStop = price - StopLossPip * _pipValue;
				if (newStop > _trailingStopPrice)
				{
					_stopLossPrice = _trailingStopPrice = newStop;
				}
			}
		}
		else
		{
			if (price >= _stopLossPrice || price <= _takeProfitPrice)
			{
				BuyMarket(-Position);
			}
			else if (EnableTrailingStop)
			{
				var newStop = price + StopLossPip * _pipValue;
				if (newStop < _trailingStopPrice)
				{
					_stopLossPrice = _trailingStopPrice = newStop;
				}
			}
		}
	}

	private void TryOpenPosition(decimal price)
	{
		if (BuyLevel > 0 && price >= BuyLevel)
		{
			_isLong = true;
			_entryPrice = price;
			_stopLossPrice = price - StopLossPip * _pipValue;
			_takeProfitPrice = price + TakeProfitPip * _pipValue;
			_trailingStopPrice = _stopLossPrice;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (SellLevel > 0 && price <= SellLevel)
		{
			_isLong = false;
			_entryPrice = price;
			_stopLossPrice = price + StopLossPip * _pipValue;
			_takeProfitPrice = price - TakeProfitPip * _pipValue;
			_trailingStopPrice = _stopLossPrice;
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}

