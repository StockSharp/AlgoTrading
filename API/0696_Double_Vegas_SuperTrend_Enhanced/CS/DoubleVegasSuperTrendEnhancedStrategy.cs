using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double Vegas SuperTrend Enhanced strategy.
/// Combines two SuperTrend calculations adjusted by Vegas channel volatility.
/// </summary>
public class DoubleVegasSuperTrendEnhancedStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod1;
	private readonly StrategyParam<int> _vegasWindow1;
	private readonly StrategyParam<decimal> _multiplier1;
	private readonly StrategyParam<decimal> _volatilityAdjustment1;
	private readonly StrategyParam<int> _atrPeriod2;
	private readonly StrategyParam<int> _vegasWindow2;
	private readonly StrategyParam<decimal> _multiplier2;
	private readonly StrategyParam<decimal> _volatilityAdjustment2;
	private readonly StrategyParam<string> _tradeDirection;
	private readonly StrategyParam<bool> _useHoldDays;
	private readonly StrategyParam<int> _holdDays;
	private readonly StrategyParam<string> _tpslCondition;
	private readonly StrategyParam<decimal> _takeProfitPerc;
	private readonly StrategyParam<decimal> _stopLossPerc;

	private decimal _prevUpper1;
	private decimal _prevLower1;
	private int _trend1 = 1;
	private bool _hasTrend1;

	private decimal _prevUpper2;
	private decimal _prevLower2;
	private int _trend2 = 1;
	private bool _hasTrend2;

	private DateTimeOffset? _longEntryTime;
	private DateTimeOffset? _shortEntryTime;

	public DoubleVegasSuperTrendEnhancedStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles", "General");

		_atrPeriod1 =
			Param(nameof(AtrPeriod1), 10).SetDisplay("ATR Period 1", "ATR length for first SuperTrend", "SuperTrend 1");

		_vegasWindow1 =
			Param(nameof(VegasWindow1), 100).SetDisplay("Vegas Window 1", "Vegas channel SMA period", "SuperTrend 1");

		_multiplier1 = Param(nameof(Multiplier1), 5m)
						   .SetDisplay("Multiplier 1", "Base multiplier for first SuperTrend", "SuperTrend 1");

		_volatilityAdjustment1 =
			Param(nameof(VolatilityAdjustment1), 5m)
				.SetDisplay("Volatility Adjustment 1", "Volatility factor for first SuperTrend", "SuperTrend 1");

		_atrPeriod2 =
			Param(nameof(AtrPeriod2), 5).SetDisplay("ATR Period 2", "ATR length for second SuperTrend", "SuperTrend 2");

		_vegasWindow2 =
			Param(nameof(VegasWindow2), 200).SetDisplay("Vegas Window 2", "Vegas channel SMA period", "SuperTrend 2");

		_multiplier2 = Param(nameof(Multiplier2), 7m)
						   .SetDisplay("Multiplier 2", "Base multiplier for second SuperTrend", "SuperTrend 2");

		_volatilityAdjustment2 =
			Param(nameof(VolatilityAdjustment2), 7m)
				.SetDisplay("Volatility Adjustment 2", "Volatility factor for second SuperTrend", "SuperTrend 2");

		_tradeDirection =
			Param(nameof(TradeDirection), "Both").SetDisplay("Trade Direction", "Allowed trading direction", "General");

		_useHoldDays = Param(nameof(UseHoldDays), true).SetDisplay("Use Hold Days", "Enable holding period", "Exits");

		_holdDays = Param(nameof(HoldDays), 5).SetDisplay("Hold Days", "Number of days to hold position", "Exits");

		_tpslCondition = Param(nameof(TpslCondition), "None")
							 .SetDisplay("TPSL Condition", "Which protection to enable", "Protection");

		_takeProfitPerc =
			Param(nameof(TakeProfitPerc), 30m).SetDisplay("Take Profit (%)", "Take profit percentage", "Protection");

		_stopLossPerc =
			Param(nameof(StopLossPerc), 20m).SetDisplay("Stop Loss (%)", "Stop loss percentage", "Protection");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod1
	{
		get => _atrPeriod1.Value;
		set => _atrPeriod1.Value = value;
	}

	public int VegasWindow1
	{
		get => _vegasWindow1.Value;
		set => _vegasWindow1.Value = value;
	}

	public decimal Multiplier1
	{
		get => _multiplier1.Value;
		set => _multiplier1.Value = value;
	}

	public decimal VolatilityAdjustment1
	{
		get => _volatilityAdjustment1.Value;
		set => _volatilityAdjustment1.Value = value;
	}

	public int AtrPeriod2
	{
		get => _atrPeriod2.Value;
		set => _atrPeriod2.Value = value;
	}

	public int VegasWindow2
	{
		get => _vegasWindow2.Value;
		set => _vegasWindow2.Value = value;
	}

	public decimal Multiplier2
	{
		get => _multiplier2.Value;
		set => _multiplier2.Value = value;
	}

	public decimal VolatilityAdjustment2
	{
		get => _volatilityAdjustment2.Value;
		set => _volatilityAdjustment2.Value = value;
	}

	public string TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	public bool UseHoldDays
	{
		get => _useHoldDays.Value;
		set => _useHoldDays.Value = value;
	}

	public int HoldDays
	{
		get => _holdDays.Value;
		set => _holdDays.Value = value;
	}

	public string TpslCondition
	{
		get => _tpslCondition.Value;
		set => _tpslCondition.Value = value;
	}

	public decimal TakeProfitPerc
	{
		get => _takeProfitPerc.Value;
		set => _takeProfitPerc.Value = value;
	}

	public decimal StopLossPerc
	{
		get => _stopLossPerc.Value;
		set => _stopLossPerc.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevUpper1 = _prevLower1 = 0m;
		_prevUpper2 = _prevLower2 = 0m;
		_trend1 = _trend2 = 1;
		_hasTrend1 = _hasTrend2 = false;
		_longEntryTime = _shortEntryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var vegasSma1 = new SimpleMovingAverage { Length = VegasWindow1 };
		var vegasStd1 = new StandardDeviation { Length = VegasWindow1 };
		var atr1 = new AverageTrueRange { Length = AtrPeriod1 };

		var vegasSma2 = new SimpleMovingAverage { Length = VegasWindow2 };
		var vegasStd2 = new StandardDeviation { Length = VegasWindow2 };
		var atr2 = new AverageTrueRange { Length = AtrPeriod2 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(vegasSma1, vegasStd1, atr1, vegasSma2, vegasStd2, atr2, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		Unit? tp = null;
		Unit? sl = null;
		if (TpslCondition == "TP" || TpslCondition == "Both")
			tp = TakeProfitPerc.Percents();
		if (TpslCondition == "SL" || TpslCondition == "Both")
			sl = StopLossPerc.Percents();

		if (tp != null || sl != null)
			StartProtection(tp, sl);
	}

	private void ProcessCandle(ICandleMessage candle, decimal vegasMa1, decimal vegasStd1, decimal atr1,
							   decimal vegasMa2, decimal vegasStd2, decimal atr2)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (vegasMa1 == 0m || vegasMa2 == 0m)
			return;

		var hlc3 = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		var channelWidth1 = vegasStd1 * 2m;
		var adjusted1 = Multiplier1 + VolatilityAdjustment1 * (channelWidth1 / vegasMa1);
		var upper1 = hlc3 - adjusted1 * atr1;
		var lower1 = hlc3 + adjusted1 * atr1;

		if (!_hasTrend1)
		{
			_prevUpper1 = upper1;
			_prevLower1 = lower1;
			_hasTrend1 = true;
		}

		_trend1 = candle.ClosePrice > _prevLower1 ? 1 : candle.ClosePrice < _prevUpper1 ? -1 : _trend1;
		upper1 = _trend1 == 1 ? Math.Max(upper1, _prevUpper1) : upper1;
		lower1 = _trend1 == -1 ? Math.Min(lower1, _prevLower1) : lower1;
		_prevUpper1 = upper1;
		_prevLower1 = lower1;

		var channelWidth2 = vegasStd2 * 2m;
		var adjusted2 = Multiplier2 + VolatilityAdjustment2 * (channelWidth2 / vegasMa2);
		var upper2 = hlc3 - adjusted2 * atr2;
		var lower2 = hlc3 + adjusted2 * atr2;

		if (!_hasTrend2)
		{
			_prevUpper2 = upper2;
			_prevLower2 = lower2;
			_hasTrend2 = true;
		}

		_trend2 = candle.ClosePrice > _prevLower2 ? 1 : candle.ClosePrice < _prevUpper2 ? -1 : _trend2;
		upper2 = _trend2 == 1 ? Math.Max(upper2, _prevUpper2) : upper2;
		lower2 = _trend2 == -1 ? Math.Min(lower2, _prevLower2) : lower2;
		_prevUpper2 = upper2;
		_prevLower2 = lower2;

		var enterLong = _trend1 == 1 && _trend2 == 1;
		var enterShort = _trend1 == -1 && _trend2 == -1;
		var exitLong = _trend1 == -1 || _trend2 == -1;
		var exitShort = _trend1 == 1 || _trend2 == 1;

		var allowLong = TradeDirection == "Long" || TradeDirection == "Both";
		var allowShort = TradeDirection == "Short" || TradeDirection == "Both";
		var now = candle.OpenTime;

		if (UseHoldDays)
		{
			if (_longEntryTime != null && now >= _longEntryTime + TimeSpan.FromDays(HoldDays) && Position > 0)
			{
				SellMarket(Position);
				_longEntryTime = null;
			}

			if (_shortEntryTime != null && now >= _shortEntryTime + TimeSpan.FromDays(HoldDays) && Position < 0)
			{
				BuyMarket(Position.Abs());
				_shortEntryTime = null;
			}
		}
		else
		{
			if (exitLong && Position > 0)
			{
				SellMarket(Position);
				_longEntryTime = null;
			}

			if (exitShort && Position < 0)
			{
				BuyMarket(Position.Abs());
				_shortEntryTime = null;
			}
		}

		if (enterLong && allowLong && Position <= 0)
		{
			var volume = Volume + Position.Abs();
			BuyMarket(volume);
			_longEntryTime = now;
		}
		else if (enterShort && allowShort && Position >= 0)
		{
			var volume = Volume + Position.Abs();
			SellMarket(volume);
			_shortEntryTime = now;
		}
	}
}
