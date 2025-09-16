using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "E-Skoch-Open" MetaTrader strategy using StockSharp high level API.
/// The strategy reacts to a three-candle closing price pattern and applies
/// martingale position sizing together with equity based stops.
/// </summary>
public class ESkochOpenStrategy : Strategy
{
	private const decimal MartingaleMultiplier = 1.6m;

	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableBuySignals;
	private readonly StrategyParam<bool> _enableSellSignals;
	private readonly StrategyParam<decimal> _targetProfitPercent;
	private readonly StrategyParam<bool> _closeOnOppositeSignal;
	private readonly StrategyParam<int> _maxBuyTrades;
	private readonly StrategyParam<int> _maxSellTrades;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _initialOrderVolume;

	private decimal _pointValue;
	private decimal _currentVolume;
	private decimal _entryEquity;
	private decimal _baselineEquity;
	private bool _positionTracked;

	private decimal? _closeMinus1;
	private decimal? _closeMinus2;
	private decimal? _closeMinus3;

	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	private int _activeLongEntries;
	private int _activeShortEntries;

	/// <summary>
	/// Stop loss distance expressed in adjusted points (default: 130).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in adjusted points (default: 200).
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables long entries created by the pattern.
	/// </summary>
	public bool EnableBuySignals
	{
		get => _enableBuySignals.Value;
		set => _enableBuySignals.Value = value;
	}

	/// <summary>
	/// Enables short entries created by the pattern.
	/// </summary>
	public bool EnableSellSignals
	{
		get => _enableSellSignals.Value;
		set => _enableSellSignals.Value = value;
	}

	/// <summary>
	/// Equity percentage gain that triggers closing every open position.
	/// </summary>
	public decimal TargetProfitPercent
	{
		get => _targetProfitPercent.Value;
		set => _targetProfitPercent.Value = value;
	}

	/// <summary>
	/// When true, opposite trades immediately flatten the existing position.
	/// </summary>
	public bool CloseOnOppositeSignal
	{
		get => _closeOnOppositeSignal.Value;
		set => _closeOnOppositeSignal.Value = value;
	}

	/// <summary>
	/// Maximum number of consecutive long entries (-1 disables the limit).
	/// </summary>
	public int MaxBuyTrades
	{
		get => _maxBuyTrades.Value;
		set => _maxBuyTrades.Value = value;
	}

	/// <summary>
	/// Maximum number of consecutive short entries (-1 disables the limit).
	/// </summary>
	public int MaxSellTrades
	{
		get => _maxSellTrades.Value;
		set => _maxSellTrades.Value = value;
	}

	/// <summary>
	/// Candle type used for pattern detection.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base order volume used for the first trade in a sequence.
	/// </summary>
	public decimal InitialOrderVolume
	{
		get => _initialOrderVolume.Value;
		set => _initialOrderVolume.Value = value;
	}

