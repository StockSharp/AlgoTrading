using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the MetaTrader expert RPM5.
/// Rebuilds the BullsBearsEyes oscillator and follows its bullish/bearish bias.
/// Uses ATR based trailing stops together with fixed stop-loss and take-profit targets.
/// </summary>
public class Rpm5BullsBearsEyesStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _gamma;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private BullPower _bulls = null!;
	private BearPower _bears = null!;
	private AverageTrueRange _atr = null!;

	private decimal _l0;
	private decimal _l1;
	private decimal _l2;
	private decimal _l3;

	private decimal _pipSize;

	private decimal? _longEntry;
	private decimal? _longStop;
	private decimal? _longTake;

	private decimal? _shortEntry;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Initializes a new instance of <see cref="Rpm5BullsBearsEyesStrategy"/>.
	/// </summary>
	public Rpm5BullsBearsEyesStrategy()
	{
		_period = Param(nameof(Period), 13)
		.SetGreaterThanZero()
		.SetDisplay("Bulls/Bears Period", "Averaging period applied to Bulls Power and Bears Power.", "Indicator")
		.SetCanOptimize(true);

		_gamma = Param(nameof(Gamma), 0.5m)
		.SetRange(0m, 1m)
		.SetDisplay("Gamma", "Smoothing ratio used by the four-stage IIR filter.", "Indicator")
		.SetCanOptimize(true);

		_threshold = Param(nameof(Threshold), 0.5m)
		.SetRange(0m, 1m)
		.SetDisplay("Threshold", "Level separating bullish (above) and bearish (below) zones.", "Indicator")
		.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Lookback used to rebuild the ATR trailing distance.", "Risk")
		.SetCanOptimize(true);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "Multiplier applied to ATR when computing trailing distance.", "Risk")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 25m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Protective distance measured in pips.", "Risk")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Profit target distance measured in pips.", "Risk")
		.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Market order size for new entries.", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe processed by the strategy.", "General");
	}

	/// <summary>
	/// Bulls Power and Bears Power averaging period.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Smoothing factor used by the BullsBearsEyes filter.
	/// </summary>
	public decimal Gamma
	{
		get => _gamma.Value;
		set => _gamma.Value = value;
	}

	/// <summary>
	/// Threshold separating bullish and bearish zones.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// ATR length used for the trailing stop reconstruction.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the ATR value when computing trailing distance.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	/// Market order size for new entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_bulls = null!;
		_bears = null!;
		_atr = null!;

		_l0 = _l1 = _l2 = _l3 = 0m;
		_pipSize = 0m;

		_longEntry = null;
		_longStop = null;
		_longTake = null;

		_shortEntry = null;
		_shortStop = null;
		_shortTake = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Gamma < 0m || Gamma > 1m)
		throw new InvalidOperationException("Gamma must remain between 0 and 1.");

		Volume = TradeVolume;
		_pipSize = GetPipSize();

		_bulls = new BullPower { Length = Period };
		_bears = new BearPower { Length = Period };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_bulls, _bears, _atr, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal bullsValue, decimal bearsValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_bulls.IsFormed || !_bears.IsFormed || !_atr.IsFormed)
		return;

		// Manage existing positions before looking for fresh entries.
		ManageOpenPosition(candle, atrValue);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var ratio = CalculateRatio(bullsValue, bearsValue);
		if (ratio is not decimal current)
		return;

		if (Position != 0m)
		return;

		var volume = Volume;
		if (volume <= 0m)
		return;

		if (current > Threshold)
		{
			// Bulls dominate, open a long position if flat.
			BuyMarket(volume);
		}
		else if (current < Threshold)
		{
			// Bears dominate, open a short position if flat.
			SellMarket(volume);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position > 0m)
		{
			SetupLongTargets();
		}
		else if (Position < 0m)
		{
			SetupShortTargets();
		}
		else
		{
			ClearLongTargets();
			ClearShortTargets();
		}
	}

	private void ManageOpenPosition(ICandleMessage candle, decimal atrValue)
	{
		if (Position > 0m && _longEntry is decimal longEntry)
		{
			if (_longStop is decimal longStop && candle.LowPrice <= longStop)
			{
				// Long stop loss reached on the latest candle.
				SellMarket(Position);
				ClearLongTargets();
				return;
			}

			if (_longTake is decimal longTake && candle.HighPrice >= longTake)
			{
				// Long take profit touched.
				SellMarket(Position);
				ClearLongTargets();
				return;
			}

			UpdateLongTrailing(candle, longEntry, atrValue);
		}
		else if (Position < 0m && _shortEntry is decimal shortEntry)
		{
			if (_shortStop is decimal shortStop && candle.HighPrice >= shortStop)
			{
				// Short stop loss reached on the latest candle.
				BuyMarket(Math.Abs(Position));
				ClearShortTargets();
				return;
			}

			if (_shortTake is decimal shortTake && candle.LowPrice <= shortTake)
			{
				// Short take profit touched.
				BuyMarket(Math.Abs(Position));
				ClearShortTargets();
				return;
			}

			UpdateShortTrailing(candle, shortEntry, atrValue);
		}
	}

	private void UpdateLongTrailing(ICandleMessage candle, decimal entryPrice, decimal atrValue)
	{
		var trailingDistance = CalculateTrailingDistance(atrValue);
		if (trailingDistance <= 0m)
		return;

		var advance = candle.ClosePrice - entryPrice;
		if (advance <= trailingDistance)
		return;

		var newStop = candle.ClosePrice - trailingDistance;
		if (_longStop is not decimal currentStop || newStop > currentStop)
		_longStop = newStop;
	}

	private void UpdateShortTrailing(ICandleMessage candle, decimal entryPrice, decimal atrValue)
	{
		var trailingDistance = CalculateTrailingDistance(atrValue);
		if (trailingDistance <= 0m)
		return;

		var advance = entryPrice - candle.ClosePrice;
		if (advance <= trailingDistance)
		return;

		var newStop = candle.ClosePrice + trailingDistance;
		if (_shortStop is not decimal currentStop || newStop < currentStop)
		_shortStop = newStop;
	}

	private decimal CalculateTrailingDistance(decimal atrValue)
	{
		var pip = _pipSize;
		if (pip <= 0m)
		pip = 0.0001m;

		var distance = pip + (atrValue * AtrMultiplier);
		return distance > 0m ? distance : 0m;
	}

	private void SetupLongTargets()
	{
		if (PositionPrice is not decimal entryPrice)
		return;

		_longEntry = entryPrice;

		var pip = _pipSize;
		if (pip <= 0m)
		pip = 0.0001m;

		var stopDistance = StopLossPips * pip;
		_longStop = stopDistance > 0m ? entryPrice - stopDistance : null;

		var takeDistance = TakeProfitPips * pip;
		_longTake = takeDistance > 0m ? entryPrice + takeDistance : null;

		ClearShortTargets();
	}

	private void SetupShortTargets()
	{
		if (PositionPrice is not decimal entryPrice)
		return;

		_shortEntry = entryPrice;

		var pip = _pipSize;
		if (pip <= 0m)
		pip = 0.0001m;

		var stopDistance = StopLossPips * pip;
		_shortStop = stopDistance > 0m ? entryPrice + stopDistance : null;

		var takeDistance = TakeProfitPips * pip;
		_shortTake = takeDistance > 0m ? entryPrice - takeDistance : null;

		ClearLongTargets();
	}

	private void ClearLongTargets()
	{
		_longEntry = null;
		_longStop = null;
		_longTake = null;
	}

	private void ClearShortTargets()
	{
		_shortEntry = null;
		_shortStop = null;
		_shortTake = null;
	}

	private decimal? CalculateRatio(decimal bullsValue, decimal bearsValue)
	{
		var sum = bullsValue + bearsValue;
		var gamma = Gamma;

		var prevL0 = _l0;
		var prevL1 = _l1;
		var prevL2 = _l2;
		var prevL3 = _l3;

		// Four-stage IIR filter replicated from the original indicator.
		_l0 = ((1m - gamma) * sum) + (gamma * prevL0);
		_l1 = (-gamma * _l0) + prevL0 + (gamma * prevL1);
		_l2 = (-gamma * _l1) + prevL1 + (gamma * prevL2);
		_l3 = (-gamma * _l2) + prevL2 + (gamma * prevL3);

		var cu = 0m;
		var cd = 0m;

		if (_l0 >= _l1)
		cu = _l0 - _l1;
		else
		cd = _l1 - _l0;

		if (_l1 >= _l2)
		cu += _l1 - _l2;
		else
		cd += _l2 - _l1;

		if (_l2 >= _l3)
		cu += _l2 - _l3;
		else
		cd += _l3 - _l2;

		var denom = cu + cd;
		if (denom == 0m)
		return null;

		if (cu == 0m && cd > 0m)
		return 0m;

		if (cd == 0m && cu > 0m)
		return 1m;

		return cu / denom;
	}

	private decimal GetPipSize()
	{
		var priceStep = Security?.PriceStep;
		if (priceStep is null || priceStep == 0m)
		return 0.0001m;

		return priceStep.Value;
	}
}

