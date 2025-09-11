using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy entering above yesterday's high with optional ROC and trailing stop filters.
/// </summary>
public class YesterdaysHighStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gap;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _useRocFilter;
	private readonly StrategyParam<decimal> _rocThreshold;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<decimal> _trailEnter;
	private readonly StrategyParam<decimal> _trailOffset;
	private readonly StrategyParam<bool> _closeOnEma;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _currentHigh;
	private decimal _prevClose;
	private DateTime _sessionDate;
	private decimal _lastClose;

	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _trailActive;
	private decimal _trailActivationPrice;
	private decimal _trailHighest;
	private decimal _trailStopPrice;
	private bool _entryInitialized;

	private readonly ExponentialMovingAverage _ema = new();

	/// <summary>
	/// Gap percent for entry price above previous high.
	/// </summary>
	public decimal Gap
	{
		get => _gap.Value;
		set => _gap.Value = value;
	}

	/// <summary>
	/// Stop-loss percent from entry price.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take-profit percent from entry price.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Enable rate of change filter.
	/// </summary>
	public bool UseRocFilter
	{
		get => _useRocFilter.Value;
		set => _useRocFilter.Value = value;
	}

	/// <summary>
	/// ROC threshold percent.
	/// </summary>
	public decimal RocThreshold
	{
		get => _rocThreshold.Value;
		set => _rocThreshold.Value = value;
	}

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Activation percent for trailing stop.
	/// </summary>
	public decimal TrailEnter
	{
		get => _trailEnter.Value;
		set => _trailEnter.Value = value;
	}

	/// <summary>
	/// Offset percent for trailing stop.
	/// </summary>
	public decimal TrailOffset
	{
		get => _trailOffset.Value;
		set => _trailOffset.Value = value;
	}

	/// <summary>
	/// Close position when price crosses below EMA.
	/// </summary>
	public bool CloseOnEma
	{
		get => _closeOnEma.Value;
		set => _closeOnEma.Value = value;
	}

	/// <summary>
	/// EMA period length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="YesterdaysHighStrategy"/>.
	/// </summary>
	public YesterdaysHighStrategy()
	{
		_gap = Param(nameof(Gap), 1m)
			.SetDisplay("Gap%", "Entry gap percent", "Entry");
		_stopLoss = Param(nameof(StopLoss), 3m)
			.SetDisplay("Stop-loss", "Stop-loss percent", "Entry");
		_takeProfit = Param(nameof(TakeProfit), 9m)
			.SetDisplay("Take-profit", "Take-profit percent", "Entry");
		_useRocFilter = Param(nameof(UseRocFilter), false)
			.SetDisplay("ROC Filter", "Enable ROC filter", "Filters");
		_rocThreshold = Param(nameof(RocThreshold), 1m)
			.SetDisplay("Treshold", "ROC threshold", "Filters");
		_useTrailing = Param(nameof(UseTrailing), true)
			.SetDisplay("Trailing-stop", "Enable trailing stop", "Trailing");
		_trailEnter = Param(nameof(TrailEnter), 2m)
			.SetDisplay("Trailing-stop", "Activation percent", "Trailing");
		_trailOffset = Param(nameof(TrailOffset), 1m)
			.SetDisplay("Offset Trailing", "Offset percent", "Trailing");
		_closeOnEma = Param(nameof(CloseOnEma), false)
			.SetDisplay("Close EMA", "Close on EMA cross", "Trailing Stop Settings");
		_emaLength = Param(nameof(EmaLength), 10)
			.SetDisplay("EMA length", "EMA length", "Trailing Stop Settings");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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
		_prevHigh = 0;
		_currentHigh = 0;
		_prevClose = 0;
		_lastClose = 0;
		_sessionDate = default;
		_stopPrice = 0;
		_takePrice = 0;
		_trailActive = false;
		_trailActivationPrice = 0;
		_trailHighest = 0;
		_trailStopPrice = 0;
		_entryInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		_ema.Length = EmaLength;
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateSession(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;

		if (Position <= 0)
		{
			_entryInitialized = false;
			if (ShouldEnter(price))
			{
				CancelActiveOrders();
				var activation = _prevHigh * (1 + Gap / 100m);
				BuyStop(Volume + Math.Abs(Position), activation);
			}
		}
		else
		{
			ManagePosition(price, ema);
		}
	}

	private void UpdateSession(ICandleMessage candle)
	{
		var date = candle.OpenTime.Date;
		if (_sessionDate == default)
		{
			_sessionDate = date;
			_currentHigh = candle.HighPrice;
			_prevClose = candle.ClosePrice;
		}
		else if (date > _sessionDate)
		{
			_prevHigh = _currentHigh;
			_prevClose = _lastClose;
			_currentHigh = candle.HighPrice;
			_sessionDate = date;
		}
		else
		{
			if (candle.HighPrice > _currentHigh)
				_currentHigh = candle.HighPrice;
		}
		_lastClose = candle.ClosePrice;
	}

	private bool ShouldEnter(decimal price)
	{
		if (_prevHigh == 0)
			return false;
		if (UseRocFilter && _prevClose > 0)
		{
			var roc = (price - _prevClose) / _prevClose * 100m;
			if (roc <= RocThreshold)
				return false;
		}
		return price < _prevHigh;
	}

	private void ManagePosition(decimal price, decimal ema)
	{
		if (!_entryInitialized)
		{
			var entry = PositionPrice;
			_stopPrice = entry * (1 - StopLoss / 100m);
			_takePrice = entry * (1 + TakeProfit / 100m);
			_trailActive = false;
			_trailActivationPrice = entry * (1 + TrailEnter / 100m);
			_trailHighest = 0;
			_entryInitialized = true;
		}

		if (CloseOnEma && price < ema)
		{
			SellMarket(Position);
			return;
		}

		if (UseTrailing)
		{
			if (!_trailActive && price > _trailActivationPrice)
			{
				_trailActive = true;
				_trailHighest = price;
			}
			if (_trailActive)
			{
				_trailHighest = Math.Max(_trailHighest, price);
				_trailStopPrice = _trailHighest * (1 - TrailOffset / 100m);
				if (price < _trailStopPrice)
				{
					SellMarket(Position);
					return;
				}
			}
		}

		if (price < _stopPrice)
		{
			SellMarket(Position);
			return;
		}
		if (price > _takePrice)
		{
			SellMarket(Position);
		}
	}
}
