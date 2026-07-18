namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy combining ADX, CCI and moving average trend filter.
/// Enters when +DI crosses -DI and CCI confirms extreme values.
/// </summary>
public class AdxCciMaStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevPlusDi;
	private decimal _prevMinusDi;
	private int _cooldownRemaining;

	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AdxCciMaStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for CCI", "Indicators");

		_adxLength = Param(nameof(AdxLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "Length for ADX", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 20m)
			.SetDisplay("ADX Threshold", "ADX level to confirm trend", "Indicators");

		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Length of moving average", "MA Trend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevPlusDi = 0;
		_prevMinusDi = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxLength };
		var ma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, cci, ma, ProcessCandle)
			.Start();

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adxTyped = (IAverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx)
			return;

		var dx = adxTyped.Dx;
		if (dx.Plus is not decimal plusDi || dx.Minus is not decimal minusDi)
			return;

		var cci = cciValue.ToDecimal();
		var ma = maValue.ToDecimal();

		if (_prevPlusDi == 0)
		{
			_prevPlusDi = plusDi;
			_prevMinusDi = minusDi;
			return;
		}

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_prevPlusDi = plusDi;
			_prevMinusDi = minusDi;
			return;
		}

		var longSignal = plusDi > minusDi && _prevPlusDi <= _prevMinusDi;
		var shortSignal = minusDi > plusDi && _prevMinusDi <= _prevPlusDi;

		if (longSignal && cci > 0m && adx >= AdxThreshold && candle.ClosePrice > ma && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (shortSignal && cci < 0m && adx >= AdxThreshold && candle.ClosePrice < ma && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}

		_prevPlusDi = plusDi;
		_prevMinusDi = minusDi;
	}
}
