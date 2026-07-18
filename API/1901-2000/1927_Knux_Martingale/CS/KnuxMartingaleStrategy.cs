using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Martingale strategy increasing volume after losses and filtered by ADX.
/// </summary>
public class KnuxMartingaleStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _lotsMultiplier;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trendThreshold;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _currentVolume;
	private decimal _prevSma;
	private bool _hasPrevSma;
	private int _barsSinceExit;

	/// <summary>
	/// ADX period used for trend strength.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Volume multiplier applied after a losing trade.
	/// </summary>
	public decimal LotsMultiplier
	{
		get => _lotsMultiplier.Value;
		set => _lotsMultiplier.Value = value;
	}

	/// <summary>
	/// Stop loss in absolute price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in absolute price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Minimum normalized distance between price and trend average.
	/// </summary>
	public decimal TrendThreshold
	{
		get => _trendThreshold.Value;
		set => _trendThreshold.Value = value;
	}

	/// <summary>
	/// Cooldown after a flat position before a new entry.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public KnuxMartingaleStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Period for ADX filter", "Indicators");

		_lotsMultiplier = Param(nameof(LotsMultiplier), 1.5m)
			.SetDisplay("Lots Multiplier", "Multiplier for losing trades", "Risk");

		_stopLoss = Param(nameof(StopLoss), 150m)
			.SetDisplay("Stop Loss", "Absolute stop loss in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 300m)
			.SetDisplay("Take Profit", "Absolute take profit in price units", "Risk");

		_trendThreshold = Param(nameof(TrendThreshold), 0.008m)
			.SetDisplay("Trend Threshold", "Minimum distance from trend average", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 3)
			.SetDisplay("Cooldown Bars", "Bars to wait after a completed position", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for strategy", "General");
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
		_currentVolume = Volume;
		_prevSma = 0m;
		_hasPrevSma = false;
		_barsSinceExit = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_currentVolume = Volume;

		var sma = new SimpleMovingAverage { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_barsSinceExit < CooldownBars)
			_barsSinceExit++;

		if (!_hasPrevSma)
		{
			_prevSma = smaValue;
			_hasPrevSma = true;
			return;
		}

		var distance = smaValue == 0m ? 0m : Math.Abs(candle.ClosePrice - smaValue) / smaValue;
		var isTrendUp = candle.ClosePrice > smaValue && smaValue > _prevSma;
		var isTrendDown = candle.ClosePrice < smaValue && smaValue < _prevSma;

		if (distance < TrendThreshold)
		{
			_prevSma = smaValue;
			return;
		}

		if (_barsSinceExit < CooldownBars && Position == 0)
		{
			_prevSma = smaValue;
			return;
		}

		var volume = Math.Max(Volume, _currentVolume);

		if (isTrendUp && candle.ClosePrice > candle.OpenPrice && Position <= 0)
		{
			BuyMarket(volume);
		}
		else if (isTrendDown && candle.ClosePrice < candle.OpenPrice && Position >= 0)
		{
			SellMarket(volume);
		}

		_prevSma = smaValue;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade myTrade)
	{
		base.OnOwnTradeReceived(myTrade);

		if (Position != 0)
			return;

		if (myTrade.PnL < 0)
		{
			_currentVolume *= LotsMultiplier;
		}
		else
		{
			_currentVolume = Volume;
		}

		_barsSinceExit = 0;
	}
}