	/// <summary>
	/// Creates the strategy parameters with defaults similar to the MQL version.
	/// </summary>
	public ESkochOpenStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 130m)
		.SetDisplay("Stop Loss Points", "Loss distance measured in adjusted points", "Risk")
		.SetGreaterThanOrEqual(0m);
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 200m)
		.SetDisplay("Take Profit Points", "Profit distance measured in adjusted points", "Risk")
		.SetGreaterThanOrEqual(0m);
		_enableBuySignals = Param(nameof(EnableBuySignals), true)
		.SetDisplay("Enable Buy", "Allow opening long positions", "Trading");
		_enableSellSignals = Param(nameof(EnableSellSignals), true)
		.SetDisplay("Enable Sell", "Allow opening short positions", "Trading");
		_targetProfitPercent = Param(nameof(TargetProfitPercent), 1.2m)
		.SetDisplay("Target Profit %", "Close all positions after reaching this equity growth", "Risk")
		.SetGreaterThanOrEqual(0m);
		_closeOnOppositeSignal = Param(nameof(CloseOnOppositeSignal), false)
		.SetDisplay("Close On Opposite", "Close open positions when an opposite signal appears", "Trading");
		_maxBuyTrades = Param(nameof(MaxBuyTrades), 1)
		.SetDisplay("Max Long Trades", "Maximum concurrent long trades", "Risk");
		_maxSellTrades = Param(nameof(MaxSellTrades), 1)
		.SetDisplay("Max Short Trades", "Maximum concurrent short trades", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for pattern recognition", "Data");
		_initialOrderVolume = Param(nameof(InitialOrderVolume), 0.01m)
		.SetDisplay("Initial Volume", "Volume of the first trade", "Trading")
		.SetGreaterThanZero();
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

		_closeMinus1 = null;
		_closeMinus2 = null;
		_closeMinus3 = null;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_activeLongEntries = 0;
		_activeShortEntries = 0;
		_positionTracked = false;
		_pointValue = 0m;
		_currentVolume = 0m;
		_entryEquity = 0m;
		_baselineEquity = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = InitialOrderVolume;
		_pointValue = CalculatePointValue();
		_currentVolume = NormalizeVolume(InitialOrderVolume);

		var equity = Portfolio?.CurrentValue ?? 0m;
		_baselineEquity = equity;
		_entryEquity = equity;
		_positionTracked = Position != 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}


		CheckEquityTarget();

		if (CheckProtection(candle))
		{
			// Skip new entries if a protection exit already triggered on this bar.
			UpdateCloses(candle.ClosePrice);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateCloses(candle.ClosePrice);
			return;
		}

		if (_closeMinus1.HasValue && _closeMinus2.HasValue && _closeMinus3.HasValue)
		{
			var close1 = _closeMinus1.Value;
			var close2 = _closeMinus2.Value;
			var close3 = _closeMinus3.Value;

			var buySignal = close3 > close2 && close1 < close2;
			var sellSignal = close3 > close2 && close2 < close1;

			if (buySignal)
			{
				HandleBuySignal(candle);
			}

			if (sellSignal)
			{
				HandleSellSignal(candle);
			}
		}

		UpdateCloses(candle.ClosePrice);
	}

	private void HandleBuySignal(ICandleMessage candle)
	{
		if (!EnableBuySignals)
		{
			return;
		}

		if (CloseOnOppositeSignal && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			return;
		}

		if (Position > 0)
		{
			return;
		}

		if (MaxBuyTrades != -1 && _activeLongEntries >= MaxBuyTrades)
		{
			return;
		}

		var volume = NormalizeVolume(_currentVolume);
		if (volume <= 0m)
		{
			return;
		}

		BuyMarket(volume);
		_activeLongEntries++;
		_positionTracked = true;
		_entryEquity = Portfolio?.CurrentValue ?? _entryEquity;
		SetupProtection(true, candle.ClosePrice);
	}

	private void HandleSellSignal(ICandleMessage candle)
	{
		if (!EnableSellSignals)
		{
			return;
		}

		if (CloseOnOppositeSignal && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			return;
		}

		if (Position < 0)
		{
			return;
		}

		if (MaxSellTrades != -1 && _activeShortEntries >= MaxSellTrades)
		{
			return;
		}

		var volume = NormalizeVolume(_currentVolume);
		if (volume <= 0m)
		{
			return;
		}

		SellMarket(volume);
		_activeShortEntries++;
		_positionTracked = true;
		_entryEquity = Portfolio?.CurrentValue ?? _entryEquity;
		SetupProtection(false, candle.ClosePrice);
	}

	private bool CheckProtection(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}

			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}

			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}
		}

		return false;
	}

	private void SetupProtection(bool isLong, decimal referencePrice)
	{
		var point = _pointValue;
		if (point <= 0m)
		{
			point = Security?.PriceStep ?? 0m;
		}

		if (isLong)
		{
			_longStop = StopLossPoints > 0m ? referencePrice - StopLossPoints * point : null;
			_longTake = TakeProfitPoints > 0m ? referencePrice + TakeProfitPoints * point : null;
			_shortStop = null;
			_shortTake = null;
		}
		else
		{
			_shortStop = StopLossPoints > 0m ? referencePrice + StopLossPoints * point : null;
			_shortTake = TakeProfitPoints > 0m ? referencePrice - TakeProfitPoints * point : null;
			_longStop = null;
			_longTake = null;
		}
	}

	private void ResetProtection()
	{
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
	}

	private void UpdateCloses(decimal close)
	{
		_closeMinus3 = _closeMinus2;
		_closeMinus2 = _closeMinus1;
		_closeMinus1 = close;
	}

	private void CheckEquityTarget()
	{
		if (TargetProfitPercent <= 0m)
		{
			return;
		}

		if (_baselineEquity <= 0m)
		{
			return;
		}

		var equity = Portfolio?.CurrentValue ?? 0m;
		var growthPercent = (equity - _baselineEquity) / _baselineEquity * 100m;

		if (growthPercent >= TargetProfitPercent)
		{
			CloseAllPositions();
		}
	}

	private void CloseAllPositions()
	{
		if (Position > 0)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			if (_positionTracked)
			{
				var equity = Portfolio?.CurrentValue ?? _baselineEquity;
				if (equity >= _entryEquity)
				{
					_currentVolume = NormalizeVolume(InitialOrderVolume);
				}
				else
				{
					_currentVolume = NormalizeVolume(_currentVolume * MartingaleMultiplier);
				}

				_baselineEquity = equity;
				_positionTracked = false;
				ResetProtection();
				_activeLongEntries = 0;
				_activeShortEntries = 0;
			}
			else
			{
				_baselineEquity = Portfolio?.CurrentValue ?? _baselineEquity;
			}
		}
		else
		{
			_positionTracked = true;
			_entryEquity = Portfolio?.CurrentValue ?? _entryEquity;
		}
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
		{
			return 0m;
		}

		var sec = Security;
		if (sec != null)
		{
			var step = sec.VolumeStep ?? 0m;
			if (step > 0m)
			{
				volume = Math.Floor(volume / step) * step;
			}

			var min = sec.VolumeMin ?? 0m;
			if (min > 0m && volume < min)
			{
				volume = min;
			}

			var max = sec.VolumeMax ?? 0m;
			if (max > 0m && volume > max)
			{
				volume = max;
			}
		}

		return volume;
	}

	private decimal CalculatePointValue()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		{
			return 0m;
		}

		var decimals = CountDecimals(step);
		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}

	private static int CountDecimals(decimal value)
	{
		value = Math.Abs(value);
		var decimals = 0;

		while (value != Math.Truncate(value) && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}
