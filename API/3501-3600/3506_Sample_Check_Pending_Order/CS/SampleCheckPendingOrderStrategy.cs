namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Sample Check Pending Order strategy: CCI trend following.
/// Buys when CCI crosses above zero, sells when CCI crosses below zero.
/// </summary>
public class SampleCheckPendingOrderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _cciLevel;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _prevCci;
	private int _candlesSinceTrade;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int Period { get => _period.Value; set => _period.Value = value; }
	public decimal CciLevel { get => _cciLevel.Value; set => _cciLevel.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public SampleCheckPendingOrderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_period = Param(nameof(Period), 30)
			.SetGreaterThanZero()
			.SetDisplay("Period", "CCI period", "Indicators");
		_cciLevel = Param(nameof(CciLevel), 100m)
			.SetDisplay("CCI Level", "CCI crossover threshold", "Signals");
		_signalCooldownCandles = Param(nameof(SignalCooldownCandles), 4)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevCci = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevCci = 0;
		_candlesSinceTrade = SignalCooldownCandles;
		_hasPrev = false;
		var cci = new CommodityChannelIndex { Length = Period };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(cci, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (_candlesSinceTrade < SignalCooldownCandles)
			_candlesSinceTrade++;

		if (_hasPrev)
		{
			if (_prevCci <= -CciLevel && cciValue > -CciLevel && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (_prevCci >= CciLevel && cciValue < CciLevel && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevCci = cciValue;
		_hasPrev = true;
	}
}
