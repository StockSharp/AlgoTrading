using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Game theory trading strategy.
/// Herd behaviour is an RSI extreme (above 70 or below 30) on volume above HerdThreshold times its VolumeMaLength average. A liquidity
/// trap is a sweep beyond the LiquidityLookback high or low that closes back inside. Institutional flow is volume above
/// InstVolumeMultiplier times its average, and the smart money bias is the accumulation/distribution line against its InstMaLength
/// average. The Nash equilibrium is the NashPeriod SMA with bands one standard deviation away.
/// Longs come from herd selling into a bear trap or accumulation (contrarian), institutional buying with accumulation above the
/// InstMaLength SMA (momentum), or a close back above the lower Nash band (reversion); shorts mirror them. The size is increased by
/// half on institutional volume and halved when price is within NashDeviation of the equilibrium. Opposite signals reverse, and
/// optional percent stop loss and take profit close the position.
/// </summary>
public class GameTheoryTradingStrategy : Strategy
{
	private const decimal _rsiOverbought = 70m;
	private const decimal _rsiOversold = 30m;

	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<decimal> _herdThreshold;
	private readonly StrategyParam<int> _liquidityLookback;
	private readonly StrategyParam<decimal> _instVolumeMultiplier;
	private readonly StrategyParam<int> _instMaLength;
	private readonly StrategyParam<int> _nashPeriod;
	private readonly StrategyParam<decimal> _nashDeviation;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeMa;
	private SimpleMovingAverage _adMa;
	private decimal? _prevHighest;
	private decimal? _prevLowest;
	private decimal? _prevClose;
	private decimal? _prevUpperBand;
	private decimal? _prevLowerBand;

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Length of the volume average.
	/// </summary>
	public int VolumeMaLength
	{
		get => _volumeMaLength.Value;
		set => _volumeMaLength.Value = value;
	}

	/// <summary>
	/// Volume to average ratio that marks herd behaviour.
	/// </summary>
	public decimal HerdThreshold
	{
		get => _herdThreshold.Value;
		set => _herdThreshold.Value = value;
	}

	/// <summary>
	/// Candles whose high and low define liquidity pools.
	/// </summary>
	public int LiquidityLookback
	{
		get => _liquidityLookback.Value;
		set => _liquidityLookback.Value = value;
	}

	/// <summary>
	/// Volume to average ratio that marks institutional flow.
	/// </summary>
	public decimal InstVolumeMultiplier
	{
		get => _instVolumeMultiplier.Value;
		set => _instVolumeMultiplier.Value = value;
	}

	/// <summary>
	/// Length of the institutional trend and A/D averages.
	/// </summary>
	public int InstMaLength
	{
		get => _instMaLength.Value;
		set => _instMaLength.Value = value;
	}

	/// <summary>
	/// Period of the Nash equilibrium average and deviation.
	/// </summary>
	public int NashPeriod
	{
		get => _nashPeriod.Value;
		set => _nashPeriod.Value = value;
	}

	/// <summary>
	/// Relative distance from the equilibrium regarded as near it.
	/// </summary>
	public decimal NashDeviation
	{
		get => _nashDeviation.Value;
		set => _nashDeviation.Value = value;
	}

