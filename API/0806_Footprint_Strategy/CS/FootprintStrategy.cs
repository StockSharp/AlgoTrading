using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Footprint strategy based on buy/sell volume imbalance.
/// </summary>
public class FootprintStrategy : Strategy
{
	private readonly StrategyParam<int> _imbalancePercent;
	private readonly StrategyParam<bool> _useTrend;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _enableStopLoss;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private decimal _buyVolume;
	private decimal _sellVolume;
	private decimal? _prevTrendClose;
	private bool _trendUp = true;

	/// <summary>
	/// Required imbalance percentage.
	/// </summary>
	public int ImbalancePercent
	{
		get => _imbalancePercent.Value;
		set => _imbalancePercent.Value = value;
	}

	/// <summary>
	/// Enable trend filter.
	/// </summary>
	public bool UseTrend
	{
		get => _useTrend.Value;
		set => _useTrend.Value = value;
	}

	/// <summary>
	/// Candle type for footprint calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type for trend detection.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Enable stop loss.
	/// </summary>
	public bool EnableStopLoss
	{
		get => _enableStopLoss.Value;
		set => _enableStopLoss.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="FootprintStrategy"/>.
	/// </summary>
	public FootprintStrategy()
	{
		_imbalancePercent = Param(nameof(ImbalancePercent), 300)
			.SetGreaterThanZero()
			.SetDisplay("Imbalance %", "Required buy/sell volume imbalance", "General")
			.SetCanOptimize(true)
			.SetOptimize(100, 500, 100);

		_useTrend = Param(nameof(UseTrend), true)
			.SetDisplay("Use Trend", "Enable daily trend filter", "Trend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromSeconds(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for footprint calculation", "General");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Trend Candle", "Timeframe for trend filter", "Trend");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss as percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_enableStopLoss = Param(nameof(EnableStopLoss), false)
			.SetDisplay("Enable Stop Loss", "Use stop loss protection", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit as percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, DataType.Ticks);
		yield return (Security, TrendCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_buyVolume = 0;
		_sellVolume = 0;
		_prevTrendClose = null;
		_trendUp = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var candleSub = SubscribeCandles(CandleType);
		candleSub
			.Bind(ProcessCandle)
			.Start();

		SubscribeTicks()
			.Bind(trade =>
			{
				if (trade.OriginSide == Sides.Buy)
					_buyVolume += trade.Volume;
				else
					_sellVolume += trade.Volume;
			})
			.Start();

		SubscribeCandles(TrendCandleType)
			.Bind(ProcessTrendCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent * 100m, UnitTypes.Percent),
			stopLoss: EnableStopLoss ? new Unit(StopLossPercent * 100m, UnitTypes.Percent) : null
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessTrendCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevTrendClose is decimal prev)
			_trendUp = candle.ClosePrice > prev;

		_prevTrendClose = candle.ClosePrice;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var imbalanceRatio = _sellVolume > 0 ? _buyVolume / _sellVolume : decimal.MaxValue;
		var imbalancePercent = (imbalanceRatio - 1m) * 100m;

		LogInfo($"Buy Vol: {_buyVolume}, Sell Vol: {_sellVolume}, Imbalance %: {imbalancePercent}");

		if (imbalancePercent >= ImbalancePercent && candle.ClosePrice < candle.OpenPrice && (!UseTrend || _trendUp) && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}

		_buyVolume = 0;
		_sellVolume = 0;
	}
}
