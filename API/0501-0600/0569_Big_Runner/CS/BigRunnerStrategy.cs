using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Big Runner strategy.
/// Buys when on the same candle the close crosses above the fast SMA and the fast SMA crosses above the slow SMA, and sells on the
/// mirrored crosses; an opposite signal reverses the position. The order size is PercentOfPortfolio of the portfolio value times
/// Leverage divided by the price. Separate long and short percent stop losses and take profits are measured from the entry close
/// (0 disables a level).
/// </summary>
public class BigRunnerStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _takeProfitLongPercent;
	private readonly StrategyParam<decimal> _takeProfitShortPercent;
	private readonly StrategyParam<decimal> _stopLossLongPercent;
	private readonly StrategyParam<decimal> _stopLossShortPercent;
	private readonly StrategyParam<decimal> _percentOfPortfolio;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal _entryPrice;

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Long take profit in percent.
	/// </summary>
	public decimal TakeProfitLongPercent
	{
		get => _takeProfitLongPercent.Value;
		set => _takeProfitLongPercent.Value = value;
	}

	/// <summary>
	/// Short take profit in percent.
	/// </summary>
	public decimal TakeProfitShortPercent
	{
		get => _takeProfitShortPercent.Value;
		set => _takeProfitShortPercent.Value = value;
	}

	/// <summary>
	/// Long stop loss in percent.
	/// </summary>
	public decimal StopLossLongPercent
	{
		get => _stopLossLongPercent.Value;
		set => _stopLossLongPercent.Value = value;
	}

	/// <summary>
	/// Short stop loss in percent.
	/// </summary>
	public decimal StopLossShortPercent
	{
		get => _stopLossShortPercent.Value;
		set => _stopLossShortPercent.Value = value;
	}

	/// <summary>
	/// Share of the portfolio value used per trade, in percent.
	/// </summary>
	public decimal PercentOfPortfolio
	{
		get => _percentOfPortfolio.Value;
		set => _percentOfPortfolio.Value = value;
	}

	/// <summary>
	/// Leverage applied to the position value.
	/// </summary>
	public decimal Leverage
	{
		get => _leverage.Value;
		set => _leverage.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BigRunnerStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast SMA period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow SMA period", "Indicators");

		_takeProfitLongPercent = Param(nameof(TakeProfitLongPercent), 4m)
			.SetNotNegative()
			.SetDisplay("Take Profit Long %", "Long take profit in percent", "Risk");

		_takeProfitShortPercent = Param(nameof(TakeProfitShortPercent), 7m)
			.SetNotNegative()
			.SetDisplay("Take Profit Short %", "Short take profit in percent", "Risk");

		_stopLossLongPercent = Param(nameof(StopLossLongPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Long %", "Long stop loss in percent", "Risk");

		_stopLossShortPercent = Param(nameof(StopLossShortPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Short %", "Short stop loss in percent", "Risk");

		_percentOfPortfolio = Param(nameof(PercentOfPortfolio), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Percent Of Portfolio", "Share of the portfolio value used per trade", "Money Management");

		_leverage = Param(nameof(Leverage), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Leverage", "Leverage applied to the position value", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fast = new SimpleMovingAverage { Length = FastLength };
		var slow = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ResetState()
	{
		_prevClose = null;
		_prevFast = null;
		_prevSlow = null;
		_entryPrice = 0;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		var prevFast = _prevFast;
		var prevSlow = _prevSlow;

		_prevClose = close;
		_prevFast = fast;
		_prevSlow = slow;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (CheckStops(candle))
			return;

		if (prevClose is not decimal pc || prevFast is not decimal pf || prevSlow is not decimal ps)
			return;

		var longSignal = pc <= pf && close > fast && pf <= ps && fast > slow;
		var shortSignal = pc >= pf && close < fast && pf >= ps && fast < slow;

		if (longSignal && Position <= 0)
		{
			BuyMarket(GetOrderVolume(close) + Math.Abs(Position));
			_entryPrice = close;
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(GetOrderVolume(close) + Math.Abs(Position));
			_entryPrice = close;
		}
	}

	private bool CheckStops(ICandleMessage candle)
	{
		if (_entryPrice <= 0)
			return false;

		if (Position > 0)
		{
			var stopHit = StopLossLongPercent > 0 && candle.LowPrice <= _entryPrice * (1m - StopLossLongPercent / 100m);
			var takeHit = TakeProfitLongPercent > 0 && candle.HighPrice >= _entryPrice * (1m + TakeProfitLongPercent / 100m);

			if (stopHit || takeHit)
			{
				SellMarket(Position);
				_entryPrice = 0;
				return true;
			}
		}
		else if (Position < 0)
		{
			var stopHit = StopLossShortPercent > 0 && candle.HighPrice >= _entryPrice * (1m + StopLossShortPercent / 100m);
			var takeHit = TakeProfitShortPercent > 0 && candle.LowPrice <= _entryPrice * (1m - TakeProfitShortPercent / 100m);

			if (stopHit || takeHit)
			{
				BuyMarket(-Position);
				_entryPrice = 0;
				return true;
			}
		}

		return false;
	}

	private decimal GetOrderVolume(decimal price)
	{
		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (equity <= 0 || price <= 0)
			return Volume;

		var volume = equity * PercentOfPortfolio / 100m * Leverage / price;

		if (Security?.VolumeStep is decimal step && step > 0)
			volume = Math.Floor(volume / step) * step;

		if (Security?.MaxVolume is decimal maxVolume && maxVolume > 0 && volume > maxVolume)
			volume = maxVolume;

		return volume > 0 ? volume : Volume;
	}
}
