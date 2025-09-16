using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy derived from the Eugene expert advisor.
/// </summary>
public class EugeneInsideBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _activationHour;

	private decimal _prevOpen1;
	private decimal _prevHigh1;
	private decimal _prevLow1;

	private decimal _prevOpen2;
	private decimal _prevHigh2;
	private decimal _prevLow2;

	private bool _hasPrev1;
	private bool _hasPrev2;

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Hour of day after which confirmations are automatically valid.
	/// </summary>
	public int ActivationHour
	{
		get => _activationHour.Value;
		set => _activationHour.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="EugeneInsideBreakoutStrategy"/>.
	/// </summary>
	public EugeneInsideBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");

		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_activationHour = Param(nameof(ActivationHour), 8)
			.SetRange(0, 23)
			.SetDisplay("Activation Hour", "Hour when confirmations become unconditional", "Filters");

		ResetHistory();
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

		ResetHistory();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

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
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateHistory(candle);
			return;
		}

		if (!_hasPrev2)
		{
			UpdateHistory(candle);
			return;
		}

		var open1 = _prevOpen1;
		var open2 = _prevOpen2;
		var high0 = candle.HighPrice;
		var high1 = _prevHigh1;
		var high2 = _prevHigh2;
		var low0 = candle.LowPrice;
		var low1 = _prevLow1;
		var low2 = _prevLow2;

		// Replicate the original expert advisor checks for inside bars.
		var blackInsider = high1 <= high2 && low1 >= low2 && open1 <= open1;
		var whiteInsider = high1 <= high2 && low1 >= low2 && open1 > open1;
		var whiteBird = whiteInsider && open2 > open2;
		var blackBird = blackInsider && open2 < open2;

		// ZigZag style confirmation levels based on the previous candle body.
		var zigLevelBuy = open1 < open1
			? open1 - (open1 - open1) / 3m
			: open1 - (open1 - low1) / 3m;

		var zigLevelSell = open1 > open1
			? open1 + (open1 - open1) / 3m
			: open1 + (high1 - open1) / 3m;

		var confirmBuy = (low0 <= zigLevelBuy || candle.CloseTime.Hour >= ActivationHour) && !blackBird && !whiteInsider;
		var confirmSell = (high0 >= zigLevelSell || candle.CloseTime.Hour >= ActivationHour) && !whiteBird && !blackInsider;

		var buySignal = high0 > high1;
		var sellSignal = low0 < low1;

		if (Position == 0)
		{
			if (buySignal && confirmBuy && low0 > low1 && low1 < high2)
			{
				BuyMarket(Volume);
			}
			else if (sellSignal && confirmSell && high0 < high1)
			{
				SellMarket(Volume);
			}
		}
		else if (Position > 0)
		{
			if (sellSignal && confirmSell && high0 < high1)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (buySignal && confirmBuy && low0 > low1 && low1 < high2)
				BuyMarket(-Position);
		}

		UpdateHistory(candle);
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		// Keep the two most recent completed candles for decision making.
		_prevOpen2 = _prevOpen1;
		_prevHigh2 = _prevHigh1;
		_prevLow2 = _prevLow1;
		_hasPrev2 = _hasPrev1;

		_prevOpen1 = candle.OpenPrice;
		_prevHigh1 = candle.HighPrice;
		_prevLow1 = candle.LowPrice;
		_hasPrev1 = true;
	}

	private void ResetHistory()
	{
		_prevOpen1 = default;
		_prevHigh1 = default;
		_prevLow1 = default;
		_prevOpen2 = default;
		_prevHigh2 = default;
		_prevLow2 = default;
		_hasPrev1 = false;
		_hasPrev2 = false;
	}
}
