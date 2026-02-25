using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid-based mean reversion strategy inspired by Sea Dragon hedging approach.
/// Buys at grid levels below entry, sells at grid levels above, with EMA trend filter.
/// </summary>
public class SeaDragon2Strategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _gridPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _lastGridPrice;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public decimal GridPercent { get => _gridPercent.Value; set => _gridPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SeaDragon2Strategy()
	{
		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period for trend", "General");

		_gridPercent = Param(nameof(GridPercent), 0.5m)
			.SetDisplay("Grid %", "Grid spacing as price percent", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0;
		_lastGridPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		var gridStep = price * GridPercent / 100m;

		if (Position == 0)
		{
			// Enter based on EMA direction
			if (price > emaValue)
			{
				BuyMarket();
				_entryPrice = price;
				_lastGridPrice = price;
			}
			else if (price < emaValue)
			{
				SellMarket();
				_entryPrice = price;
				_lastGridPrice = price;
			}
			return;
		}

		if (_lastGridPrice == 0)
			_lastGridPrice = price;

		// Grid logic: add to position or take profit
		if (Position > 0)
		{
			// Take profit if price moves up by grid step from entry
			if (price >= _entryPrice + gridStep * 2)
			{
				SellMarket();
				_entryPrice = 0;
				_lastGridPrice = 0;
			}
			// Stop loss if too far below
			else if (price <= _entryPrice - gridStep * 4)
			{
				SellMarket();
				_entryPrice = 0;
				_lastGridPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (price <= _entryPrice - gridStep * 2)
			{
				BuyMarket();
				_entryPrice = 0;
				_lastGridPrice = 0;
			}
			else if (price >= _entryPrice + gridStep * 4)
			{
				BuyMarket();
				_entryPrice = 0;
				_lastGridPrice = 0;
			}
		}
	}
}
