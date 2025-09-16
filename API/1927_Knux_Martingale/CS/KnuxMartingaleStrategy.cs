using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<DataType> _candleType;

	private decimal _currentVolume;

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

		_takeProfit = Param(nameof(TakeProfit), 50m)
			.SetDisplay("Take Profit", "Absolute take profit in price units", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_currentVolume = Volume;

		var adx = new AverageDirectionalIndex { Length = AdxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var adxTyped = (AverageDirectionalIndexValue)adxValue;

		if (adxTyped.MovingAverage is not decimal adxMa)
			return;

		// Trade only in trending markets
		if (adxMa < 25)
			return;

		var volume = _currentVolume + Math.Abs(Position);

		if (candle.ClosePrice > candle.OpenPrice && Position <= 0)
		{
			BuyMarket(volume);
		}
		else if (candle.ClosePrice < candle.OpenPrice && Position >= 0)
		{
			SellMarket(volume);
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade myTrade)
	{
		base.OnNewMyTrade(myTrade);

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
	}
}

