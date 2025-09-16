using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Replicates the Basic CCI RSI MetaTrader strategy with pip based risk controls and trailing stop logic.
/// </summary>
public class BasicCciRsiStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLevelUp;
	private readonly StrategyParam<decimal> _rsiLevelDown;
	private readonly StrategyParam<decimal> _cciLevelUp;
	private readonly StrategyParam<decimal> _cciLevelDown;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci;
	private RelativeStrengthIndex _rsi;

	private decimal? _previousCci;
	private decimal? _previousRsi;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _pipSize;

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
	/// Trailing stop activation distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional distance required before moving the trailing stop.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Averaging period for the Commodity Channel Index.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Averaging period for the Relative Strength Index.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Overbought threshold for RSI.
	/// </summary>
	public decimal RsiLevelUp
	{
		get => _rsiLevelUp.Value;
		set => _rsiLevelUp.Value = value;
	}

	/// <summary>
	/// Oversold threshold for RSI.
	/// </summary>
	public decimal RsiLevelDown
	{
		get => _rsiLevelDown.Value;
		set => _rsiLevelDown.Value = value;
	}

	/// <summary>
	/// Upper threshold for CCI confirmation.
	/// </summary>
	public decimal CciLevelUp
	{
		get => _cciLevelUp.Value;
		set => _cciLevelUp.Value = value;
	}

	/// <summary>
	/// Lower threshold for CCI confirmation.
	/// </summary>
	public decimal CciLevelDown
	{
		get => _cciLevelDown.Value;
		set => _cciLevelDown.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="BasicCciRsiStrategy"/> with default parameters.
	/// </summary>
	public BasicCciRsiStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 125m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 60m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop (pips)", "Trailing stop offset in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step (pips)", "Extra profit before trailing adjusts", "Risk")
			.SetCanOptimize(true);

		_cciPeriod = Param(nameof(CciPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Commodity Channel Index period", "Indicators")
			.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Relative Strength Index period", "Indicators")
			.SetCanOptimize(true);

		_rsiLevelUp = Param(nameof(RsiLevelUp), 75m)
			.SetDisplay("RSI Upper Level", "RSI overbought confirmation", "Indicators")
			.SetCanOptimize(true);

		_rsiLevelDown = Param(nameof(RsiLevelDown), 30m)
			.SetDisplay("RSI Lower Level", "RSI oversold confirmation", "Indicators")
			.SetCanOptimize(true);

		_cciLevelUp = Param(nameof(CciLevelUp), 80m)
			.SetDisplay("CCI Upper Level", "CCI overbought confirmation", "Indicators")
			.SetCanOptimize(true);

		_cciLevelDown = Param(nameof(CciLevelDown), -95m)
			.SetDisplay("CCI Lower Level", "CCI oversold confirmation", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for calculations", "General");
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

		_previousCci = null;
		_previousRsi = null;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = 0m;
		if (Security?.PriceStep != null)
		{
			_pipSize = Security.PriceStep.Value;

			if (Security.Decimals is 3 or 5)
				_pipSize *= 10m;
		}

		if (_pipSize <= 0m)
			_pipSize = 1m;

		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci, _rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var exited = ManagePosition(candle);
		if (exited)
		{
			_previousCci = cciValue;
			_previousRsi = rsiValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousCci = cciValue;
			_previousRsi = rsiValue;
			return;
		}

		if (_previousCci.HasValue && _previousRsi.HasValue)
		{
			var rsiBuy = rsiValue > RsiLevelUp && _previousRsi.Value > RsiLevelUp;
			var rsiSell = rsiValue < RsiLevelDown && _previousRsi.Value < RsiLevelDown;

			var cciBuy = cciValue > CciLevelUp && _previousCci.Value > CciLevelUp;
			var cciSell = cciValue < CciLevelDown && _previousCci.Value < CciLevelDown;

			if (rsiBuy && cciBuy && Position <= 0)
				EnterLong(candle);
			if (rsiSell && cciSell && Position >= 0)
				EnterShort(candle);
		}

		_previousCci = cciValue;
		_previousRsi = rsiValue;
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position == 0)
			return false;

		var trailingStop = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (Position > 0)
		{
			var stopHit = _stopPrice > 0m && candle.LowPrice <= _stopPrice;
			var takeHit = _takePrice > 0m && candle.HighPrice >= _takePrice;

			if (stopHit || takeHit)
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}

			if (TrailingStopPips > 0m && trailingStop > 0m)
			{
				var profit = candle.ClosePrice - _entryPrice;
				var threshold = trailingStop + trailingStep;
				if (profit > threshold)
				{
					var desiredStop = candle.ClosePrice - trailingStop;
					if (_stopPrice == 0m || _stopPrice < candle.ClosePrice - threshold)
					{
						_stopPrice = desiredStop;
						LogInfo($"Update long trailing stop to {_stopPrice}");
					}
				}
			}
		}
		else
		{
			var stopHit = _stopPrice > 0m && candle.HighPrice >= _stopPrice;
			var takeHit = _takePrice > 0m && candle.LowPrice <= _takePrice;

			if (stopHit || takeHit)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return true;
			}

			if (TrailingStopPips > 0m && trailingStop > 0m)
			{
				var profit = _entryPrice - candle.ClosePrice;
				var threshold = trailingStop + trailingStep;
				if (profit > threshold)
				{
					var desiredStop = candle.ClosePrice + trailingStop;
					if (_stopPrice == 0m || _stopPrice > candle.ClosePrice + threshold)
					{
						_stopPrice = desiredStop;
						LogInfo($"Update short trailing stop to {_stopPrice}");
					}
				}
			}
		}

		return false;
	}

	private void EnterLong(ICandleMessage candle)
	{
		var volume = Volume + Math.Max(0m, -Position);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_entryPrice = candle.ClosePrice;
		_stopPrice = CalculateLongStop(candle.ClosePrice);
		_takePrice = CalculateLongTake(candle.ClosePrice);
	}

	private void EnterShort(ICandleMessage candle)
	{
		var volume = Volume + Math.Max(0m, Position);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		_entryPrice = candle.ClosePrice;
		_stopPrice = CalculateShortStop(candle.ClosePrice);
		_takePrice = CalculateShortTake(candle.ClosePrice);
	}

	private decimal CalculateLongStop(decimal price)
	{
		var distance = StopLossPips * _pipSize;
		if (distance <= 0m)
			return _stopPrice;

		var level = price - distance;
		return level > 0m ? level : 0m;
	}

	private decimal CalculateShortStop(decimal price)
	{
		var distance = StopLossPips * _pipSize;
		if (distance <= 0m)
			return _stopPrice;

		return price + distance;
	}

	private decimal CalculateLongTake(decimal price)
	{
		var distance = TakeProfitPips * _pipSize;
		if (distance <= 0m)
			return 0m;

		return price + distance;
	}

	private decimal CalculateShortTake(decimal price)
	{
		var distance = TakeProfitPips * _pipSize;
		if (distance <= 0m)
			return 0m;

		var level = price - distance;
		return level > 0m ? level : 0m;
	}

	private void ResetProtection()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}
}
