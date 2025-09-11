using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that sizes positions based on risk and trades Keltner Channel breakouts.
/// </summary>
public class PropFirmBusinessSimulatorStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private AverageTrueRange _atr;

	/// <summary>
	/// Moving average period for the channel basis.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// ATR period for channel width.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for ATR to form channel bands.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Risk per trade as a percent of equity.
	/// </summary>
	public decimal RiskPerTrade
	{
		get => _riskPerTrade.Value;
		set => _riskPerTrade.Value = value;
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
	/// Initialize the strategy.
	/// </summary>
	public PropFirmBusinessSimulatorStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
		.SetDisplay("MA Period", "Moving average period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 5);

		_atrPeriod = Param(nameof(AtrPeriod), 10)
		.SetDisplay("ATR Period", "ATR period for channel width", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_multiplier = Param(nameof(Multiplier), 2m)
		.SetDisplay("Multiplier", "ATR multiplier", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);

		_riskPerTrade = Param(nameof(RiskPerTrade), 1m)
		.SetDisplay("Risk %", "Risk per trade as percent of equity", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_ema = null;
		_atr = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = MaPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_ema, _atr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_ema.IsFormed || !_atr.IsFormed)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var upper = maValue + Multiplier * atrValue;
		var lower = maValue - Multiplier * atrValue;

		if (upper <= lower)
		return;

		var equity = Portfolio.CurrentValue ?? 0m;
		var targetRisk = Math.Abs(equity * (RiskPerTrade / 100m));

		var bandWidth = Math.Abs(upper - lower);
		if (bandWidth == 0m)
		return;

		var qty = Math.Round(targetRisk / bandWidth, 2);
		if (qty <= 0m)
		return;

		CancelActiveOrders();

		var buyVolume = qty + Math.Max(-Position, 0m);
		var sellVolume = qty + Math.Max(Position, 0m);

		if (candle.HighPrice <= upper)
		BuyStop(buyVolume, upper);

		if (candle.LowPrice >= lower)
		SellStop(sellVolume, lower);
	}
}
