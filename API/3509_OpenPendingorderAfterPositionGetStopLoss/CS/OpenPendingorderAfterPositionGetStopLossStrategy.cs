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
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _lastK;
	private decimal? _prevK;
	private decimal _entryPrice;

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

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for indicator", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stochastic = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochasticValue.IsFinal)
			return;

		var stoch = (StochasticOscillatorValue)stochasticValue;
		if (stoch.K is not decimal currentK)
			return;

		var close = candle.ClosePrice;

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

		// Need at least 2 values to determine slope
		if (_lastK is not decimal prevK)
		{
			_lastK = currentK;
			return;
		}

		_prevK = _lastK;
		_lastK = currentK;

		// Rising %K -> go long
		if (currentK > prevK && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(); // close short

			BuyMarket();
			_entryPrice = close;
		}
		// Falling %K -> go short
		else if (currentK < prevK && Position >= 0)
		{
			if (Position > 0)
				SellMarket(); // close long

			SellMarket();
			_entryPrice = close;
		}
	}
}
