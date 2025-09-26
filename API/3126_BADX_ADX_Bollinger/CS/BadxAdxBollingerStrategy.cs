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
/// BADX strategy that combines Average Directional Index filtering with Bollinger Bands reversals.
/// Opens long trades when price dips below the lower band while ADX signals a ranging market and shorts on the opposite condition.
/// Built with the high-level API so protective orders and trailing stops are managed automatically.
/// </summary>
public class BadxAdxBollingerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxLevel;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;

	private AverageDirectionalIndex _adx;
	private BollingerBands _bollinger;
	private decimal _pipSize;

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Averaging period for ADX.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX threshold that defines a ranging environment.
	/// </summary>
	public decimal AdxLevel
	{
		get => _adxLevel.Value;
		set => _adxLevel.Value = value;
	}

	/// <summary>
	/// Period for Bollinger Bands calculation.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for Bollinger Bands.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step distance expressed in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BadxAdxBollingerStrategy"/>.
	/// </summary>
	public BadxAdxBollingerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for signals", "General");

		_adxPeriod = Param(nameof(AdxPeriod), 30)
		.SetDisplay("ADX Period", "Averaging period for ADX", "Indicators")
		.SetGreaterThanZero();

		_adxLevel = Param(nameof(AdxLevel), 20m)
		.SetDisplay("ADX Level", "ADX threshold for ranging market", "Indicators")
		.SetGreaterThanZero();

		_bollingerPeriod = Param(nameof(BollingerPeriod), 10)
		.SetDisplay("Bands Period", "Averaging period for Bollinger Bands", "Indicators")
		.SetGreaterThanZero();

		_bollingerDeviation = Param(nameof(BollingerDeviation), 1.5m)
		.SetDisplay("Bands Deviation", "Standard deviations for Bollinger Bands", "Indicators")
		.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetDisplay("Trailing Step (pips)", "Minimum move before trailing", "Risk");

		Volume = 1m;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_adx?.Reset();
		_bollinger?.Reset();
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerDeviation };

		_pipSize = GetPipSize();
		if (_pipSize <= 0m)
			_pipSize = Security?.PriceStep ?? 0m;

		Unit takeProfitUnit = null;
		Unit stopLossUnit = null;
		Unit trailingStopUnit = null;
		Unit trailingStepUnit = null;

		if (_pipSize > 0m)
		{
			if (TakeProfitPips > 0m)
				takeProfitUnit = new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute);

			if (StopLossPips > 0m)
				stopLossUnit = new Unit(StopLossPips * _pipSize, UnitTypes.Absolute);

			if (TrailingStopPips > 0m)
			{
				trailingStopUnit = new Unit(TrailingStopPips * _pipSize, UnitTypes.Absolute);

				if (TrailingStepPips > 0m)
					trailingStepUnit = new Unit(TrailingStepPips * _pipSize, UnitTypes.Absolute);
			}
		}

		if (takeProfitUnit != null || stopLossUnit != null || trailingStopUnit != null)
		{
			StartProtection(
				takeProfit: takeProfitUnit,
				stopLoss: stopLossUnit,
				trailingStop: trailingStopUnit,
				trailingStep: trailingStepUnit,
				useMarketOrders: true);
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, _bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);

			if (_bollinger != null)
				DrawIndicator(area, _bollinger);

			if (_adx != null)
				DrawIndicator(area, _adx);

			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue bandsValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!adxValue.IsFinal || !bandsValue.IsFinal)
			return;

		if (adxValue is not AverageDirectionalIndexValue adxData)
			return;

		if (bandsValue is not BollingerBandsValue bollingerData)
			return;

		if (adxData.MovingAverage is not decimal adx)
			return;

		if (bollingerData.LowBand is not decimal lower || bollingerData.UpBand is not decimal upper)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m)
			return;

		var closePrice = candle.ClosePrice;
		var volume = Volume;
		if (volume <= 0m)
			volume = 1m;

		if (adx < AdxLevel && closePrice <= lower)
		{
			BuyMarket(volume);
		}
		else if (adx < AdxLevel && closePrice >= upper)
		{
			SellMarket(volume);
		}
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var temp = step;
		var decimals = 0;

		while (temp != decimal.Truncate(temp) && decimals < 10)
		{
			temp *= 10m;
			decimals++;
		}

		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}
}
