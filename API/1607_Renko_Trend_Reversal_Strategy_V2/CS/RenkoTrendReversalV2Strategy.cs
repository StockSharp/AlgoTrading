using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ATR-based renko trend reversal strategy with optional shorts and time filters.
/// </summary>
public class RenkoTrendReversalV2Strategy : Strategy
{
	private readonly StrategyParam<int> _renkoAtrLength;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;
	private readonly StrategyParam<bool> _enableShorts;
	private readonly StrategyParam<bool> _enableSignals;
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
	/// Enable short trades.
	/// </summary>
	public bool EnableShorts
	{
		get => _enableShorts.Value;
		set => _enableShorts.Value = value;
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
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="RenkoTrendReversalV2Strategy"/>.
	/// </summary>
	public RenkoTrendReversalV2Strategy()
	{
		_renkoAtrLength = Param(nameof(RenkoAtrLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for renko brick size", "Renko")
			.SetCanOptimize(true);

		_stopLossPct = Param(nameof(StopLossPct), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Trading")
			.SetCanOptimize(true);

		_takeProfitPct = Param(nameof(TakeProfitPct), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Trading")
			.SetCanOptimize(true);

		_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(2023, 7, 1)))
			.SetDisplay("Start Date", "Start date", "General");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(2025, 12, 31, 23, 59, 0)))
			.SetDisplay("End Date", "End date", "General");

		_enableShorts = Param(nameof(EnableShorts), true)
			.SetDisplay("Enable Shorts", "Allow short trades", "Trading");

		_enableSignals = Param(nameof(EnableSignals), true)
			.SetDisplay("Enable Signals", "Enable trade signals", "Trading");

		_volume = Param(nameof(Volume), 5m)
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

					_stopLoss = candle.OpenPrice * (1 - StopLossPct / 100m);
					_takeProfit = candle.OpenPrice * (1 + TakeProfitPct / 100m);
				}
				else if (sellSignal && EnableShorts && Position >= 0)
				{
					var volume = Volume + (Position > 0 ? Position : 0m);
					SellMarket(volume);

					_stopLoss = candle.OpenPrice * (1 + StopLossPct / 100m);
					_takeProfit = candle.OpenPrice * (1 - TakeProfitPct / 100m);
				}
			}
		}

		if (Position > 0 && _stopLoss != null && _takeProfit != null)
		{
			if (candle.LowPrice <= _stopLoss || candle.HighPrice >= _takeProfit)
				SellMarket(Position);
		}
		else if (Position < 0 && _stopLoss != null && _takeProfit != null)
		{
			if (candle.HighPrice >= _stopLoss || candle.LowPrice <= _takeProfit)
				BuyMarket(-Position);
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_hasPrev = true;
	}
}