	/// <summary>
	/// Enable the stop loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
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
	/// Enable the take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
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
	public GameTheoryTradingStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "Herd");

		_volumeMaLength = Param(nameof(VolumeMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Length", "Length of the volume average", "Herd");

		_herdThreshold = Param(nameof(HerdThreshold), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Herd Threshold", "Volume to average ratio that marks herd behaviour", "Herd");

		_liquidityLookback = Param(nameof(LiquidityLookback), 50)
			.SetGreaterThanZero()
			.SetDisplay("Liquidity Lookback", "Candles whose high and low define liquidity pools", "Liquidity");

		_instVolumeMultiplier = Param(nameof(InstVolumeMultiplier), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("Inst Volume Multiplier", "Volume to average ratio that marks institutional flow", "Institutional");

		_instMaLength = Param(nameof(InstMaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Inst MA Length", "Length of the institutional trend and A/D averages", "Institutional");

		_nashPeriod = Param(nameof(NashPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Nash Period", "Period of the Nash equilibrium average and deviation", "Nash");

		_nashDeviation = Param(nameof(NashDeviation), 0.02m)
			.SetNotNegative()
			.SetDisplay("Nash Deviation", "Relative distance from the equilibrium regarded as near it", "Nash");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable the stop loss", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Enable the take profit", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 5m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_prevHighest = null;
		_prevLowest = null;
		_prevClose = null;
		_prevUpperBand = null;
		_prevLowerBand = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ad = new AccumulationDistributionLine();
		var nashMa = new SimpleMovingAverage { Length = NashPeriod };
		var nashStd = new StandardDeviation { Length = NashPeriod };
		var instMa = new SimpleMovingAverage { Length = InstMaLength };
		var highest = new Highest { Length = LiquidityLookback };
		var lowest = new Lowest { Length = LiquidityLookback };

		_volumeMa = new SimpleMovingAverage { Length = VolumeMaLength };
		_adMa = new SimpleMovingAverage { Length = InstMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ad, nashMa, nashStd, instMa, highest, lowest, ProcessCandle)
			.Start();

		StartProtection(
			UseTakeProfit && TakeProfitPercent > 0 ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit(),
			UseStopLoss && StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, nashMa);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal ad, decimal nashMa, decimal nashStd, decimal instMa, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeValue = _volumeMa.Process(new DecimalIndicatorValue(_volumeMa, candle.TotalVolume, candle.OpenTime) { IsFinal = true });
		var adValue = _adMa.Process(new DecimalIndicatorValue(_adMa, ad, candle.OpenTime) { IsFinal = true });

		var close = candle.ClosePrice;
		var upperBand = nashMa + nashStd;
		var lowerBand = nashMa - nashStd;

		// Liquidity pools are the extremes of the candles before this one.
		var poolHigh = _prevHighest;
		var poolLow = _prevLowest;
		var prevClose = _prevClose;
		var prevUpper = _prevUpperBand;
		var prevLower = _prevLowerBand;

		_prevHighest = highest;
		_prevLowest = lowest;
		_prevClose = close;
		_prevUpperBand = upperBand;
		_prevLowerBand = lowerBand;

		if (!_volumeMa.IsFormed || !_adMa.IsFormed || poolHigh is not decimal high || poolLow is not decimal low || prevClose is not decimal pc)
			return;

		if (prevUpper is not decimal pUpper || prevLower is not decimal pLower)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volumeAvg = volumeValue.GetValue<decimal>();
		var adAvg = adValue.GetValue<decimal>();
		var volume = candle.TotalVolume;

		var volumeSpike = volumeAvg > 0 && volume > volumeAvg * HerdThreshold;
		var herdBuying = rsi > _rsiOverbought && volumeSpike;
		var herdSelling = rsi < _rsiOversold && volumeSpike;

		var bullTrap = candle.HighPrice > high && close < high;
		var bearTrap = candle.LowPrice < low && close > low;

		var institutional = volumeAvg > 0 && volume > volumeAvg * InstVolumeMultiplier;
		var accumulation = ad > adAvg;
		var distribution = ad < adAvg;

		var contrarianLong = herdSelling && (bearTrap || accumulation);
		var contrarianShort = herdBuying && (bullTrap || distribution);

		var momentumLong = institutional && accumulation && close > instMa && close > candle.OpenPrice;
		var momentumShort = institutional && distribution && close < instMa && close < candle.OpenPrice;

		var nashLong = pc < pLower && close > lowerBand;
		var nashShort = pc > pUpper && close < upperBand;

		var size = Volume;
		if (institutional)
			size *= 1.5m;
		if (nashMa > 0 && Math.Abs(close - nashMa) / nashMa <= NashDeviation)
			size /= 2m;

		if ((contrarianLong || momentumLong || nashLong) && Position <= 0)
			BuyMarket(size + Math.Abs(Position));
		else if ((contrarianShort || momentumShort || nashShort) && Position >= 0)
			SellMarket(size + Math.Abs(Position));
	}
}
