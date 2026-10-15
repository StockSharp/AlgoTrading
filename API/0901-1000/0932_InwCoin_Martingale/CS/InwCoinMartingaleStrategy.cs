using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale strategy for Bitcoin with MACD entry signal.
/// Enters long on MACD histogram crossover, averages down on price drops,
/// and exits on take profit.
/// </summary>
public class InwCoinMartingaleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _martingalePercent;
	private readonly StrategyParam<decimal> _martingaleMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _avgPrice;
	private int _martingaleCount;
	private decimal _prevHistogram;
	private bool _isFirst = true;

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Percent drop to trigger martingale.
	/// </summary>
	public decimal MartingalePercent
	{
		get => _martingalePercent.Value;
		set => _martingalePercent.Value = value;
	}

	/// <summary>
	/// Multiplier for each martingale step.
	/// </summary>
	public decimal MartingaleMultiplier
	{
		get => _martingaleMultiplier.Value;
		set => _martingaleMultiplier.Value = value;
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
	/// Initializes a new instance of <see cref="InwCoinMartingaleStrategy"/>.
	/// </summary>
	public InwCoinMartingaleStrategy()
	{
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetDisplay("Take Profit %", "Profit percent to exit.", "Parameters");

		_martingalePercent = Param(nameof(MartingalePercent), 5m)
			.SetDisplay("Martingale %", "Price drop percent for averaging.", "Parameters");

		_martingaleMultiplier = Param(nameof(MartingaleMultiplier), 2m)
			.SetDisplay("Multiplier", "Volume multiplier for martingale.", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles.", "General");
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
		_avgPrice = 0m;
		_martingaleCount = 0;
		_prevHistogram = 0m;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_avgPrice = 0m;
		_martingaleCount = 0;
		_prevHistogram = 0m;
		_isFirst = true;

		var emaShort = new ExponentialMovingAverage { Length = 12 };
		var emaLong = new ExponentialMovingAverage { Length = 26 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaShort, emaLong, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaShort);
			DrawIndicator(area, emaLong);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaShortVal, decimal emaLongVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		var histogram = emaShortVal - emaLongVal;

		if (_isFirst)
		{
			_prevHistogram = histogram;
			_isFirst = false;
			return;
		}

		// MACD histogram crossover as entry signal
		var buySignal = _prevHistogram <= 0 && histogram > 0;

		if (buySignal && Position <= 0 && _martingaleCount == 0)
		{
			BuyMarket();
			_avgPrice = price;
			_martingaleCount = 1;
		}
		else if (Position > 0 && _avgPrice > 0)
		{
			// Check for martingale averaging down
			var drop = (price - _avgPrice) / _avgPrice * 100m;
			if (drop <= -MartingalePercent && _martingaleCount < 5)
			{
				BuyMarket();
				_avgPrice = ((_avgPrice * (Position)) + price) / (Position + 1);
				_martingaleCount++;
			}

			// Check take profit
			var profit = (price - _avgPrice) / _avgPrice * 100m;
			if (profit >= TakeProfitPercent)
			{
				SellMarket();
				_martingaleCount = 0;
				_avgPrice = 0m;
			}
		}

		_prevHistogram = histogram;
	}
}
