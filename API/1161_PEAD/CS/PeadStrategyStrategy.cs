namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Post-earnings announcement drift strategy with gap and EMA exit.
/// </summary>
public class PeadStrategyStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gapThreshold;
	private readonly StrategyParam<decimal> _epsSurpriseThreshold;
	private readonly StrategyParam<int> _perfDays;
	private readonly StrategyParam<decimal> _stopPct;
	private readonly StrategyParam<int> _emaLen;
	private readonly StrategyParam<int> _maxHoldBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevEma;
	private decimal _prevRoc;
	private decimal _stopLevel;
	private int _barsInTrade;

	/// <summary>
	/// Gap-up threshold (%).
	/// </summary>
	public decimal GapThreshold
	{
		get => _gapThreshold.Value;
		set => _gapThreshold.Value = value;
	}

	/// <summary>
	/// EPS surprise threshold (%).
	/// </summary>
	public decimal EpsSurpriseThreshold
	{
		get => _epsSurpriseThreshold.Value;
		set => _epsSurpriseThreshold.Value = value;
	}

	/// <summary>
	/// Positive-performance look-back.
	/// </summary>
	public int PerfDays
	{
		get => _perfDays.Value;
		set => _perfDays.Value = value;
	}

	/// <summary>
	/// Initial fixed stop-loss (%).
	/// </summary>
	public decimal StopPct
	{
		get => _stopPct.Value;
		set => _stopPct.Value = value;
	}

	/// <summary>
	/// Daily EMA length for trail stop.
	/// </summary>
	public int EmaLen
	{
		get => _emaLen.Value;
		set => _emaLen.Value = value;
	}

	/// <summary>
	/// Exit after N bars from entry.
	/// </summary>
	public int MaxHoldBars
	{
		get => _maxHoldBars.Value;
		set => _maxHoldBars.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public PeadStrategyStrategy()
	{
		_gapThreshold = Param(nameof(GapThreshold), 1m)
			.SetDisplay("Gap Threshold", "Gap-up threshold (%)", "General")
			.SetCanOptimize(true);

		_epsSurpriseThreshold = Param(nameof(EpsSurpriseThreshold), 5m)
			.SetDisplay("EPS Surprise", "EPS surprise threshold (%)", "General")
			.SetCanOptimize(true);

		_perfDays = Param(nameof(PerfDays), 20)
			.SetDisplay("Performance Days", "Positive-performance look-back", "General")
			.SetCanOptimize(true);

		_stopPct = Param(nameof(StopPct), 8m)
			.SetDisplay("Stop Percent", "Initial fixed stop-loss (%)", "Risk")
			.SetCanOptimize(true);

		_emaLen = Param(nameof(EmaLen), 50)
			.SetDisplay("EMA Length", "Daily EMA length for trail stop", "Indicators")
			.SetCanOptimize(true);

		_maxHoldBars = Param(nameof(MaxHoldBars), 50)
			.SetDisplay("Max Hold Bars", "Exit after N bars from entry", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevClose = 0;
		_prevEma = 0;
		_prevRoc = 0;
		_stopLevel = 0;
		_barsInTrade = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new ExponentialMovingAverage { Length = EmaLen };
		var roc = new RateOfChange { Length = PerfDays + 1 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, roc, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, roc);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal rocValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var gapOpen = _prevClose != 0 && (candle.OpenPrice / _prevClose - 1m) >= GapThreshold / 100m;
		var perfPos = _prevRoc > 0;

		// TODO: implement EPS surprise detection and days since earnings
		var posSurprise = true;
		var daysSinceEarnings = 0;

		var entryCond = posSurprise && perfPos && gapOpen && daysSinceEarnings <= 1 && Position == 0;

		if (entryCond)
		{
			BuyMarket(Volume);
			_barsInTrade = 0;
			_stopLevel = 0;
		}

		if (Position > 0)
		{
			_barsInTrade++;

			if (_stopLevel == 0)
				_stopLevel = PositionAvgPrice * (1m - StopPct / 100m);

			var tradeProfit = candle.ClosePrice - PositionAvgPrice;
			var riskAmount = PositionAvgPrice * (StopPct / 100m);
			if (tradeProfit >= 2m * riskAmount && _stopLevel < PositionAvgPrice)
				_stopLevel = PositionAvgPrice;

			if (candle.ClosePrice > _stopLevel)
				SellStop(_stopLevel);

			if (_prevEma > 0 && _prevClose > _prevEma && candle.ClosePrice < emaValue)
				SellMarket(Position);
			else if (_barsInTrade >= MaxHoldBars)
				SellMarket(Position);
		}
		else
		{
			_barsInTrade = 0;
		}

		_prevClose = candle.ClosePrice;
		_prevEma = emaValue;
		_prevRoc = rocValue;
	}
}
