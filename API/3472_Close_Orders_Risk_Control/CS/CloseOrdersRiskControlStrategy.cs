namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Close Orders Risk Control strategy: CCI crossover with risk management.
/// Buys when CCI crosses above zero, sells when crosses below zero.
/// </summary>
public class CloseOrdersRiskControlStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciLevel;
	private readonly StrategyParam<int> _signalCooldownCandles;

	private decimal _prevCci;
	private int _candlesSinceTrade;
	private bool _hasPrev;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public decimal CciLevel { get => _cciLevel.Value; set => _cciLevel.Value = value; }
	public int SignalCooldownCandles { get => _signalCooldownCandles.Value; set => _signalCooldownCandles.Value = value; }

	public CloseOrdersRiskControlStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_cciPeriod = Param(nameof(CciPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "CCI period", "Indicators");
		_cciLevel = Param(nameof(CciLevel), 100m)
			.SetDisplay("CCI Level", "CCI threshold for crossover", "Signals");
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
		var cci = new CommodityChannelIndex { Length = CciPeriod };
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
			if (_prevCci < -CciLevel && cciValue >= -CciLevel && Position <= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				BuyMarket();
				_candlesSinceTrade = 0;
			}
			else if (_prevCci > CciLevel && cciValue <= CciLevel && Position >= 0 && _candlesSinceTrade >= SignalCooldownCandles)
			{
				SellMarket();
				_candlesSinceTrade = 0;
			}
		}

		_prevCci = cciValue;
		_hasPrev = true;
	}
}
