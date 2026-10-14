using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ADX CCI MA strategy.
/// A long opens when +DI crosses above -DI, CCI is above 100 and ADX exceeds AdxThreshold (and the close is above the moving average
/// when UseMaTrend is set); a short mirrors this with -DI crossing above +DI and CCI below -100. Percent take profit and stop loss
/// protect positions, and the optional MA risk management exits after MaRiskExitCandles consecutive closes on the wrong side of the
/// moving average.
/// </summary>
public class AdxCciMaStrategy : Strategy
{
	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MovingAverageTypeEnum
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Weighted,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,
	}

	private const decimal _cciLevel = 100m;

	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<bool> _useMaTrend;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<bool> _useMaRiskManagement;
	private readonly StrategyParam<int> _maRiskExitCandles;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevPlusDi;
	private decimal? _prevMinusDi;
	private int _againstMaCount;

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
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
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
	}

	/// <summary>
	/// ADX level a trend must exceed.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Require the close on the trade side of the moving average.
	/// </summary>
	public bool UseMaTrend
	{
		get => _useMaTrend.Value;
		set => _useMaTrend.Value = value;
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Exit after several closes against the moving average.
	/// </summary>
	public bool UseMaRiskManagement
	{
		get => _useMaRiskManagement.Value;
		set => _useMaRiskManagement.Value = value;
	}

	/// <summary>
	/// Consecutive closes against the moving average that trigger the exit.
	/// </summary>
	public int MaRiskExitCandles
	{
		get => _maRiskExitCandles.Value;
		set => _maRiskExitCandles.Value = value;
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
	/// Constructor.
	/// </summary>
	public AdxCciMaStrategy()
	{
		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long trades", "General");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short trades", "General");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_cciPeriod = Param(nameof(CciPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI period", "Indicators");

		_adxLength = Param(nameof(AdxLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "ADX period", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
			.SetDisplay("ADX Threshold", "ADX level a trend must exceed", "Indicators");

		_useMaTrend = Param(nameof(UseMaTrend), true)
			.SetDisplay("Use MA Trend", "Require the close on the trade side of the moving average", "MA");

		_maType = Param(nameof(MaType), MovingAverageTypeEnum.Simple)
			.SetDisplay("MA Type", "Moving average type", "MA");

		_maLength = Param(nameof(MaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "MA");

		_useMaRiskManagement = Param(nameof(UseMaRiskManagement), false)
			.SetDisplay("Use MA Risk Management", "Exit after several closes against the moving average", "Risk");

		_maRiskExitCandles = Param(nameof(MaRiskExitCandles), 2)
			.SetGreaterThanZero()
			.SetDisplay("MA Risk Exit Candles", "Consecutive closes against the moving average that trigger the exit", "Risk");

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
		_prevPlusDi = null;
		_prevMinusDi = null;
		_againstMaCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevPlusDi = null;
		_prevMinusDi = null;
		_againstMaCount = 0;

		var adx = new AverageDirectionalIndex { Length = AdxLength };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		IIndicator ma = MaType switch
		{
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = MaLength },
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = MaLength },
			MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage { Length = MaLength },
			_ => new SimpleMovingAverage { Length = MaLength },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, cci, ma, ProcessCandle)
			.Start();

		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue cciValue, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!adxValue.IsFormed || !cciValue.IsFormed || !maValue.IsFormed)
			return;

		var adxTyped = (IAverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx || adxTyped.Dx.Plus is not decimal plusDi || adxTyped.Dx.Minus is not decimal minusDi)
			return;

		var prevPlus = _prevPlusDi;
		var prevMinus = _prevMinusDi;
		_prevPlusDi = plusDi;
		_prevMinusDi = minusDi;

		if (prevPlus is not decimal pPlus || prevMinus is not decimal pMinus)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var cci = cciValue.ToDecimal();
		var ma = maValue.ToDecimal();
		var close = candle.ClosePrice;

		if (Position > 0 && close < ma || Position < 0 && close > ma)
			_againstMaCount++;
		else
			_againstMaCount = 0;

		if (UseMaRiskManagement && Position != 0 && _againstMaCount >= MaRiskExitCandles)
		{
			if (Position > 0)
				SellMarket(Position);
			else
				BuyMarket(-Position);

			_againstMaCount = 0;
			return;
		}

		var longSignal = EnableLong && pPlus <= pMinus && plusDi > minusDi && cci > _cciLevel && adx > AdxThreshold
			&& (!UseMaTrend || close > ma);
		var shortSignal = EnableShort && pMinus <= pPlus && minusDi > plusDi && cci < -_cciLevel && adx > AdxThreshold
			&& (!UseMaTrend || close < ma);

		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_againstMaCount = 0;
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_againstMaCount = 0;
		}
	}
}
