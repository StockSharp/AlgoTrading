using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades VIX front month when the RSI of the spread between front and second month contracts is oversold.
/// Exits when RSI rises above the exit threshold.
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
	private RelativeStrengthIndex _rsi;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI level for long entry.
	/// </summary>
	public int LongThreshold
	{
		get => _longThreshold.Value;
		set => _longThreshold.Value = value;
	}

	/// <summary>
	/// RSI level for exit.
	/// </summary>
	public int ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
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
	/// Second month VIX future.
	/// </summary>
	public Security SecondSecurity
	{
		get => _secondSecurity.Value;
		set => _secondSecurity.Value = value;
	}

	public VolatilityArbitrageSpreadOscillatorModelStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 2)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Length of RSI", "Parameters");

		_longThreshold = Param(nameof(LongThreshold), 46)
			.SetRange(0, 100)
			.SetDisplay("Long Threshold", "RSI level to enter long", "Parameters");

		_exitThreshold = Param(nameof(ExitThreshold), 76)
			.SetRange(0, 100)
			.SetDisplay("Exit Threshold", "RSI level to exit", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");

		_secondSecurity = Param(nameof(SecondSecurity), new Security { Id = "CBOE:VX2!" })
			.SetDisplay("Second Security", "Second month VIX future", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (SecondSecurity, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_frontClose = 0m;
		_secondClose = 0m;
		_rsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var frontSub = SubscribeCandles(CandleType);
		frontSub.Bind(ProcessFront).Start();

		var secondSub = SubscribeCandles(CandleType, security: SecondSecurity);
		secondSub.Bind(ProcessSecond).Start();

		var area = CreateChartArea();
		var rsiArea = CreateChartArea("Spread RSI");
		if (area != null)
		{
			DrawCandles(area, frontSub);
			DrawOwnTrades(area);
		}
		if (rsiArea != null)
			DrawIndicator(rsiArea, _rsi);
	}

	private void ProcessFront(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_frontClose = candle.ClosePrice;
		TryTrade(candle);
	}

	private void ProcessSecond(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_secondClose = candle.ClosePrice;
		TryTrade(candle);
	}

	private void TryTrade(ICandleMessage candle)
	{
		if (_frontClose == 0m || _secondClose == 0m)
			return;

		var spread = _frontClose - _secondClose;
		var rsiValue = _rsi.Process(spread, candle.OpenTime, true).ToDecimal();

		if (!_rsi.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position <= 0 && rsiValue < LongThreshold)
			BuyMarket();
		else if (Position > 0 && rsiValue > ExitThreshold)
			SellMarket();
	}
}
