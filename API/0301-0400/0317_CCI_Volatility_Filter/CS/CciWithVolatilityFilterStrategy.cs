namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on CCI with an ATR-based volatility filter.
/// </summary>
public class CciWithVolatilityFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _cciOversold;
	private readonly StrategyParam<decimal> _cciOverbought;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _atrSma;
	private int _cooldownRemaining;

	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal CciOversold { get => _cciOversold.Value; set => _cciOversold.Value = value; }
	public decimal CciOverbought { get => _cciOverbought.Value; set => _cciOverbought.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CciWithVolatilityFilterStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for CCI calculation", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators");

		_cciOversold = Param(nameof(CciOversold), -100m)
			.SetDisplay("CCI Oversold", "CCI oversold level", "Indicators");

		_cciOverbought = Param(nameof(CciOverbought), 100m)
			.SetDisplay("CCI Overbought", "CCI overbought level", "Indicators");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 24)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between entries", "Trading");

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
		_cci = null;
		_atr = null;
		_atrSma = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_atrSma = new SMA { Length = AtrPeriod };
		_cooldownRemaining = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var cciValue = _cci.Process(candle);
		var atrValue = _atr.Process(candle);
		if (!cciValue.IsFormed || !atrValue.IsFormed)
			return;

		var atrAverage = _atrSma.Process(new DecimalIndicatorValue(_atrSma, atrValue.ToDecimal(), candle.ServerTime) { IsFinal = true });
		if (!atrAverage.IsFormed)
			return;

		var cci = cciValue.ToDecimal();
		var atr = atrValue.ToDecimal();
		var averageAtr = atrAverage.ToDecimal();
		var isTradableVolatility = averageAtr <= 0m || atr <= averageAtr * 10m;

		if (_cooldownRemaining > 0 || !isTradableVolatility)
			return;

		if (Position == 0 && cci <= CciOversold)
		{
			BuyMarket();
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (Position == 0 && cci >= CciOverbought)
		{
			SellMarket();
			_cooldownRemaining = SignalCooldownBars;
		}
	}
}
