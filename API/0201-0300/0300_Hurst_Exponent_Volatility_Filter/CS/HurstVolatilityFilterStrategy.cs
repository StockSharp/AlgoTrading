using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hurst exponent mean reversion with a volatility filter.
/// Trades back toward the moving average only while the Hurst exponent is below 0.5 (anti-persistent prices)
/// and ATR is below its own average. Exits when price returns to the moving average or volatility expands.
/// </summary>
public class HurstVolatilityFilterStrategy : Strategy
{
	private const decimal _randomWalkHurst = 0.5m;

	private readonly StrategyParam<int> _hurstPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _atrAverage;
	private HurstExponent _hurst;
	private decimal? _prevClose;

	/// <summary>
	/// Period for the Hurst exponent.
	/// </summary>
	public int HurstPeriod
	{
		get => _hurstPeriod.Value;
		set => _hurstPeriod.Value = value;
	}

	/// <summary>
	/// Period for the moving average.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Period for ATR and its average.
	/// </summary>
	public int ATRPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
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
	/// Initialize <see cref="HurstVolatilityFilterStrategy"/>.
	/// </summary>
	public HurstVolatilityFilterStrategy()
	{
		_hurstPeriod = Param(nameof(HurstPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Hurst Period", "Period for the Hurst exponent", "Indicators");

		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for the moving average", "Indicators");

		_atrPeriod = Param(nameof(ATRPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR and its average", "Indicators");

		_stopLoss = Param(nameof(StopLoss), 2.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_atrAverage = null;
		_hurst = null;
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = MAPeriod };
		var atr = new AverageTrueRange { Length = ATRPeriod };
		_hurst = new HurstExponent { Length = HurstPeriod };
		_atrAverage = new SimpleMovingAverage { Length = ATRPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, atr, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: null,
			stopLoss: StopLoss > 0 ? new Unit(StopLoss, UnitTypes.Percent) : null
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atrAverage = _atrAverage.Process(atrValue, candle.ServerTime, true).ToDecimal();

		var prevClose = _prevClose;
		_prevClose = candle.ClosePrice;

		if (prevClose is not decimal prev)
			return;

		// Hurst is measured on bar-to-bar price changes, where 0.5 separates trending from mean-reverting behaviour.
		var hurstResult = _hurst.Process(candle.ClosePrice - prev, candle.ServerTime, true);

		if (!_atrAverage.IsFormed || !_hurst.IsFormed || hurstResult.IsEmpty)
			return;

		var hurstValue = hurstResult.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var isQuiet = atrValue < atrAverage;
		var isMeanReverting = hurstValue < _randomWalkHurst;

		if (Position > 0 && (close >= smaValue || !isQuiet))
		{
			SellMarket(Position);
			return;
		}

		if (Position < 0 && (close <= smaValue || !isQuiet))
		{
			BuyMarket(-Position);
			return;
		}

		if (!isMeanReverting || !isQuiet)
			return;

		if (close < smaValue && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close > smaValue && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
