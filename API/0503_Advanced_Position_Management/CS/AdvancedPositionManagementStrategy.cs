using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with multiple take profit levels and optional trailing stop.
/// </summary>
public class AdvancedPositionManagementStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _tp1Percent;
	private readonly StrategyParam<int> _tp1PositionPercent;
	private readonly StrategyParam<decimal> _tp2Percent;
	private readonly StrategyParam<int> _tp2PositionPercent;
	private readonly StrategyParam<bool> _useTp2;
	private readonly StrategyParam<decimal> _tp3Percent;
	private readonly StrategyParam<int> _tp3PositionPercent;
	private readonly StrategyParam<bool> _useTp3;
	private readonly StrategyParam<decimal> _initialStopLossPercent;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<bool> _trailingStopPercentType;
	private readonly StrategyParam<decimal> _trailingStopPercent;
	private readonly StrategyParam<decimal> _trailingStopActivationPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;

	private EMA _fastEma;
	private EMA _slowEma;
	private bool _wasFastBelow;
	private bool _isInitialized;
	private decimal _entryPrice;
	private decimal _entryVolume;
	private decimal _currentStopLoss;
	private decimal _tp1Price;
	private decimal _tp2Price;
	private decimal _tp3Price;
	private bool _trailingStopActive;
	private int _lastTpHit;

	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}
	public decimal Tp1Percent
	{
		get => _tp1Percent.Value;
		set => _tp1Percent.Value = value;
	}
	public int Tp1PositionPercent
	{
		get => _tp1PositionPercent.Value;
		set => _tp1PositionPercent.Value = value;
	}
	public decimal Tp2Percent
	{
		get => _tp2Percent.Value;
		set => _tp2Percent.Value = value;
	}
	public int Tp2PositionPercent
	{
		get => _tp2PositionPercent.Value;
		set => _tp2PositionPercent.Value = value;
	}
	public bool UseTp2
	{
		get => _useTp2.Value;
		set => _useTp2.Value = value;
	}
	public decimal Tp3Percent
	{
		get => _tp3Percent.Value;
		set => _tp3Percent.Value = value;
	}
	public int Tp3PositionPercent
	{
		get => _tp3PositionPercent.Value;
		set => _tp3PositionPercent.Value = value;
	}
	public bool UseTp3
	{
		get => _useTp3.Value;
		set => _useTp3.Value = value;
	}
	public decimal InitialStopLossPercent
	{
		get => _initialStopLossPercent.Value;
		set => _initialStopLossPercent.Value = value;
	}
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}
	public bool TrailingStopPercentType
	{
		get => _trailingStopPercentType.Value;
		set => _trailingStopPercentType.Value = value;
	}
	public decimal TrailingStopPercent
	{
		get => _trailingStopPercent.Value;
		set => _trailingStopPercent.Value = value;
	}
	public decimal TrailingStopActivationPercent
	{
		get => _trailingStopActivationPercent.Value;
		set => _trailingStopActivationPercent.Value = value;
	}
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	public bool AllowLong
	{
		get => _allowLong.Value;
		set => _allowLong.Value = value;
	}
	public bool AllowShort
	{
		get => _allowShort.Value;
		set => _allowShort.Value = value;
	}

	public AdvancedPositionManagementStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
						  .SetDisplay("Fast EMA Length", "Period of the fast EMA", "General")
						  .SetCanOptimize(true)
						  .SetOptimize(5, 20, 5);

		_slowLength = Param(nameof(SlowLength), 20)
						  .SetDisplay("Slow EMA Length", "Period of the slow EMA", "General")
						  .SetCanOptimize(true)
						  .SetOptimize(10, 40, 5);

		_tp1Percent = Param(nameof(Tp1Percent), 2m)
						  .SetDisplay("Take Profit 1 %", "Percentage for first take profit", "Position management");

		_tp1PositionPercent =
			Param(nameof(Tp1PositionPercent), 30)
				.SetDisplay("TP1 Quantity %", "Quantity percent for first take profit", "Position management");

		_tp2Percent = Param(nameof(Tp2Percent), 4m)
						  .SetDisplay("Take Profit 2 %", "Percentage for second take profit", "Position management");

		_tp2PositionPercent =
			Param(nameof(Tp2PositionPercent), 30)
				.SetDisplay("TP2 Quantity %", "Quantity percent for second take profit", "Position management");

		_useTp2 = Param(nameof(UseTp2), true).SetDisplay("Use TP2", "Enable second take profit", "Position management");

		_tp3Percent = Param(nameof(Tp3Percent), 6m)
						  .SetDisplay("Take Profit 3 %", "Percentage for third take profit", "Position management");

		_tp3PositionPercent =
			Param(nameof(Tp3PositionPercent), 40)
				.SetDisplay("TP3 Quantity %", "Quantity percent for third take profit", "Position management");

		_useTp3 = Param(nameof(UseTp3), true).SetDisplay("Use TP3", "Enable third take profit", "Position management");

		_initialStopLossPercent = Param(nameof(InitialStopLossPercent), 2m)
									  .SetDisplay("Initial Stop Loss %", "Initial stop loss percent", "Risk");

		_useTrailingStop =
			Param(nameof(UseTrailingStop), false).SetDisplay("Use Trailing Stop", "Enable trailing stop", "Risk");

		_trailingStopPercentType =
			Param(nameof(TrailingStopPercentType), true)
				.SetDisplay("Trailing Stop Is Percent", "True - percent, False - previous TP", "Risk");

		_trailingStopPercent =
			Param(nameof(TrailingStopPercent), 3m).SetDisplay("Trailing Stop %", "Trailing stop percentage", "Risk");

		_trailingStopActivationPercent =
			Param(nameof(TrailingStopActivationPercent), 3m)
				.SetDisplay("Trailing Activation %", "Profit percent to activate trailing", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles", "General");

		_allowLong = Param(nameof(AllowLong), true).SetDisplay("Allow Long", "Enable long trades", "General");

		_allowShort = Param(nameof(AllowShort), true).SetDisplay("Allow Short", "Enable short trades", "General");
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
		_wasFastBelow = false;
		_isInitialized = false;
		_entryPrice = 0m;
		_entryVolume = 0m;
		_currentStopLoss = 0m;
		_tp1Price = 0m;
		_tp2Price = 0m;
		_tp3Price = 0m;
		_trailingStopActive = false;
		_lastTpHit = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma = new EMA { Length = FastLength };
		_slowEma = new EMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_fastEma, _slowEma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized && _fastEma.IsFormed && _slowEma.IsFormed)
		{
			_wasFastBelow = fast < slow;
			_isInitialized = true;
			return;
		}

		if (!_isInitialized)
			return;

		var fastBelow = fast < slow;

		if (_wasFastBelow && !fastBelow && Position <= 0 && AllowLong)
		{
			EnterLong(candle.ClosePrice);
		}
		else if (!_wasFastBelow && fastBelow && Position >= 0 && AllowShort)
		{
			EnterShort(candle.ClosePrice);
		}

		ManagePosition(candle);

		_wasFastBelow = fastBelow;
	}

	private void EnterLong(decimal price)
	{
		_entryVolume = Volume + Math.Abs(Position);
		_entryPrice = price;
		_currentStopLoss = price * (1m - InitialStopLossPercent / 100m);
		_tp1Price = price * (1m + Tp1Percent / 100m);
		_tp2Price = price * (1m + Tp2Percent / 100m);
		_tp3Price = price * (1m + Tp3Percent / 100m);
		_trailingStopActive = false;
		_lastTpHit = 0;
		BuyMarket(_entryVolume);
	}

	private void EnterShort(decimal price)
	{
		_entryVolume = Volume + Math.Abs(Position);
		_entryPrice = price;
		_currentStopLoss = price * (1m + InitialStopLossPercent / 100m);
		_tp1Price = price * (1m - Tp1Percent / 100m);
		_tp2Price = price * (1m - Tp2Percent / 100m);
		_tp3Price = price * (1m - Tp3Percent / 100m);
		_trailingStopActive = false;
		_lastTpHit = 0;
		SellMarket(_entryVolume);
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (candle.LowPrice <= _currentStopLoss)
			{
				SellMarket(Position);
				ResetPositionState();
				return;
			}

			if (_lastTpHit < 1 && candle.HighPrice >= _tp1Price)
			{
				var qty = _entryVolume * Tp1PositionPercent / 100m;
				SellMarket(qty);
				_lastTpHit = 1;
			}

			if (_lastTpHit < 2 && UseTp2 && candle.HighPrice >= _tp2Price)
			{
				var qty = _entryVolume * Tp2PositionPercent / 100m;
				SellMarket(qty);
				_lastTpHit = 2;
			}

			if (_lastTpHit < 3 && UseTp3 && candle.HighPrice >= _tp3Price)
			{
				var qty = _entryVolume * Tp3PositionPercent / 100m;
				SellMarket(qty);
				_lastTpHit = 3;
			}

			if (UseTrailingStop)
			{
				var currentPrice = candle.HighPrice;
				var profitPercent = (currentPrice - _entryPrice) / _entryPrice * 100m;

				if (profitPercent >= TrailingStopActivationPercent && !_trailingStopActive)
					_trailingStopActive = true;

				if (_trailingStopActive)
				{
					if (TrailingStopPercentType)
					{
						var newStop = currentPrice * (1m - TrailingStopPercent / 100m);
						if (newStop > _currentStopLoss)
							_currentStopLoss = newStop;
					}
					else
					{
						if (currentPrice >= _tp3Price && _lastTpHit < 3)
						{
							_currentStopLoss = _tp2Price;
							_lastTpHit = 3;
						}
						else if (currentPrice >= _tp2Price && _lastTpHit < 2)
						{
							_currentStopLoss = _tp1Price;
							_lastTpHit = 2;
						}
						else if (currentPrice >= _tp1Price && _lastTpHit < 1)
						{
							_currentStopLoss = _entryPrice;
							_lastTpHit = 1;
						}
					}
				}
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _currentStopLoss)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return;
			}

			if (_lastTpHit < 1 && candle.LowPrice <= _tp1Price)
			{
				var qty = _entryVolume * Tp1PositionPercent / 100m;
				BuyMarket(qty);
				_lastTpHit = 1;
			}

			if (_lastTpHit < 2 && UseTp2 && candle.LowPrice <= _tp2Price)
			{
				var qty = _entryVolume * Tp2PositionPercent / 100m;
				BuyMarket(qty);
				_lastTpHit = 2;
			}

			if (_lastTpHit < 3 && UseTp3 && candle.LowPrice <= _tp3Price)
			{
				var qty = _entryVolume * Tp3PositionPercent / 100m;
				BuyMarket(qty);
				_lastTpHit = 3;
			}

			if (UseTrailingStop)
			{
				var currentPrice = candle.LowPrice;
				var profitPercent = (_entryPrice - currentPrice) / _entryPrice * 100m;

				if (profitPercent >= TrailingStopActivationPercent && !_trailingStopActive)
					_trailingStopActive = true;

				if (_trailingStopActive)
				{
					if (TrailingStopPercentType)
					{
						var newStop = currentPrice * (1m + TrailingStopPercent / 100m);
						if (newStop < _currentStopLoss)
							_currentStopLoss = newStop;
					}
					else
					{
						if (currentPrice <= _tp3Price && _lastTpHit < 3)
						{
							_currentStopLoss = _tp2Price;
							_lastTpHit = 3;
						}
						else if (currentPrice <= _tp2Price && _lastTpHit < 2)
						{
							_currentStopLoss = _tp1Price;
							_lastTpHit = 2;
						}
						else if (currentPrice <= _tp1Price && _lastTpHit < 1)
						{
							_currentStopLoss = _entryPrice;
							_lastTpHit = 1;
						}
					}
				}
			}
		}
		else
		{
			_trailingStopActive = false;
			_lastTpHit = 0;
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_entryVolume = 0m;
		_currentStopLoss = 0m;
		_tp1Price = 0m;
		_tp2Price = 0m;
		_tp3Price = 0m;
		_trailingStopActive = false;
		_lastTpHit = 0;
	}
}
