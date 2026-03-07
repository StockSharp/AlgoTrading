using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Combines an SMA trend filter with RSI and Stochastic oscillators.
/// Enters long when price is above SMA, RSI is oversold and Stochastic is oversold.
/// Enters short when price is below SMA, RSI is overbought and Stochastic is overbought.
/// </summary>
public class ExpertRsiStochasticMaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<decimal> _stochUpperLevel;
	private readonly StrategyParam<decimal> _stochLowerLevel;
	private readonly StrategyParam<int> _maPeriod;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal RsiUpperLevel
	{
		get => _rsiUpperLevel.Value;
		set => _rsiUpperLevel.Value = value;
	}

	public decimal RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
	}

	public int StochKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	public decimal StochUpperLevel
	{
		get => _stochUpperLevel.Value;
		set => _stochUpperLevel.Value = value;
	}

	public decimal StochLowerLevel
	{
		get => _stochLowerLevel.Value;
		set => _stochLowerLevel.Value = value;
	}

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public ExpertRsiStochasticMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for calculations", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Number of bars for RSI", "RSI");

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 80m)
			.SetDisplay("RSI Overbought", "Upper RSI threshold", "RSI");

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 20m)
			.SetDisplay("RSI Oversold", "Lower RSI threshold", "RSI");

		_stochKPeriod = Param(nameof(StochKPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Length of Stochastic %K", "Stochastic");

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Length of Stochastic %D", "Stochastic");

		_stochUpperLevel = Param(nameof(StochUpperLevel), 70m)
			.SetDisplay("Stoch Overbought", "Upper Stochastic threshold", "Stochastic");

		_stochLowerLevel = Param(nameof(StochLowerLevel), 30m)
			.SetDisplay("Stoch Oversold", "Lower Stochastic threshold", "Stochastic");

		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length of moving average", "Moving Average");
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
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SMA { Length = MaPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var stoch = new StochasticOscillator();
		stoch.K.Length = StochKPeriod;
		stoch.D.Length = StochDPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, rsi, stoch, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue, IIndicatorValue rsiValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!smaValue.IsFinal || !rsiValue.IsFinal || !stochValue.IsFinal)
			return;

		var sma = smaValue.IsEmpty ? (decimal?)null : smaValue.GetValue<decimal>();
		var rsiVal = rsiValue.IsEmpty ? (decimal?)null : rsiValue.GetValue<decimal>();

		if (sma is not decimal smaDecimal || rsiVal is not decimal rsiDecimal)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal stochK || stoch.D is not decimal stochD)
			return;

		var price = candle.ClosePrice;

		// Long entry: price above SMA, RSI oversold, Stochastic oversold
		if (price > smaDecimal && rsiDecimal < RsiLowerLevel && stochK < StochLowerLevel && stochD < StochLowerLevel)
		{
			if (Position <= 0)
				BuyMarket();
		}
		// Short entry: price below SMA, RSI overbought, Stochastic overbought
		else if (price < smaDecimal && rsiDecimal > RsiUpperLevel && stochK > StochUpperLevel && stochD > StochUpperLevel)
		{
			if (Position >= 0)
				SellMarket();
		}
		// Exit long: Stochastic overbought
		else if (Position > 0 && stochK > StochUpperLevel)
		{
			SellMarket();
		}
		// Exit short: Stochastic oversold
		else if (Position < 0 && stochK < StochLowerLevel)
		{
			BuyMarket();
		}
	}
}
