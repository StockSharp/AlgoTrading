using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Forex Sky" MetaTrader strategy that trades MACD swings with daily trade limits.
/// </summary>
public class ForexSkyStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergence _macd;
	private decimal? _macdPrev1;
	private decimal? _macdPrev2;
	private decimal? _macdPrev3;
	private decimal? _macdPrev4;
	private DateTime? _lastTradeDay;
	private DateTimeOffset? _lastTradeBarTime;
	private decimal _pointValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="ForexSkyStrategy"/> class.
	/// </summary>
	public ForexSkyStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast Period", "Fast EMA length used by the MACD indicator.", "MACD");

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow Period", "Slow EMA length used by the MACD indicator.", "MACD");

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal Period", "Signal EMA length used by the MACD indicator.", "MACD");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Distance to the take-profit target expressed in instrument points.", "Risk management");

		_stopLossPoints = Param(nameof(StopLossPoints), 3000)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Distance to the stop-loss order expressed in instrument points.", "Risk management");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Base market order volume measured in lots.", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for MACD calculations.", "General");
	}

	/// <summary>
	/// Fast EMA period used by the MACD indicator.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period used by the MACD indicator.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period used by the MACD indicator.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Take-profit distance measured in instrument points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance measured in instrument points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Base market order size.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type used to feed the MACD indicator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_macd = null;
		_macdPrev1 = null;
		_macdPrev2 = null;
		_macdPrev3 = null;
		_macdPrev4 = null;
		_lastTradeDay = null;
		_lastTradeBarTime = null;
		_pointValue = 0m;

		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		_pointValue = CalculatePointValue();

		Unit takeProfitUnit = null;
		if (TakeProfitPoints > 0 && _pointValue > 0m)
			takeProfitUnit = new Unit(TakeProfitPoints * _pointValue, UnitTypes.Absolute);

		Unit stopLossUnit = null;
		if (StopLossPoints > 0 && _pointValue > 0m)
			stopLossUnit = new Unit(StopLossPoints * _pointValue, UnitTypes.Absolute);

		if (takeProfitUnit != null || stopLossUnit != null)
		{
			StartProtection(
				takeProfit: takeProfitUnit,
				stopLoss: stopLossUnit,
				isStopTrailing: false,
				useMarketOrders: true);
		}

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = FastPeriod,
			LongPeriod = SlowPeriod,
			SignalPeriod = SignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdLine, decimal macdSignal, decimal macdHistogram)
	{
		_ = macdSignal;
		_ = macdHistogram;

		if (candle.State != CandleStates.Finished)
		{
			UpdateMacdHistory(macdLine);
			return;
		}

		if (_macd == null || !_macd.IsFormed)
		{
			UpdateMacdHistory(macdLine);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateMacdHistory(macdLine);
			return;
		}

		var prev1 = _macdPrev1;
		var prev2 = _macdPrev2;
		var prev3 = _macdPrev3;
		var prev4 = _macdPrev4;

		if (prev1 == null || prev2 == null || prev3 == null || prev4 == null)
		{
			UpdateMacdHistory(macdLine);
			return;
		}

		var tradeDate = candle.OpenTime.Date;

		if (_lastTradeDay.HasValue && _lastTradeDay.Value == tradeDate)
		{
			UpdateMacdHistory(macdLine);
			return;
		}

		if (_lastTradeBarTime.HasValue && _lastTradeBarTime.Value == candle.OpenTime)
		{
			UpdateMacdHistory(macdLine);
			return;
		}

		var bullishSignal = macdLine > 0m
			&& macdLine > 0.00009m
			&& (prev1 <= 0m || prev2 <= 0m || prev3 <= 0m);

		var bearishSignal = (macdLine < 0m
			&& macdLine < -0.0004m
			&& (prev1 >= 0m || prev2 >= 0m || prev3 >= 0m)
			&& prev4 >= 0.001m)
			|| prev4 >= 0.003m;

		if (Volume <= 0m)
		{
			UpdateMacdHistory(macdLine);
			return;
		}

		if (bullishSignal && Position == 0)
		{
			// Enter long once per day when the MACD flips from negative to positive with momentum confirmation.
			BuyMarket(Volume);
			_lastTradeBarTime = candle.OpenTime;
			_lastTradeDay = tradeDate;
		}
		else if (bearishSignal && Position == 0)
		{
			// Enter short once per day when the MACD turns negative after a positive stretch.
			SellMarket(Volume);
			_lastTradeBarTime = candle.OpenTime;
			_lastTradeDay = tradeDate;
		}

		UpdateMacdHistory(macdLine);
	}

	private void UpdateMacdHistory(decimal macdValue)
	{
		// Shift the stored MACD history to maintain the last four completed values.
		_macdPrev4 = _macdPrev3;
		_macdPrev3 = _macdPrev2;
		_macdPrev2 = _macdPrev1;
		_macdPrev1 = macdValue;
	}

	private decimal CalculatePointValue()
	{
		if (Security == null)
			return 0m;

		var step = Security.PriceStep ?? Security.Step ?? 0m;
		return step > 0m ? step : 0m;
	}
}
