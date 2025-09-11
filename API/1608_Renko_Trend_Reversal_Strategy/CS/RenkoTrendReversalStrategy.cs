using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ATR-based renko trend reversal strategy with optional stop loss and take profit.
/// </summary>
public class RenkoTrendReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _renkoAtrLength;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<bool> _enableSignals;
	private readonly StrategyParam<bool> _enableSlTp;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<decimal> _volume;

	private DataType _renkoType;
	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrev;

	private decimal? _stopLoss;
	private decimal? _takeProfit;

	/// <summary>
	/// ATR period used to calculate renko brick size.
	/// </summary>
	public int RenkoAtrLength
	{
		get => _renkoAtrLength.Value;
		set => _renkoAtrLength.Value = value;
	}

	/// <summary>
	/// Start date of trading.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// End date of trading.
	/// </summary>
	public DateTimeOffset EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	/// <summary>
	/// Enable trade signals.
	/// </summary>
	public bool EnableSignals
	{
		get => _enableSignals.Value;
		set => _enableSignals.Value = value;
	}

	/// <summary>
	/// Enable stop loss and take profit.
	/// </summary>
	public bool EnableSlTp
	{
		get => _enableSlTp.Value;
		set => _enableSlTp.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPct
	{
		get => _takeProfitPct.Value;
		set => _takeProfitPct.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="RenkoTrendReversalStrategy"/>.
	/// </summary>
	public RenkoTrendReversalStrategy()
	{
		_renkoAtrLength = Param(nameof(RenkoAtrLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for renko brick size", "Renko")
			.SetCanOptimize(true);

		_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(2021, 1, 1)))
			.SetDisplay("Start Date", "Start date", "General");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(2025, 12, 31, 23, 59, 0)))
			.SetDisplay("End Date", "End date", "General");

		_enableSignals = Param(nameof(EnableSignals), true)
			.SetDisplay("Enable Signals", "Enable trade signals", "Trading");

		_enableSlTp = Param(nameof(EnableSlTp), true)
			.SetDisplay("Enable SL/TP", "Enable stop loss and take profit", "Trading");

		_stopLossPct = Param(nameof(StopLossPct), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Trading")
			.SetCanOptimize(true);

		_takeProfitPct = Param(nameof(TakeProfitPct), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Trading")
			.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		_renkoType ??= DataType.Create(typeof(RenkoCandleMessage), new RenkoCandleArg
		{
			BuildFrom = RenkoBuildFrom.Atr,
			Length = RenkoAtrLength
		});

		return [(Security, _renkoType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(_renkoType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (EnableSignals)
		{
			var timeOk = candle.OpenTime >= StartDate && candle.OpenTime <= EndDate;

			if (timeOk)
			{
				var buySignal = _hasPrev && _prevOpen > _prevClose && candle.OpenPrice < candle.ClosePrice;
				var sellSignal = _hasPrev && _prevOpen < _prevClose && candle.OpenPrice > candle.ClosePrice;

				if (buySignal && Position <= 0)
				{
					var volume = Volume + (Position < 0 ? -Position : 0m);
					BuyMarket(volume);

					if (EnableSlTp)
					{
						_stopLoss = candle.OpenPrice * (1 - StopLossPct / 100m);
						_takeProfit = candle.OpenPrice * (1 + TakeProfitPct / 100m);
					}
				}
				else if (sellSignal && Position >= 0)
				{
					var volume = Volume + (Position > 0 ? Position : 0m);
					SellMarket(volume);

					if (EnableSlTp)
					{
						_stopLoss = candle.OpenPrice * (1 + StopLossPct / 100m);
						_takeProfit = candle.OpenPrice * (1 - TakeProfitPct / 100m);
					}
				}
			}
		}

		if (EnableSlTp && _stopLoss != null && _takeProfit != null)
		{
			if (Position > 0)
			{
				if (candle.LowPrice <= _stopLoss || candle.HighPrice >= _takeProfit)
					SellMarket(Position);
			}
			else if (Position < 0)
			{
				if (candle.HighPrice >= _stopLoss || candle.LowPrice <= _takeProfit)
					BuyMarket(-Position);
			}
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_hasPrev = true;
	}
}

