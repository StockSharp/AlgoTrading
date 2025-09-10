using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining ADX, CCI and moving average trend filter.
/// Enters when +DI crosses -DI and CCI confirms extreme values.
/// Optional MA risk management closes position after consecutive closes against MA.
/// </summary>
public class AdxCciMaStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<bool> _useMaTrend;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<bool> _useMaRisk;
	private readonly StrategyParam<int> _maRiskExitCandles;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci;
	private AverageDirectionalIndex _adx;
	private IIndicator _ma;

	private decimal _prevPlusDi;
	private decimal _prevMinusDi;
	private int _longAgainstCount;
	private int _shortAgainstCount;

	public bool EnableLong { get => _enableLong.Value; set => _enableLong.Value = value; }
	public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public bool UseMaTrend { get => _useMaTrend.Value; set => _useMaTrend.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public MovingAverageTypeEnum MaType { get => _maType.Value; set => _maType.Value = value; }
	public bool UseMaRiskManagement { get => _useMaRisk.Value; set => _useMaRisk.Value = value; }
	public int MaRiskExitCandles { get => _maRiskExitCandles.Value; set => _maRiskExitCandles.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AdxCciMaStrategy()
	{
		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long trades", "General");
		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short trades", "General");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
			.SetNotNegative();
		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
			.SetNotNegative();

		_cciPeriod = Param(nameof(CciPeriod), 15)
			.SetDisplay("CCI Period", "Period for Commodity Channel Index", "Indicators")
			.SetCanOptimize(true);
		_adxLength = Param(nameof(AdxLength), 10)
			.SetDisplay("ADX Length", "Length for Average Directional Index", "Indicators")
			.SetCanOptimize(true);
		_adxThreshold = Param(nameof(AdxThreshold), 20m)
			.SetDisplay("ADX Threshold", "ADX level to confirm trend", "Indicators")
			.SetCanOptimize(true);

		_useMaTrend = Param(nameof(UseMaTrend), true)
			.SetDisplay("Use MA Trend", "Enable moving average trend filter", "MA Trend");
		_maLength = Param(nameof(MaLength), 200)
			.SetDisplay("MA Length", "Length of moving average", "MA Trend")
			.SetCanOptimize(true);
		_maType = Param(nameof(MaType), MovingAverageTypeEnum.Simple)
			.SetDisplay("MA Type", "Type of moving average", "MA Trend");
		_useMaRisk = Param(nameof(UseMaRiskManagement), false)
			.SetDisplay("Use MA Risk Management", "Exit after candles against MA", "Risk Management");
		_maRiskExitCandles = Param(nameof(MaRiskExitCandles), 2)
			.SetDisplay("MA Risk Exit Candles", "Number of closes against MA to exit", "Risk Management");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_cci?.Reset();
		_adx?.Reset();
		_ma?.Reset();

		_prevPlusDi = 0;
		_prevMinusDi = 0;
		_longAgainstCount = 0;
		_shortAgainstCount = 0;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cci = new() { Length = CciPeriod };
		_adx = new() { Length = AdxLength };
		_ma = MaType switch
		{
			MovingAverageTypeEnum.Hull => new HullMovingAverage(),
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage(),
			MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage(),
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage(),
			MovingAverageTypeEnum.VolumeWeighted => new VolumeWeightedMovingAverage(),
			_ => new SimpleMovingAverage()
		};
		_ma.Length = MaLength;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, _cci, _ma, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);

			var adxArea = CreateChartArea();
			if (adxArea != null)
			{
				DrawIndicator(adxArea, _adx);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue cciValue, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		var plusDi = adxTyped.Dx.Plus;
		var minusDi = adxTyped.Dx.Minus;
		var adx = adxTyped.MovingAverage;

		var cci = cciValue.ToDecimal();
		var ma = maValue.ToDecimal();

		var longSignal = plusDi > minusDi && _prevPlusDi <= _prevMinusDi;
		var shortSignal = minusDi > plusDi && _prevMinusDi <= _prevPlusDi;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevPlusDi = plusDi;
			_prevMinusDi = minusDi;
			return;
		}

		if (EnableLong && longSignal && cci > 100m && adx >= AdxThreshold && (!UseMaTrend || candle.ClosePrice > ma) && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (EnableShort && shortSignal && cci < -100m && adx >= AdxThreshold && (!UseMaTrend || candle.ClosePrice < ma) && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		if (UseMaRiskManagement)
		{
			if (Position > 0)
			{
				if (candle.ClosePrice < ma)
				{
					_longAgainstCount += 1;
					if (_longAgainstCount >= MaRiskExitCandles)
					{
						SellMarket(Math.Abs(Position));
						_longAgainstCount = 0;
					}
				}
				else
				{
					_longAgainstCount = 0;
				}
			}
			else if (Position < 0)
			{
				if (candle.ClosePrice > ma)
				{
					_shortAgainstCount += 1;
					if (_shortAgainstCount >= MaRiskExitCandles)
					{
						BuyMarket(Math.Abs(Position));
						_shortAgainstCount = 0;
					}
				}
				else
				{
					_shortAgainstCount = 0;
				}
			}
			else
			{
				_longAgainstCount = 0;
				_shortAgainstCount = 0;
			}
		}

		_prevPlusDi = plusDi;
		_prevMinusDi = minusDi;
	}

	public enum MovingAverageTypeEnum
	{
		Simple,
		Hull,
		Exponential,
		Smoothed,
		Weighted,
		VolumeWeighted
	}
}
