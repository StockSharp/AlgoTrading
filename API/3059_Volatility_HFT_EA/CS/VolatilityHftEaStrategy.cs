using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean-reversion expert advisor that buys volatility spikes when price stretches far above a fast moving average.
/// Converted from the MetaTrader 5 "Volatility HFT EA" script.
/// </summary>
public class VolatilityHftEaStrategy : Strategy
{
	private const int MinimumBars = 60;

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _maDifferencePips;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastMa = null!;

	private decimal _pipSize = 1m;
	private decimal? _previousSma;
	private decimal? _smaTwoBarsAgo;
	private int _processedCandles;

	private decimal _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	public VolatilityHftEaStrategy()
	{
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

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		Volume = OrderVolume;

		_fastMa = new SimpleMovingAverage
		{
			Length = FastMaLength
		};

		_previousSma = null;
		_smaTwoBarsAgo = null;
		_processedCandles = 0;
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

		var threshold = MaDifferencePips * _pipSize;

		if (_smaTwoBarsAgo.HasValue)
		{
			var distance = candle.ClosePrice - smaValue;
			var isBreakout = distance >= threshold;
			var isSlopePositive = smaValue > _smaTwoBarsAgo.Value;

			if (isBreakout && isSlopePositive && Position == 0)
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
				ResetPositionState();
				return;
			}

			if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
			{
				SellMarket(exitVolume);
				ResetPositionState();
			}
		}
		else if (Position < 0)
		{
			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				BuyMarket(exitVolume);
				ResetPositionState();
				return;
			}

			if (_stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value)
			{
				BuyMarket(exitVolume);
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
