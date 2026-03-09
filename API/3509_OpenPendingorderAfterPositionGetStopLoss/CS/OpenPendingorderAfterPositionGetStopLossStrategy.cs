namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Trades based on the slope of the Stochastic %K line.
/// Buys when %K is rising, sells when %K is falling.
/// Uses take-profit and stop-loss protection.
/// </summary>
public class OpenPendingorderAfterPositionGetStopLossStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<int> _signalCooldownCandles;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _lastK;
	private decimal? _prevK;
	private decimal _entryPrice;
	private int _candlesSinceTrade;

	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	public decimal StopLossPct
	{
		get => _stopLossPct.Value;
		set => _stopLossPct.Value = value;
	}

	public decimal TakeProfitPct
	{
		get => _takeProfitPct.Value;
		set => _takeProfitPct.Value = value;
	}

	public int SignalCooldownCandles
	{
		get => _signalCooldownCandles.Value;
		set => _signalCooldownCandles.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public OpenPendingorderAfterPositionGetStopLossStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 22)
			.SetDisplay("%K Period", "Number of bars for %K", "Indicators");

		_dPeriod = Param(nameof(DPeriod), 7)
			.SetDisplay("%D Period", "Smoothing period for %K", "Indicators");

		_slowing = Param(nameof(Slowing), 2)
			.SetDisplay("Slowing", "Additional smoothing factor", "Indicators");

		_stopLossPct = Param(nameof(StopLossPct), 2m)
			.SetDisplay("Stop Loss %", "Stop-loss as percentage of entry price", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 3m)
			.SetDisplay("Take Profit %", "Take-profit as percentage of entry price", "Risk");

		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 4)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between entries", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_lastK = null;
		_prevK = null;
		_entryPrice = 0;
		_candlesSinceTrade = SignalCooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_lastK = null;
		_prevK = null;
		_entryPrice = 0;
		_candlesSinceTrade = SignalCooldownCandles;

		var rsi = new RelativeStrengthIndex { Length = KPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal currentK)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		// Check stop-loss / take-profit on existing position
		if (Position != 0 && _entryPrice > 0)
		{
			if (Position > 0)
			{
				var pnlPct = (close - _entryPrice) / _entryPrice * 100m;
				if (pnlPct <= -StopLossPct || pnlPct >= TakeProfitPct)
				{
					SellMarket();
					_entryPrice = 0;
					_prevK = currentK;
					_lastK = currentK;
					return;
				}
			}
			else if (Position < 0)
			{
				var pnlPct = (_entryPrice - close) / _entryPrice * 100m;
				if (pnlPct <= -StopLossPct || pnlPct >= TakeProfitPct)
				{
					BuyMarket();
					_entryPrice = 0;
					_prevK = currentK;
					_lastK = currentK;
					return;
				}
			}
		}

		// Need at least 2 values to determine signal transition.
		if (_lastK is not decimal prevK)
		{
			_lastK = currentK;
			return;
		}

		_prevK = _lastK;
		_lastK = currentK;

		var crossedUp = prevK <= 45m && currentK > 45m;
		var crossedDown = prevK >= 55m && currentK < 55m;

		if (crossedUp && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
		{
			BuyMarket();
			_entryPrice = close;
			_candlesSinceTrade = 0;
		}
		else if (crossedDown && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
		{
			SellMarket();
			_entryPrice = close;
			_candlesSinceTrade = 0;
		}
	}
}
