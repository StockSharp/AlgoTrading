using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility Arbitrage Spread Oscillator Model (VASOM).
/// Uses RSI on the spread between two securities to detect mean reversion opportunities.
/// </summary>
public class VolatilityArbitrageSpreadOscillatorModelStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _longThreshold;
	private readonly StrategyParam<int> _exitThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _secondSecurity;

	private decimal _frontClose;
	private decimal _secondClose;
	private decimal _rsiVal;
	private decimal _prevRsi;
	private int _cooldown;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int LongThreshold { get => _longThreshold.Value; set => _longThreshold.Value = value; }
	public int ExitThreshold { get => _exitThreshold.Value; set => _exitThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public Security SecondSecurity { get => _secondSecurity.Value; set => _secondSecurity.Value = value; }

	public VolatilityArbitrageSpreadOscillatorModelStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of RSI", "Parameters");

		_longThreshold = Param(nameof(LongThreshold), 35)
			.SetRange(0, 100)
			.SetDisplay("Long Threshold", "RSI level to enter long", "Parameters");

		_exitThreshold = Param(nameof(ExitThreshold), 65)
			.SetRange(0, 100)
			.SetDisplay("Exit Threshold", "RSI level to exit", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");

		_secondSecurity = Param<Security>(nameof(SecondSecurity))
			.SetDisplay("Second Security", "Second security for spread", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var list = new List<(Security sec, DataType dt)> { (Security, CandleType) };
		if (SecondSecurity != null)
			list.Add((SecondSecurity, CandleType));
		return list;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_frontClose = 0;
		_secondClose = 0;
		_rsiVal = 0;
		_prevRsi = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		_frontClose = 0;
		_secondClose = 0;
		_rsiVal = 0;
		_prevRsi = 0;
		_cooldown = 0;

		var frontSub = SubscribeCandles(CandleType);

		frontSub
			.Bind(rsi, ProcessFront)
			.Start();

		if (SecondSecurity != null)
		{
			var secondSub = SubscribeCandles(CandleType, security: SecondSecurity);
			secondSub.Bind(ProcessSecond).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, frontSub);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessSecond(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_secondClose = candle.ClosePrice;
	}

	private void ProcessFront(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_frontClose = candle.ClosePrice;
		_prevRsi = _rsiVal;
		_rsiVal = rsiValue;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		if (_prevRsi == 0)
			return;

		// Long entry when RSI crosses below threshold
		if (_rsiVal < LongThreshold && Position <= 0)
		{
			BuyMarket();
			_cooldown = 45;
		}
		// Exit long / short entry when RSI crosses above threshold
		else if (_rsiVal > ExitThreshold && Position >= 0)
		{
			SellMarket();
			_cooldown = 45;
		}
	}
}
