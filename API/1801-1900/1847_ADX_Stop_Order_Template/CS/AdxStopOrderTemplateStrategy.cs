using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on ADX and DMI crossovers.
/// </summary>
public class AdxStopOrderTemplateStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxSignal;
	private readonly StrategyParam<int> _pips;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<decimal> _maxSpread;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _minDiSpread;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal? _prevPlus;
	private decimal? _prevMinus;
	private int _cooldownRemaining;

	/// <summary>
	/// Initializes a new instance of the <see cref="AdxStopOrderTemplateStrategy"/> class.
	/// </summary>
	public AdxStopOrderTemplateStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Calculation period for ADX and DMI.", "Indicators");

		_adxSignal = Param(nameof(AdxSignal), 20m)
			.SetGreaterThanZero()
			.SetDisplay("ADX Threshold", "Minimum ADX value to allow entries.", "Indicators");

		_pips = Param(nameof(Pips), 10)
			.SetGreaterThanZero()
			.SetDisplay("Pending Offset", "Distance in price steps for stop orders.", "Orders");

		_takeProfit = Param(nameof(TakeProfit), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit size in price steps.", "Risk");

		_stopLoss = Param(nameof(StopLoss), 500)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss size in price steps.", "Risk");

		_maxSpread = Param(nameof(MaxSpread), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Max Spread", "Maximum allowed spread in price steps.", "Orders");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for analysis.", "General");

		_minDiSpread = Param(nameof(MinDiSpread), 5m)
			.SetDisplay("DI Spread", "Minimum spread between DI+ and DI-.", "Filters");

		_cooldownBars = Param(nameof(CooldownBars), 6)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change.", "Trading");
	}

	#region Parameters

	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	public decimal AdxSignal
	{
		get => _adxSignal.Value;
		set => _adxSignal.Value = value;
	}

	public int Pips
	{
		get => _pips.Value;
		set => _pips.Value = value;
	}

	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public decimal MaxSpread
	{
		get => _maxSpread.Value;
		set => _maxSpread.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal MinDiSpread
	{
		get => _minDiSpread.Value;
		set => _minDiSpread.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	#endregion

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevPlus = null;
		_prevMinus = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var dmi = new DirectionalIndex { Length = AdxPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(dmi, adx, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawIndicator(area, dmi);
			DrawOwnTrades(area);
		}

		var step = Security.PriceStep ?? 1m;
		StartProtection(
			new Unit(TakeProfit * step, UnitTypes.Absolute),
			new Unit(StopLoss * step, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue dmiValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished || !dmiValue.IsFinal || !adxValue.IsFinal)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		var dmiTyped = (DirectionalIndexValue)dmiValue;
		if (dmiTyped.Plus is not decimal diPlus || dmiTyped.Minus is not decimal diMinus)
			return;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx)
			return;

		if (_prevPlus is not decimal prevPlus || _prevMinus is not decimal prevMinus)
		{
			_prevPlus = diPlus;
			_prevMinus = diMinus;
			return;
		}

		var crossUp = prevPlus <= prevMinus && diPlus > diMinus;
		var crossDown = prevPlus >= prevMinus && diPlus < diMinus;
		var diSpread = Math.Abs(diPlus - diMinus);

		if (_cooldownRemaining == 0 && adx >= AdxSignal && diSpread >= MinDiSpread)
		{
			if (crossUp && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();

				BuyMarket();
				_cooldownRemaining = CooldownBars;
			}
			else if (crossDown && Position >= 0)
			{
				if (Position > 0)
					SellMarket();

				SellMarket();
				_cooldownRemaining = CooldownBars;
			}
		}
		else if (Position > 0 && crossDown)
		{
			SellMarket();
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && crossUp)
		{
			BuyMarket();
			_cooldownRemaining = CooldownBars;
		}

		_prevPlus = diPlus;
		_prevMinus = diMinus;
	}
}
