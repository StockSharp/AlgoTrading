using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using System.Globalization;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean-reversion expert advisor that buys volatility spikes when price stretches far above a fast moving average.
/// Converted from the MetaTrader 5 "Volatility HFT EA" script.
/// </summary>
public class VolatilityHftEaStrategy : Strategy
{
	private readonly StrategyParam<int> _minimumBars;

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _maDifferencePips;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastMa = null!;

	private decimal _pipSize = 1m;
	private decimal? _previousSma;
	private decimal? _smaTwoBarsAgo;
	private int _processedCandles;
	private int _cooldownLeft;

	private decimal _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	public VolatilityHftEaStrategy()
	{
		_minimumBars = Param(nameof(MinimumBars), 60)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Bars", "Minimum completed candles before signal evaluation", "Signal");

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume applied to market orders", "Trading");

		_fastMaLength = Param(nameof(FastMaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Period of the fast simple moving average", "Signal");

		_stopLossPips = Param(nameof(StopLossPips), 15m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk");

		_maDifferencePips = Param(nameof(MaDifferencePips), 15m)
			.SetGreaterThanZero()
			.SetDisplay("MA Difference (pips)", "Minimum distance between price and the moving average", "Signal");

		_cooldownBars = Param(nameof(CooldownBars), 24)
			.SetNotNegative()
			.SetDisplay("Cooldown Bars", "Bars to wait after entry or exit", "Signal");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for signal detection", "General");
	}

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public decimal MaDifferencePips
	{
		get => _maDifferencePips.Value;
		set => _maDifferencePips.Value = value;
	}

	public int MinimumBars
	{
		get => _minimumBars.Value;
		set => _minimumBars.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_fastMa = null!;
		_previousSma = null;
		_smaTwoBarsAgo = null;
		_processedCandles = 0;
		_cooldownLeft = 0;
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_pipSize = CalculatePipSize();
		Volume = OrderVolume;

		_fastMa = new SMA
		{
			Length = FastMaLength
		};

		_previousSma = null;
		_smaTwoBarsAgo = null;
		_processedCandles = 0;
		_cooldownLeft = 0;
		ResetPositionState();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageActivePosition(candle);

		if (_cooldownLeft > 0)
			_cooldownLeft--;

		if (!_fastMa.IsFormed)
		{
			UpdateSmaHistory(smaValue);
			_processedCandles++;
			return;
		}

		if (_processedCandles < MinimumBars)
		{
			UpdateSmaHistory(smaValue);
			_processedCandles++;
			return;
		}

		var threshold = Math.Max(MaDifferencePips, 10m) * _pipSize;

		if (_smaTwoBarsAgo.HasValue && _cooldownLeft == 0)
		{
			var distance = candle.ClosePrice - smaValue;
			var isBreakout = distance >= threshold;
			var isSlopePositive = _previousSma.HasValue && _previousSma.Value > _smaTwoBarsAgo.Value && smaValue > _previousSma.Value;
			var isBullishBar = candle.ClosePrice > candle.OpenPrice;

			if (isBreakout && isSlopePositive && isBullishBar && Position == 0)
			{
				EnterLong(candle, smaValue);
			}
		}

		UpdateSmaHistory(smaValue);
		_processedCandles++;
	}

	private void EnterLong(ICandleMessage candle, decimal smaValue)
	{
		// Strategy holds only one long position at a time.
		if (Position != 0)
			return;

		Volume = OrderVolume;
		BuyMarket();
		_cooldownLeft = CooldownBars;

		_entryPrice = candle.ClosePrice;

		var stopDistance = StopLossPips * _pipSize;
		_stopLossPrice = stopDistance > 0m ? _entryPrice - stopDistance : null;
		_takeProfitPrice = smaValue;
	}

	private void ManageActivePosition(ICandleMessage candle)
	{
		if (Position == 0)
		{
			ResetPositionState();
			return;
		}

		var exitVolume = Math.Abs(Position);

		if (Position > 0)
		{
			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				SellMarket(exitVolume);
				_cooldownLeft = CooldownBars;
				ResetPositionState();
				return;
			}

			if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
			{
				SellMarket(exitVolume);
				_cooldownLeft = CooldownBars;
				ResetPositionState();
			}
		}
		else if (Position < 0)
		{
			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				BuyMarket(exitVolume);
				_cooldownLeft = CooldownBars;
				ResetPositionState();
				return;
			}

			if (_stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value)
			{
				BuyMarket(exitVolume);
				_cooldownLeft = CooldownBars;
				ResetPositionState();
			}
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	private void UpdateSmaHistory(decimal smaValue)
	{
		_smaTwoBarsAgo = _previousSma;
		_previousSma = smaValue;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;

		if (step <= 0m)
			return 1m;

		var decimals = GetDecimalPlaces(step);

		return decimals is 3 or 5
			? step * 10m
			: step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var text = Math.Abs(value).ToString(CultureInfo.InvariantCulture);
		var separatorIndex = text.IndexOf('.');

		return separatorIndex < 0 ? 0 : text.Length - separatorIndex - 1;
	}
}

