using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that trades when the closing price crosses a predefined level.
/// The strategy sizes positions based on the selected risk percentage and applies static stop-loss and take-profit levels.
/// </summary>
public class BrakeoutTraderV1Strategy : Strategy
{
	private readonly StrategyParam<decimal> _breakoutLevel;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousClose;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _pipSize;

	/// <summary>
	/// Price level that must be broken to generate signals.
	/// </summary>
	public decimal BreakoutLevel
	{
		get => _breakoutLevel.Value;
		set => _breakoutLevel.Value = value;
	}

	/// <summary>
	/// Enables or disables long breakout trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Enables or disables short breakout trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in points relative to the pip size.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points relative to the pip size.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Percentage of account equity to risk on each trade.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate breakouts.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BrakeoutTraderV1Strategy"/>.
	/// </summary>
	public BrakeoutTraderV1Strategy()
	{
		_breakoutLevel = Param(nameof(BreakoutLevel), 0m)
			.SetDisplay("Breakout Level", "Static price level monitored for breakouts", "Signal");

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long breakout positions", "Signal");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short breakout positions", "Signal");

		_stopLossPoints = Param(nameof(StopLossPoints), 140m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Points", "Stop-loss distance expressed in pip points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 180m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Points", "Take-profit distance expressed in pip points", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Percentage of equity risked per trade", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for breakout detection", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousClose = null;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var priceStep = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals;
		_pipSize = priceStep;

		if (decimals is 3 or 5)
			_pipSize *= 10m;

		if (_pipSize <= 0m)
			_pipSize = 1m;

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

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (ManageOpenPosition(candle))
		{
			UpdatePreviousClose(candle.ClosePrice);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdatePreviousClose(candle.ClosePrice);
			return;
		}

		var prevClose = _previousClose;
		if (prevClose is null)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var currentClose = candle.ClosePrice;
		var prevValue = prevClose.Value;

		var breakoutUp = currentClose > BreakoutLevel && prevValue <= BreakoutLevel;
		var breakoutDown = currentClose < BreakoutLevel && prevValue >= BreakoutLevel;

		if (breakoutUp && EnableLong)
		{
			EnterLong(currentClose);
		}
		else if (breakoutDown && EnableShort)
		{
			EnterShort(currentClose);
		}

		_previousClose = currentClose;
	}

	private bool ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			// Exit long if stop-loss is touched.
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}

			// Exit long if take-profit is reached.
			if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}
		}
		else if (Position < 0)
		{
			// Exit short if stop-loss is touched.
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}

			// Exit short if take-profit is reached.
			if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}
		}
		else if (_entryPrice.HasValue)
		{
			// Reset cached levels after position is fully closed.
			ResetPositionState();
		}

		return false;
	}

	private void EnterLong(decimal price)
	{
		if (Position > 0)
			return;

		var volume = CalculateOrderVolume();
		var closingVolume = Position < 0 ? Math.Abs(Position) : 0m;
		var totalVolume = closingVolume + volume;

		if (totalVolume <= 0m)
			return;

		if (closingVolume > 0m)
			ResetPositionState();

		BuyMarket(totalVolume);
		SetPositionTargets(price, true, volume > 0m);
	}

	private void EnterShort(decimal price)
	{
		if (Position < 0)
			return;

		var volume = CalculateOrderVolume();
		var closingVolume = Position > 0 ? Position : 0m;
		var totalVolume = closingVolume + volume;

		if (totalVolume <= 0m)
			return;

		if (closingVolume > 0m)
			ResetPositionState();

		SellMarket(totalVolume);
		SetPositionTargets(price, false, volume > 0m);
	}

	private void SetPositionTargets(decimal entryPrice, bool isLong, bool hasNewPosition)
	{
		if (!hasNewPosition)
		{
			return;
		}

		_entryPrice = entryPrice;

		if (StopLossPoints > 0m && _pipSize > 0m)
			_stopPrice = isLong
				? entryPrice - StopLossPoints * _pipSize
				: entryPrice + StopLossPoints * _pipSize;
		else
			_stopPrice = null;

		if (TakeProfitPoints > 0m && _pipSize > 0m)
			_takePrice = isLong
				? entryPrice + TakeProfitPoints * _pipSize
				: entryPrice - TakeProfitPoints * _pipSize;
		else
			_takePrice = null;
	}

	private decimal CalculateOrderVolume()
	{
		var baseVolume = Volume;
		var stopDistance = StopLossPoints * _pipSize;

		if (stopDistance <= 0m || RiskPercent <= 0m)
			return AdjustVolume(baseVolume);

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
			return AdjustVolume(baseVolume);

		var riskValue = equity * RiskPercent / 100m;
		if (riskValue <= 0m)
			return AdjustVolume(baseVolume);

		var qty = riskValue / stopDistance;
		var adjusted = AdjustVolume(qty);

		return adjusted > 0m ? adjusted : AdjustVolume(baseVolume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;
		if (security != null)
		{
			var step = security.VolumeStep;
			if (step is decimal s && s > 0m)
			{
				volume = Math.Floor(volume / s) * s;
			}

			var min = security.VolumeMin;
			if (min is decimal minVol && volume < minVol)
				volume = minVol;

			var max = security.VolumeMax;
			if (max is decimal maxVol && maxVol > 0m && volume > maxVol)
				volume = maxVol;

			if (volume <= 0m)
				volume = step is decimal stepVal && stepVal > 0m ? stepVal : 0m;
		}

		if (volume <= 0m)
			volume = volume == 0m ? 1m : Math.Abs(volume);

		return volume;
	}

	private void UpdatePreviousClose(decimal close)
	{
		_previousClose = close;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}
}
