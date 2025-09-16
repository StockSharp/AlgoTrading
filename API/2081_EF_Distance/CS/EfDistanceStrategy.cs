
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EF Distance reversal strategy.
/// Uses a smoothed price series and ATR filter to trade turning points.
/// </summary>
public class EfDistanceStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrThreshold;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	/// <summary>
	/// SMA period.
	/// </summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Minimum ATR to allow entries.
	/// </summary>
	public decimal AtrThreshold
	{
		get => _atrThreshold.Value;
		set => _atrThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
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
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public EfDistanceStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Period for the smoothing moving average", "Indicator");

		_atrPeriod = Param(nameof(AtrPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for the volatility filter", "Indicator");

		_atrThreshold = Param(nameof(AtrThreshold), 1m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Threshold", "Minimum ATR value to allow entries", "Indicator");

		_stopLoss = Param(nameof(StopLoss), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Absolute stop loss distance", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Absolute take profit distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");
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
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var sma = new SMA { Length = SmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);

		decimal? prev = null;
		decimal? prev2 = null;

		subscription.Bind(sma, atr, (candle, smaValue, atrValue) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			if (!prev.HasValue || !prev2.HasValue)
			{
				prev2 = prev;
				prev = smaValue;
				return;
			}

			var atrEnough = atrValue >= AtrThreshold;

			if (atrEnough)
			{
				if (prev < prev2 && smaValue > prev)
				{
					if (Position <= 0)
					{
						BuyMarket(Volume + Math.Abs(Position));
						_entryPrice = candle.ClosePrice;
					}
				}
				else if (prev > prev2 && smaValue < prev)
				{
					if (Position >= 0)
					{
						SellMarket(Volume + Math.Abs(Position));
						_entryPrice = candle.ClosePrice;
					}
				}
			}

			if (Position > 0)
			{
				if (StopLoss > 0m && candle.ClosePrice <= _entryPrice - StopLoss)
					SellMarket(Position);
				else if (TakeProfit > 0m && candle.ClosePrice >= _entryPrice + TakeProfit)
					SellMarket(Position);
			}
			else if (Position < 0)
			{
				if (StopLoss > 0m && candle.ClosePrice >= _entryPrice + StopLoss)
					BuyMarket(-Position);
				else if (TakeProfit > 0m && candle.ClosePrice <= _entryPrice - TakeProfit)
					BuyMarket(-Position);
			}

			prev2 = prev;
			prev = smaValue;
		}).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}
}
