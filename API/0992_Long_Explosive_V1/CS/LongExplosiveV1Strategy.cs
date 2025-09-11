using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long Explosive V1 strategy enters long positions on strong price increases and exits on sharp drops.
/// </summary>
public class LongExplosiveV1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _priceIncreasePercent;
	private readonly StrategyParam<decimal> _priceDecreasePercent;

	private decimal _previousClose;
	private bool _isFirst = true;

	/// <summary>
	/// Candle type for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Price increase percentage to enter a long position.
	/// </summary>
	public decimal PriceIncreasePercent
	{
		get => _priceIncreasePercent.Value;
		set => _priceIncreasePercent.Value = value;
	}

	/// <summary>
	/// Price decrease percentage to exit.
	/// </summary>
	public decimal PriceDecreasePercent
	{
		get => _priceDecreasePercent.Value;
		set => _priceDecreasePercent.Value = value;
	}

	public LongExplosiveV1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
		_priceIncreasePercent = Param(nameof(PriceIncreasePercent), 1m)
			.SetDisplay("Price increase (%)", "Percentage increase to enter long", "General")
			.SetCanOptimize(true);
		_priceDecreasePercent = Param(nameof(PriceDecreasePercent), 1m)
			.SetDisplay("Price decrease (%)", "Percentage decrease to exit", "General")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirst)
		{
			_previousClose = candle.ClosePrice;
			_isFirst = false;
			return;
		}

		var change = candle.ClosePrice - _previousClose;
		var longLimit = candle.ClosePrice * PriceIncreasePercent / 100m;
		var shortLimit = candle.ClosePrice * PriceDecreasePercent / 100m;

		if (change < -shortLimit)
			ClosePosition();

		if (change > longLimit)
		{
			ClosePosition();
			BuyMarket();
		}

		_previousClose = candle.ClosePrice;
	}
}

