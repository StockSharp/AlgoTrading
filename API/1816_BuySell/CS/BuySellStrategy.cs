using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Buy/Sell strategy based on moving average direction and ATR.
/// Opens long when the moving average turns up and short when it turns down.
/// Includes optional stop-loss and take-profit in price points.
/// </summary>
public class BuySellStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BuySellStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Moving average period", "Indicator")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicator")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 1000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 2000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk")
			.SetCanOptimize(true);

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
			.SetDisplay("Allow Long Entry", "Permission to open long positions", "Permissions");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
			.SetDisplay("Allow Short Entry", "Permission to open short positions", "Permissions");

		_allowLongExit = Param(nameof(AllowLongExit), true)
			.SetDisplay("Allow Long Exit", "Permission to close long positions", "Permissions");

		_allowShortExit = Param(nameof(AllowShortExit), true)
			.SetDisplay("Allow Short Exit", "Permission to close short positions", "Permissions");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator calculation", "General");
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

		var ma = new SMA { Length = MaPeriod };
		var atr = new ATR { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);

		var prevMa = 0m;
		var prevUp = 0m;
		var prevDn = 0m;

		subscription
			.Bind(ma, atr, (candle, maValue, atrValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				if (!IsFormedAndOnlineAndAllowTrading())
					return;

				var up = 0m;
				var dn = 0m;

				if (maValue > prevMa)
					dn = maValue - atrValue;
				else if (maValue < prevMa)
					up = maValue + atrValue;

				if (up != 0m)
				{
					if (AllowLongEntry && prevDn != 0m && Position <= 0)
					{
						_entryPrice = candle.ClosePrice;
						BuyMarket(Volume + Math.Abs(Position));
					}

					if (AllowShortExit && Position < 0)
						BuyMarket(Math.Abs(Position));
				}
				else if (dn != 0m)
				{
					if (AllowShortEntry && prevUp != 0m && Position >= 0)
					{
						_entryPrice = candle.ClosePrice;
						SellMarket(Volume + Math.Abs(Position));
					}

					if (AllowLongExit && Position > 0)
						SellMarket(Math.Abs(Position));
				}

				if (Position != 0)
					CheckStops(candle.ClosePrice);

				prevMa = maValue;
				prevUp = up;
				prevDn = dn;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void CheckStops(decimal currentPrice)
	{
		if (_entryPrice == 0m)
			return;

		var step = Security?.PriceStep ?? 1m;

		if (Position > 0)
		{
			if (StopLoss > 0)
			{
				var stop = _entryPrice - StopLoss * step;
				if (currentPrice <= stop)
					SellMarket(Position);
			}

			if (TakeProfit > 0)
			{
				var take = _entryPrice + TakeProfit * step;
				if (currentPrice >= take)
					SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			if (StopLoss > 0)
			{
				var stop = _entryPrice + StopLoss * step;
				if (currentPrice >= stop)
					BuyMarket(-Position);
			}

			if (TakeProfit > 0)
			{
				var take = _entryPrice - TakeProfit * step;
				if (currentPrice <= take)
					BuyMarket(-Position);
			}
		}
	}
}
