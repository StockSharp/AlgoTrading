using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified version of TrendMaster Pro 2.3 strategy based on moving average cross with RSI and optional trend filter.
/// </summary>
public class TrendMasterPro23WithAlertsStrategy : Strategy
{
	public enum MaType
	{
		Ema,
		Sma
	}

	private readonly StrategyParam<MaType> _maType;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<bool> _enableTrendFilter;
	private readonly StrategyParam<MaType> _trendMaType;
	private readonly StrategyParam<int> _trendMaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiLong;
	private readonly StrategyParam<decimal> _rsiShort;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevShort;
	private decimal _prevLong;

	/// <summary>
	/// Type of moving average for signals.
	/// </summary>
	public MaType MovingAverageType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Length of short moving average.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Length of long moving average.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Enable higher timeframe trend filter.
	/// </summary>
	public bool EnableTrendFilter
	{
		get => _enableTrendFilter.Value;
		set => _enableTrendFilter.Value = value;
	}

	/// <summary>
	/// Moving average type for trend filter.
	/// </summary>
	public MaType TrendMaType
	{
		get => _trendMaType.Value;
		set => _trendMaType.Value = value;
	}

	/// <summary>
	/// Length of trend moving average.
	/// </summary>
	public int TrendMaLength
	{
		get => _trendMaLength.Value;
		set => _trendMaLength.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI threshold for long entries.
	/// </summary>
	public decimal RsiLongThreshold
	{
		get => _rsiLong.Value;
		set => _rsiLong.Value = value;
	}

	/// <summary>
	/// RSI threshold for short entries.
	/// </summary>
	public decimal RsiShortThreshold
	{
		get => _rsiShort.Value;
		set => _rsiShort.Value = value;
	}

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Initializes a new instance of the <see cref="TrendMasterPro23WithAlertsStrategy"/> class.
	/// </summary>
	public TrendMasterPro23WithAlertsStrategy()
	{
		_maType = Param(nameof(MovingAverageType), MaType.Ema).SetDisplay("MA Type", "Moving average type", "Indicators");
		_shortLength = Param(nameof(ShortLength), 9).SetGreaterThanZero().SetDisplay("Short MA", "Short MA length", "Indicators");
		_longLength = Param(nameof(LongLength), 21).SetGreaterThanZero().SetDisplay("Long MA", "Long MA length", "Indicators");
		_enableTrendFilter = Param(nameof(EnableTrendFilter), false).SetDisplay("Enable Trend Filter", "Use higher timeframe trend", "Filter");
		_trendMaType = Param(nameof(TrendMaType), MaType.Ema).SetDisplay("Trend MA Type", "Trend filter MA", "Filter");
		_trendMaLength = Param(nameof(TrendMaLength), 50).SetGreaterThanZero().SetDisplay("Trend MA Length", "Trend filter length", "Filter");
		_rsiLength = Param(nameof(RsiLength), 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "Indicators");
		_rsiLong = Param(nameof(RsiLongThreshold), 55m).SetDisplay("RSI Long", "Long threshold", "Indicators");
		_rsiShort = Param(nameof(RsiShortThreshold), 45m).SetDisplay("RSI Short", "Short threshold", "Indicators");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m).SetGreaterThanZero().SetDisplay("Take Profit %", "TP percent", "Risk");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetGreaterThanZero().SetDisplay("Stop Loss %", "SL percent", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	private IIndicator CreateMa(MaType type, int length)
	{
		return type == MaType.Sma ? new SimpleMovingAverage { Length = length } : new ExponentialMovingAverage { Length = length };
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		_prevShort = _prevLong = 0m;
		var shortMa = CreateMa(MovingAverageType, ShortLength);
		var longMa = CreateMa(MovingAverageType, LongLength);
		var trendMa = CreateMa(TrendMaType, TrendMaLength);
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true);
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(shortMa, longMa, trendMa, rsi, ProcessCandle).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortMa);
			DrawIndicator(area, longMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortMa, decimal longMa, decimal trendMa, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
		return;
		bool trendLong = !EnableTrendFilter || candle.ClosePrice > trendMa;
		bool trendShort = !EnableTrendFilter || candle.ClosePrice < trendMa;
		bool crossUp = _prevShort <= _prevLong && shortMa > longMa;
		bool crossDown = _prevShort >= _prevLong && shortMa < longMa;
		bool longCond = crossUp && rsi > RsiLongThreshold && trendLong;
		bool shortCond = crossDown && rsi < RsiShortThreshold && trendShort;
		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevShort = shortMa;
			_prevLong = longMa;
			return;
		}
		var volume = Volume + Math.Abs(Position);
		if (longCond && Position <= 0)
		{
			BuyMarket(volume);
		}
		else if (shortCond && Position >= 0)
		{
			SellMarket(volume);
		}
		else if (crossDown && Position > 0)
		{
			SellMarket(Position);
		}
		else if (crossUp && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}
		_prevShort = shortMa;
		_prevLong = longMa;
	}
}
