using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on zero crossing of the price derivative.
/// The derivative is calculated as momentum divided by period and scaled by 100.
/// When the derivative switches sign the opposite position is opened and the current is closed.
/// </summary>
public class DerivativeZeroCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _derivativePeriod;
	private readonly StrategyParam<PriceTypeEnum> _priceType;
	private readonly StrategyParam<bool> _buyEntry;
	private readonly StrategyParam<bool> _sellEntry;
	private readonly StrategyParam<bool> _buyExit;
	private readonly StrategyParam<bool> _sellExit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private Momentum _momentum = null!;
	private decimal? _prevDerivative;

	/// <summary>
	/// Derivative smoothing period.
	/// </summary>
	public int DerivativePeriod
	{
		get => _derivativePeriod.Value;
		set => _derivativePeriod.Value = value;
	}

	/// <summary>
	/// Price type used in derivative calculation.
	/// </summary>
	public PriceTypeEnum PriceType
	{
		get => _priceType.Value;
		set => _priceType.Value = value;
	}

	/// <summary>
	/// Enable long entries.
	/// </summary>
	public bool BuyEntry
	{
		get => _buyEntry.Value;
		set => _buyEntry.Value = value;
	}

	/// <summary>
	/// Enable short entries.
	/// </summary>
	public bool SellEntry
	{
		get => _sellEntry.Value;
		set => _sellEntry.Value = value;
	}

	/// <summary>
	/// Enable closing long positions.
	/// </summary>
	public bool BuyExit
	{
		get => _buyExit.Value;
		set => _buyExit.Value = value;
	}

	/// <summary>
	/// Enable closing short positions.
	/// </summary>
	public bool SellExit
	{
		get => _sellExit.Value;
		set => _sellExit.Value = value;
	}

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DerivativeZeroCrossStrategy"/>.
	/// </summary>
	public DerivativeZeroCrossStrategy()
	{
		_derivativePeriod = Param(nameof(DerivativePeriod), 34)
			.SetGreaterThanZero()
			.SetDisplay("Derivative Period", "Smoothing period for derivative", "Indicator");

		_priceType = Param(nameof(PriceType), PriceTypeEnum.Weighted)
			.SetDisplay("Price Type", "Source price for derivative", "Indicator");

		_buyEntry = Param(nameof(BuyEntry), true)
			.SetDisplay("Buy Entry", "Allow long entries", "Trading");

		_sellEntry = Param(nameof(SellEntry), true)
			.SetDisplay("Sell Entry", "Allow short entries", "Trading");

		_buyExit = Param(nameof(BuyExit), true)
			.SetDisplay("Buy Exit", "Allow closing longs", "Trading");

		_sellExit = Param(nameof(SellExit), true)
			.SetDisplay("Sell Exit", "Allow closing shorts", "Trading");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk")
			.SetGreaterThanZero();

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Take profit in points", "Risk")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevDerivative = null;
		_momentum = null!;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_momentum = new Momentum { Length = DerivativePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Point),
			stopLoss: new Unit(StopLoss, UnitTypes.Point),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _momentum);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Only process finished candles
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = GetPrice(candle);
		var momentumValue = _momentum.Process(price, candle.OpenTime, true).ToDecimal();

		var derivative = momentumValue / DerivativePeriod * 100m;

		if (_prevDerivative is null)
		{
			_prevDerivative = derivative;
			return;
		}

		var prev = _prevDerivative.Value;

		// Derivative crossed down through zero
		if (prev > 0m && derivative <= 0m)
		{
			if (SellExit && Position < 0)
				BuyMarket(Math.Abs(Position));
			if (BuyEntry && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		// Derivative crossed up through zero
		else if (prev < 0m && derivative >= 0m)
		{
			if (BuyExit && Position > 0)
				SellMarket(Position);
			if (SellEntry && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevDerivative = derivative;
	}

	private decimal GetPrice(ICandleMessage candle)
	{
		return PriceType switch
		{
			PriceTypeEnum.Close => candle.ClosePrice,
			PriceTypeEnum.Open => candle.OpenPrice,
			PriceTypeEnum.High => candle.HighPrice,
			PriceTypeEnum.Low => candle.LowPrice,
			PriceTypeEnum.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			PriceTypeEnum.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			PriceTypeEnum.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}
}

/// <summary>
/// Price type used for derivative calculation.
/// </summary>
public enum PriceTypeEnum
{
	Close,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted
}
