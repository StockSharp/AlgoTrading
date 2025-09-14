using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatile Action strategy.
/// Combines volatility breakout with Alligator trend filter on 4H timeframe.
/// Stop-loss and take-profit are based on ATR of the entry bar.
/// </summary>
public class VolatileActionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volatilityCoef;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopCoef;
	private readonly StrategyParam<decimal> _profitCoef;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr1;
	private AverageTrueRange _atrBase;
	private Highest _highest;
	private Lowest _lowest;
	private SmoothedMovingAverage _jaw;
	private SmoothedMovingAverage _teeth;
	private SmoothedMovingAverage _lips;

	private int _gatorSignal;
	private decimal _entryPrice;
	private decimal _tpPrice;
	private decimal _slPrice;

	/// <summary>
	/// Volatility coefficient.
	/// </summary>
	public decimal VolatilityCoef { get => _volatilityCoef.Value; set => _volatilityCoef.Value = value; }

	/// <summary>
	/// ATR base period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Stop-loss coefficient.
	/// </summary>
	public decimal StopCoef { get => _stopCoef.Value; set => _stopCoef.Value = value; }

	/// <summary>
	/// Take-profit coefficient.
	/// </summary>
	public decimal ProfitCoef { get => _profitCoef.Value; set => _profitCoef.Value = value; }

	/// <summary>
	/// Candle type for main analysis.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public VolatileActionStrategy()
	{
		_volatilityCoef = Param(nameof(VolatilityCoef), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Volatility Coef", "ATR1 multiplier against base ATR", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_atrPeriod = Param(nameof(AtrPeriod), 23)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Base ATR period", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 1);

		_stopCoef = Param(nameof(StopCoef), 0.6m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Coef", "ATR1 multiplier for stop-loss", "Risk");

		_profitCoef = Param(nameof(ProfitCoef), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Coef", "ATR1 multiplier for take-profit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for main calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromHours(4).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_gatorSignal = 0;
		_entryPrice = 0m;
		_tpPrice = 0m;
		_slPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr1 = new AverageTrueRange { Length = 1 };
		_atrBase = new AverageTrueRange { Length = AtrPeriod };
		_highest = new Highest { Length = 24 };
		_lowest = new Lowest { Length = 20 };

		_jaw = new SmoothedMovingAverage { Length = 13 };
		_teeth = new SmoothedMovingAverage { Length = 8 };
		_lips = new SmoothedMovingAverage { Length = 5 };

		var mainSub = SubscribeCandles(CandleType);
		mainSub.Bind(ProcessMainCandle).Start();

		var gatorSub = SubscribeCandles(TimeSpan.FromHours(4).TimeFrame());
		gatorSub.Bind(ProcessGatorCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawIndicator(area, _jaw);
			DrawIndicator(area, _teeth);
			DrawIndicator(area, _lips);
			DrawOwnTrades(area);
		}
	}

	private void ProcessGatorCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var jawVal = _jaw.Process(median);
		var teethVal = _teeth.Process(median);
		var lipsVal = _lips.Process(median);

		if (!jawVal.IsFinal || !teethVal.IsFinal || !lipsVal.IsFinal)
			return;

		var jaw = jawVal.ToDecimal();
		var teeth = teethVal.ToDecimal();
		var lips = lipsVal.ToDecimal();

		if (lips > teeth && lips > jaw && teeth > jaw && candle.ClosePrice > teeth && candle.OpenPrice > teeth)
			_gatorSignal = 1;
		else if (lips < teeth && lips < jaw && teeth < jaw && candle.ClosePrice < teeth && candle.OpenPrice < teeth)
			_gatorSignal = -1;
		else
			_gatorSignal = 0;
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atr1Val = _atr1.Process(candle);
		var atrBaseVal = _atrBase.Process(candle);
		var highestVal = _highest.Process(candle.HighPrice, candle.OpenTime, true);
		var lowestVal = _lowest.Process(candle.LowPrice, candle.OpenTime, true);

		if (!atr1Val.IsFinal || !atrBaseVal.IsFinal || !highestVal.IsFinal || !lowestVal.IsFinal)
			return;

		var atr1 = atr1Val.ToDecimal();
		var atrBase = atrBaseVal.ToDecimal();
		var max = highestVal.ToDecimal();
		var min = lowestVal.ToDecimal();

		var hl = candle.HighPrice - candle.LowPrice;
		var hc = Math.Abs(candle.HighPrice - candle.ClosePrice);
		var lc = Math.Abs(candle.LowPrice - candle.ClosePrice);

		var volSignal = 0;
		if (atr1 > VolatilityCoef * atrBase && candle.ClosePrice > candle.OpenPrice && max == candle.HighPrice && 0.3m * hl >= hc)
			volSignal = 1;
		else if (atr1 > VolatilityCoef * atrBase && candle.ClosePrice < candle.OpenPrice && min == candle.LowPrice && 0.3m * hl > lc)
			volSignal = -1;

		if (Position == 0)
		{
			if (volSignal > 0 && _gatorSignal > 0)
			{
				_entryPrice = candle.ClosePrice;
				_tpPrice = _entryPrice + ProfitCoef * atr1;
				_slPrice = _entryPrice - StopCoef * atr1;
				BuyMarket(Volume);
			}
			else if (volSignal < 0 && _gatorSignal < 0)
			{
				_entryPrice = candle.ClosePrice;
				_tpPrice = _entryPrice - ProfitCoef * atr1;
				_slPrice = _entryPrice + StopCoef * atr1;
				SellMarket(Volume);
			}
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _slPrice || candle.HighPrice >= _tpPrice)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _slPrice || candle.LowPrice <= _tpPrice)
				BuyMarket(Math.Abs(Position));
		}
	}
}
