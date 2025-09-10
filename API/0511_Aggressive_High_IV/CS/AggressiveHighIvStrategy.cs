using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Aggressive strategy for high implied volatility markets.
/// Uses EMA crossover with ATR volatility filter and ATR-based exits.
/// </summary>
public class AggressiveHighIvStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _atrMeanLength;
	private readonly StrategyParam<int> _atrStdLength;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _atrMean;
	private StandardDeviation _atrStd;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR mean period.
	/// </summary>
	public int AtrMeanLength
	{
		get => _atrMeanLength.Value;
		set => _atrMeanLength.Value = value;
	}

	/// <summary>
	/// ATR standard deviation period.
	/// </summary>
	public int AtrStdLength
	{
		get => _atrStdLength.Value;
		set => _atrStdLength.Value = value;
	}

	/// <summary>
	/// Risk per trade as fraction of equity.
	/// </summary>
	public decimal RiskFactor
	{
		get => _riskFactor.Value;
		set => _riskFactor.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AggressiveHighIvStrategy"/>.
	/// </summary>
	public AggressiveHighIvStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Period for fast EMA", "Parameters")
			.SetCanOptimize(true);

		_slowEmaLength = Param(nameof(SlowEmaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Period for slow EMA", "Parameters")
			.SetCanOptimize(true);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation period", "Parameters")
			.SetCanOptimize(true);

		_atrMeanLength = Param(nameof(AtrMeanLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Mean Length", "Period for ATR mean", "Parameters")
			.SetCanOptimize(true);

		_atrStdLength = Param(nameof(AtrStdLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Std Length", "Period for ATR standard deviation", "Parameters")
			.SetCanOptimize(true);

		_riskFactor = Param(nameof(RiskFactor), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Factor", "Fraction of equity risked per trade", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_prevFast = 0m;
		_prevSlow = 0m;
		_longStop = 0m;
		_longTake = 0m;
		_shortStop = 0m;
		_shortTake = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		_atr = new AverageTrueRange { Length = AtrLength };
		_atrMean = new SimpleMovingAverage { Length = AtrMeanLength };
		_atrStd = new StandardDeviation { Length = AtrStdLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEma, decimal slowEma, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atrMeanValue = _atrMean.Process(atrValue, candle.ServerTime, true).ToDecimal();
		var atrStdValue = _atrStd.Process(atrValue, candle.ServerTime, true).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading() || !_atrMean.IsFormed || !_atrStd.IsFormed)
		{
			_prevFast = fastEma;
			_prevSlow = slowEma;
			return;
		}

		var longCondition = _prevFast <= _prevSlow && fastEma > slowEma && atrValue > atrMeanValue + atrStdValue;
		var shortCondition = _prevFast >= _prevSlow && fastEma < slowEma && atrValue > atrMeanValue + atrStdValue;

		var portfolioValue = Portfolio.CurrentValue ?? 0m;
		var positionSize = Math.Min(portfolioValue * RiskFactor / (2m * atrValue), portfolioValue);

		if (longCondition && Position <= 0)
		{
			BuyMarket(positionSize);
			_longStop = candle.ClosePrice - 2m * atrValue;
			_longTake = candle.ClosePrice + 4m * atrValue;
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket(positionSize);
			_shortStop = candle.ClosePrice + 2m * atrValue;
			_shortTake = candle.ClosePrice - 4m * atrValue;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _longStop || candle.HighPrice >= _longTake)
			{
				SellMarket(Math.Abs(Position));
				_longStop = _longTake = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _shortStop || candle.LowPrice <= _shortTake)
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = _shortTake = 0m;
			}
		}

		_prevFast = fastEma;
		_prevSlow = slowEma;
	}
}
