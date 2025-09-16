using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku cloud retrace strategy.
/// Takes trades when price pulls back inside the cloud in the direction of the kumo slope.
/// Uses optional fixed offsets for stop-loss and take-profit management.
/// </summary>
public class IchimokuCloudRetraceStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<decimal> _stopLossOffset;
	private readonly StrategyParam<decimal> _takeProfitOffset;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	/// <summary>
	/// Tenkan-sen period.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun-sen period.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Senkou Span B period.
	/// </summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss offset in price units. Set to zero to disable.
	/// </summary>
	public decimal StopLossOffset
	{
		get => _stopLossOffset.Value;
		set => _stopLossOffset.Value = value;
	}

	/// <summary>
	/// Take-profit offset in price units. Set to zero to disable.
	/// </summary>
	public decimal TakeProfitOffset
	{
		get => _takeProfitOffset.Value;
		set => _takeProfitOffset.Value = value;
	}

	/// <summary>
	/// Candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public IchimokuCloudRetraceStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Tenkan-sen length", "Ichimoku Settings")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Kijun-sen length", "Ichimoku Settings")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span B Period", "Senkou Span B length", "Ichimoku Settings")
			.SetCanOptimize(true)
			.SetOptimize(40, 70, 5);

		_stopLossOffset = Param(nameof(StopLossOffset), 0m)
			.SetDisplay("Stop Loss Offset", "Distance from entry for stop-loss (price units)", "Risk Management");

		_takeProfitOffset = Param(nameof(TakeProfitOffset), 0m)
			.SetDisplay("Take Profit Offset", "Distance from entry for take-profit (price units)", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for analysis", "General");
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

		// Reset internal state values.
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Prepare Ichimoku indicator with user-defined lengths.
		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod }
		};

		// Subscribe to candle data and bind the indicator for processing.
		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(ichimoku, ProcessCandle)
			.Start();

		// Draw helper visuals if a chart is available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!ichimokuValue.IsFinal)
			return;

		// Manage open positions using the latest close before looking for new entries.
		ManageRisk(candle);

		if (Position == 0)
			_entryPrice = 0m;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ichimoku = (IchimokuValue)ichimokuValue;

		if (ichimoku.SenkouA is not decimal senkouA ||
			ichimoku.SenkouB is not decimal senkouB)
			return;

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		var lowerSpan = Math.Min(senkouA, senkouB);
		var upperSpan = Math.Max(senkouA, senkouB);

		var priceInsideCloud = close > lowerSpan && close < upperSpan;

		var bullishCloud = senkouA > senkouB;
		var bearishCloud = senkouB > senkouA;

		var shouldBuy = bullishCloud && close > open && priceInsideCloud;
		var shouldSell = bearishCloud && open > close && priceInsideCloud;

		if (shouldBuy && Position <= 0)
		{
			// Combine reversal and new entry volume in a single market order.
			var volume = Volume + (Position < 0 ? Math.Abs(Position) : 0m);

			if (volume > 0)
			{
				_entryPrice = close;
				BuyMarket(volume);
			}
		}
		else if (shouldSell && Position >= 0)
		{
			// Combine reversal and new entry volume in a single market order.
			var volume = Volume + (Position > 0 ? Math.Abs(Position) : 0m);

			if (volume > 0)
			{
				_entryPrice = close;
				SellMarket(volume);
			}
		}
	}

	private void ManageRisk(ICandleMessage candle)
	{
		if (_entryPrice == 0m)
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (StopLossOffset > 0m && close <= _entryPrice - StopLossOffset)
			{
				var volumeToClose = Math.Abs(Position);

				if (volumeToClose > 0m)
				{
					SellMarket(volumeToClose);
					_entryPrice = 0m;
					return;
				}
			}

			if (TakeProfitOffset > 0m && close >= _entryPrice + TakeProfitOffset)
			{
				var volumeToClose = Math.Abs(Position);

				if (volumeToClose > 0m)
				{
					SellMarket(volumeToClose);
					_entryPrice = 0m;
				}
			}
		}
		else if (Position < 0)
		{
			if (StopLossOffset > 0m && close >= _entryPrice + StopLossOffset)
			{
				var volumeToClose = Math.Abs(Position);

				if (volumeToClose > 0m)
				{
					BuyMarket(volumeToClose);
					_entryPrice = 0m;
					return;
				}
			}

			if (TakeProfitOffset > 0m && close <= _entryPrice - TakeProfitOffset)
			{
				var volumeToClose = Math.Abs(Position);

				if (volumeToClose > 0m)
				{
					BuyMarket(volumeToClose);
					_entryPrice = 0m;
				}
			}
		}
	}
}
