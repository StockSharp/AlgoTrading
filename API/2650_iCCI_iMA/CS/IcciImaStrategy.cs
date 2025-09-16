using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI and EMA crossover strategy converted from the MetaTrader iCCI iMA expert.
/// The strategy trades when the Commodity Channel Index crosses its exponential moving average.
/// </summary>
public class IcciImaStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _cciClosePeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _depositPerLot;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci = null!;
	private CommodityChannelIndex _cciClose = null!;
	private ExponentialMovingAverage _cciMa = null!;

	private decimal _pipSize;
	private decimal _lotMultiplier = 1m;
	private decimal? _entryPrice;
	private decimal? _prevCci;
	private decimal? _prev2Cci;
	private decimal? _prevCciClose;
	private decimal? _prev2CciClose;
	private decimal? _prevMa;
	private decimal? _prev2Ma;
	private int _historyCount;

	/// <summary>
	/// Constructor.
	/// </summary>
	public IcciImaStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("CCI Period", "Length of the main CCI indicator", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 100, 1);

		_cciClosePeriod = Param(nameof(CciClosePeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("CCI Close Period", "Length of the CCI used for overbought and oversold exits", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 100, 1);

		_maPeriod = Param(nameof(MaPeriod), 15)
		.SetGreaterThanZero()
		.SetDisplay("CCI EMA Period", "Length of the EMA applied to the CCI values", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 100, 1);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 40m)
		.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk");

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
		.SetDisplay("Enable Money Management", "Scale position size by account balance", "Money Management");

		_depositPerLot = Param(nameof(DepositPerLot), 1000m)
		.SetGreaterThanZero()
		.SetDisplay("Deposit Per Lot", "Balance required to increase the lot multiplier", "Money Management");

		_lotSize = Param(nameof(LotSize), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Size", "Base trading volume in lots", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.01m, 1m, 0.01m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Data series used for calculations", "General");
	}

	/// <summary>
	/// Length of the primary CCI indicator.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Length of the CCI used for exit signals around ±100.
	/// </summary>
	public int CciClosePeriod
	{
		get => _cciClosePeriod.Value;
		set => _cciClosePeriod.Value = value;
	}

	/// <summary>
	/// Exponential moving average period applied to the CCI values.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enable adaptive money management.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Deposit amount required to increase the lot multiplier by one.
	/// </summary>
	public decimal DepositPerLot
	{
		get => _depositPerLot.Value;
		set => _depositPerLot.Value = value;
	}

	/// <summary>
	/// Base trading volume in lots.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
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
			CandlePrice = CandlePrice.Typical,
		};

		_cciClose = new CommodityChannelIndex
		{
			Length = CciClosePeriod,
			CandlePrice = CandlePrice.Typical,
		};

		_cciMa = new ExponentialMovingAverage
		{
			Length = MaPeriod,
		};

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_cci, _cciClose, ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal cciCloseValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var maValue = _cciMa.Process(cciValue, candle.OpenTime, true).ToDecimal();

		if (!_cci.IsFormed || !_cciClose.IsFormed || !_cciMa.IsFormed)
		{
			UpdateHistory(cciValue, cciCloseValue, maValue);
			return;
		}

		// Update the lot multiplier according to the current balance settings.
		UpdateMoneyManagement();

		if (_historyCount < 2)
		{
			UpdateHistory(cciValue, cciCloseValue, maValue);
			return;
		}

		// Check whether stop-loss or take-profit levels were touched on the latest candle.
		HandleStops(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistory(cciValue, cciCloseValue, maValue);
			return;
		}

		var cciTwoBarsAgo = _prev2Cci ?? 0m;
		var maTwoBarsAgo = _prev2Ma ?? 0m;
		var cciCloseTwoBarsAgo = _prev2CciClose ?? 0m;

		// Determine exit conditions from the secondary CCI and the smoothed crossover.
		var shouldCloseLong = (cciCloseTwoBarsAgo > 100m && cciCloseValue <= 100m) || (cciValue < maValue && cciTwoBarsAgo >= maTwoBarsAgo);
		var shouldCloseShort = (cciCloseTwoBarsAgo < -100m && cciCloseValue >= -100m) || (cciValue > maValue && cciTwoBarsAgo <= maTwoBarsAgo);

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

		// Validate the requested lot size against security constraints.
		var volume = AdjustVolume(LotSize * _lotMultiplier);

		if (volume > 0m)
		{
			if (cciValue > maValue && cciTwoBarsAgo < maTwoBarsAgo && Position <= 0)
			{
				var totalVolume = volume + Math.Abs(Position);
				if (totalVolume > 0m)
				{
					BuyMarket(totalVolume);
					_entryPrice = candle.ClosePrice;
				}
			}
			else if (cciValue < maValue && cciTwoBarsAgo > maTwoBarsAgo && Position >= 0)
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

		UpdateHistory(cciValue, cciCloseValue, maValue);
	}

	private void HandleStops(ICandleMessage candle)
	{
		if (_entryPrice == null)
		return;

		var priceStep = _pipSize > 0m ? _pipSize : Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return;

		// Convert the configured pip distances into absolute price offsets.
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

		var ratio = (int)(balance.Value / DepositPerLot);
		if (ratio < 2)
		return;

		// Cap the multiplier at twenty lots, replicating the MQL expert behaviour.
		_lotMultiplier = Math.Min(20, ratio);
	}

	private void UpdateHistory(decimal cciValue, decimal cciCloseValue, decimal maValue)
	{
		// Shift cached values so the strategy can access readings from two completed candles ago.
		_prev2Cci = _prevCci;
		_prevCci = cciValue;

		_prev2CciClose = _prevCciClose;
		_prevCciClose = cciCloseValue;

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
			// Align the order size with the instrument volume step.
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
		// Symbols with three or five digits require a tenfold pip multiplier.
		var multiplier = scale == 3 || scale == 5 ? 10m : 1m;

		return priceStep * multiplier;
	}

	private void ResetState()
	{
		// Restore cached values and multipliers before a new backtest/run.
		_lotMultiplier = 1m;
		_entryPrice = null;
		_prevCci = null;
		_prev2Cci = null;
		_prevCciClose = null;
		_prev2CciClose = null;
		_prevMa = null;
		_prev2Ma = null;
		_historyCount = 0;
	}
}
