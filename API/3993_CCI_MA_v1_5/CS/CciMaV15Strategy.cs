namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo;

/// <summary>
/// Commodity Channel Index strategy converted from the MetaTrader "CCI_MA v1.5" expert advisor.
/// The algorithm waits for the primary CCI to cross a simple moving average of its own values
/// and relies on a secondary CCI to confirm overbought or oversold reversals.
/// </summary>
public class CciMaV15Strategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _signalCciPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _lotVolume;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _depositPerLot;
	private readonly StrategyParam<int> _maxMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci = null!;
	private CommodityChannelIndex _signalCci = null!;
	private SimpleMovingAverage _cciSignal = null!;

	private decimal _pipSize;
	private decimal _lotMultiplier = 1m;
	private decimal? _entryPrice;
	private decimal? _prevCci;
	private decimal? _prev2Cci;
	private decimal? _prevSignalCci;
	private decimal? _prev2SignalCci;
	private decimal? _prevMa;
	private decimal? _prev2Ma;
	private int _historyCount;

	/// <summary>
	/// Primary CCI period used to detect momentum swings.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Secondary CCI period that supervises exits around ±100.
	/// </summary>
	public int SignalCciPeriod
	{
		get => _signalCciPeriod.Value;
		set => _signalCciPeriod.Value = value;
	}

	/// <summary>
	/// Length of the simple moving average applied to the primary CCI.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips (set to zero to disable).
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips (set to zero to disable).
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Base volume traded on each signal before scaling.
	/// </summary>
	public decimal LotVolume
	{
		get => _lotVolume.Value;
		set => _lotVolume.Value = value;
	}

	/// <summary>
	/// Enables deposit-based volume scaling.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Balance amount required for one additional lot when money management is enabled.
	/// </summary>
	public decimal DepositPerLot
	{
		get => _depositPerLot.Value;
		set => _depositPerLot.Value = value;
	}

	/// <summary>
	/// Upper bound for the money management multiplier.
	/// </summary>
	public int MaxMultiplier
	{
		get => _maxMultiplier.Value;
		set => _maxMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type used to feed the indicators.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public CciMaV15Strategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("CCI Period", "Length of the primary CCI", "CCI")
		.SetCanOptimize(true)
		.SetOptimize(7, 35, 7);

		_signalCciPeriod = Param(nameof(SignalCciPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Exit CCI Period", "Length of the secondary CCI", "CCI")
		.SetCanOptimize(true)
		.SetOptimize(7, 35, 7);

		_maPeriod = Param(nameof(MaPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("CCI MA Period", "Simple MA length applied to the CCI", "CCI")
		.SetCanOptimize(true)
		.SetOptimize(3, 21, 3);

		_stopLossPips = Param(nameof(StopLossPips), 40m)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 120m, 20m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0m, 150m, 25m);

		_lotVolume = Param(nameof(LotVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Volume", "Base order volume before scaling", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 2m, 0.5m);

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
		.SetDisplay("Enable Money Management", "Toggle balance-based lot scaling", "Trading");

		_depositPerLot = Param(nameof(DepositPerLot), 1000m)
		.SetGreaterThanZero()
		.SetDisplay("Deposit Per Lot", "Balance required to increase the lot multiplier", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(500m, 5000m, 500m);

		_maxMultiplier = Param(nameof(MaxMultiplier), 20)
		.SetGreaterThanZero()
		.SetDisplay("Max Multiplier", "Upper bound for the lot multiplier", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Market data series used for calculations", "General");
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

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();

		_cci = new CommodityChannelIndex
		{
			Length = CciPeriod,
			CandlePrice = CandlePrice.Close,
		};

		_signalCci = new CommodityChannelIndex
		{
			Length = SignalCciPeriod,
			CandlePrice = CandlePrice.Close,
		};

		_cciSignal = new SimpleMovingAverage
		{
			Length = MaPeriod,
		};

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_cci, _signalCci, ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal signalCciValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var maValue = _cciSignal.Process(cciValue, candle.OpenTime, true).ToDecimal();

		if (!_cci.IsFormed || !_signalCci.IsFormed || !_cciSignal.IsFormed)
		{
			UpdateHistory(cciValue, signalCciValue, maValue);
			return;
		}

		UpdateMoneyManagement();

		if (_historyCount < 2)
		{
			UpdateHistory(cciValue, signalCciValue, maValue);
			return;
		}

		HandleStops(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistory(cciValue, signalCciValue, maValue);
			return;
		}

		var prevCci = _prevCci ?? cciValue;
		var prev2Cci = _prev2Cci ?? prevCci;
		var prevSignal = _prevSignalCci ?? signalCciValue;
		var prev2Signal = _prev2SignalCci ?? prevSignal;
		var prevMa = _prevMa ?? maValue;
		var prev2Ma = _prev2Ma ?? prevMa;

		var shouldCloseLong = (prev2Signal > 100m && prevSignal <= 100m) || (prevCci < prevMa && prev2Cci >= prev2Ma);
		var shouldCloseShort = (prev2Signal < -100m && prevSignal >= -100m) || (prevCci > prevMa && prev2Cci <= prev2Ma);

		if (Position > 0 && shouldCloseLong)
		{
			SellMarket(Position);
			_entryPrice = null;
		}
		else if (Position < 0 && shouldCloseShort)
		{
			BuyMarket(Math.Abs(Position));
			_entryPrice = null;
		}

		var volume = AdjustVolume(LotVolume * _lotMultiplier);
		if (volume > 0m)
		{
			if (prevCci > prevMa && prev2Cci < prev2Ma && Position <= 0)
			{
				var totalVolume = volume + Math.Abs(Position);
				if (totalVolume > 0m)
				{
					BuyMarket(totalVolume);
					_entryPrice = candle.ClosePrice;
				}
			}
			else if (prevCci < prevMa && prev2Cci > prev2Ma && Position >= 0)
			{
				var totalVolume = volume + Math.Abs(Position);
				if (totalVolume > 0m)
				{
					SellMarket(totalVolume);
					_entryPrice = candle.ClosePrice;
				}
			}
		}

		if (Position == 0)
		_entryPrice = null;

		UpdateHistory(cciValue, signalCciValue, maValue);
	}

	private void HandleStops(ICandleMessage candle)
	{
		if (_entryPrice == null)
		return;

		var priceStep = _pipSize > 0m ? _pipSize : Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return;

		var stopLossDistance = StopLossPips > 0m ? StopLossPips * priceStep : 0m;
		var takeProfitDistance = TakeProfitPips > 0m ? TakeProfitPips * priceStep : 0m;

		if (Position > 0)
		{
			var entry = _entryPrice.Value;

			if (stopLossDistance > 0m && candle.LowPrice <= entry - stopLossDistance)
			{
				SellMarket(Position);
				_entryPrice = null;
				return;
			}

			if (takeProfitDistance > 0m && candle.HighPrice >= entry + takeProfitDistance)
			{
				SellMarket(Position);
				_entryPrice = null;
			}
		}
		else if (Position < 0)
		{
			var entry = _entryPrice.Value;
			var absPosition = Math.Abs(Position);

			if (stopLossDistance > 0m && candle.HighPrice >= entry + stopLossDistance)
			{
				BuyMarket(absPosition);
				_entryPrice = null;
				return;
			}

			if (takeProfitDistance > 0m && candle.LowPrice <= entry - takeProfitDistance)
			{
				BuyMarket(absPosition);
				_entryPrice = null;
			}
		}
	}

	private void UpdateMoneyManagement()
	{
		if (!UseMoneyManagement)
		{
			_lotMultiplier = 1m;
			return;
		}

		if (DepositPerLot <= 0m)
		return;

		var balance = Portfolio?.CurrentValue;
		if (balance == null || balance <= 0m)
		return;

		var multiplier = (int)(balance.Value / DepositPerLot);
		if (multiplier < 2)
		{
			_lotMultiplier = 1m;
			return;
		}

		_lotMultiplier = Math.Min(MaxMultiplier, multiplier);
	}

	private void UpdateHistory(decimal cciValue, decimal signalCciValue, decimal maValue)
	{
		_prev2Cci = _prevCci;
		_prevCci = cciValue;

		_prev2SignalCci = _prevSignalCci;
		_prevSignalCci = signalCciValue;

		_prev2Ma = _prevMa;
		_prevMa = maValue;

		if (_historyCount < 2)
		_historyCount++;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0m;

		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (volume < minVolume)
		return 0m;

		var maxVolume = security.MaxVolume;
		if (maxVolume != null && volume > maxVolume.Value)
		volume = maxVolume.Value;

		return volume;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return 0m;

		var bits = decimal.GetBits(priceStep);
		var scale = (bits[3] >> 16) & 0xFF;
		var multiplier = scale == 3 || scale == 5 ? 10m : 1m;

		return priceStep * multiplier;
	}

	private void ResetState()
	{
		_lotMultiplier = 1m;
		_entryPrice = null;
		_prevCci = null;
		_prev2Cci = null;
		_prevSignalCci = null;
		_prev2SignalCci = null;
		_prevMa = null;
		_prev2Ma = null;
		_historyCount = 0;
	}
}

