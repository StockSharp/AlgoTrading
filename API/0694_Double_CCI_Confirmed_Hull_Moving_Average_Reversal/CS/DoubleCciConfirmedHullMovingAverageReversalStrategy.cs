namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that trades when price crosses above Hull MA confirmed by two CCI indicators.
/// </summary>
public class DoubleCciConfirmedHullMovingAverageReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<decimal> _trailingActivationMultiplier;
	private readonly StrategyParam<int> _fastCciPeriod;
	private readonly StrategyParam<int> _slowCciPeriod;
	private readonly StrategyParam<int> _hullMaLength;
	private readonly StrategyParam<int> _trailingEmaLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHighMinusHull;
	private decimal _stopLossLevel;
	private decimal _activationLevel;
	private decimal _takeProfitLevel;
	private bool _trailingActivated;

	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _stopLossAtrMultiplier.Value;
		set => _stopLossAtrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for trailing profit activation.
	/// </summary>
	public decimal TrailingActivationMultiplier
	{
		get => _trailingActivationMultiplier.Value;
		set => _trailingActivationMultiplier.Value = value;
	}

	/// <summary>
	/// Fast CCI period.
	/// </summary>
	public int FastCciPeriod
	{
		get => _fastCciPeriod.Value;
		set => _fastCciPeriod.Value = value;
	}

	/// <summary>
	/// Slow CCI period.
	/// </summary>
	public int SlowCciPeriod
	{
		get => _slowCciPeriod.Value;
		set => _slowCciPeriod.Value = value;
	}

	/// <summary>
	/// Hull MA length.
	/// </summary>
	public int HullMaLength
	{
		get => _hullMaLength.Value;
		set => _hullMaLength.Value = value;
	}

	/// <summary>
	/// Trailing EMA length.
	/// </summary>
	public int TrailingEmaLength
	{
		get => _trailingEmaLength.Value;
		set => _trailingEmaLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public DoubleCciConfirmedHullMovingAverageReversalStrategy()
	{
		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 1.75m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Stop Loss ATR Multiplier", "ATR multiplier for stop loss", "Risk Management")
			.SetCanOptimize(true);

		_trailingActivationMultiplier = Param(nameof(TrailingActivationMultiplier), 2.25m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Trailing Activation Multiplier", "ATR multiplier for trailing profit activation", "Risk Management")
			.SetCanOptimize(true);

		_fastCciPeriod = Param(nameof(FastCciPeriod), 25)
			.SetRange(5, 50)
			.SetDisplay("Fast CCI Period", "Length of fast CCI", "Indicators")
			.SetCanOptimize(true);

		_slowCciPeriod = Param(nameof(SlowCciPeriod), 50)
			.SetRange(10, 100)
			.SetDisplay("Slow CCI Period", "Length of slow CCI", "Indicators")
			.SetCanOptimize(true);

		_hullMaLength = Param(nameof(HullMaLength), 34)
			.SetRange(10, 100)
			.SetDisplay("Hull MA Length", "Length for Hull Moving Average", "Indicators")
			.SetCanOptimize(true);

		_trailingEmaLength = Param(nameof(TrailingEmaLength), 20)
			.SetRange(10, 100)
			.SetDisplay("Trailing EMA Length", "Length for trailing EMA", "Indicators")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetRange(5, 50)
			.SetDisplay("ATR Period", "ATR calculation period", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_prevHighMinusHull = default;
		_stopLossLevel = default;
		_activationLevel = default;
		_takeProfitLevel = default;
		_trailingActivated = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var hull = new HullMovingAverage { Length = HullMaLength };
		var fastCci = new CommodityChannelIndex { Length = FastCciPeriod };
		var slowCci = new CommodityChannelIndex { Length = SlowCciPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var trailingEma = new ExponentialMovingAverage { Length = TrailingEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(hull, fastCci, slowCci, atr, trailingEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hull);
			DrawIndicator(area, fastCci);
			DrawIndicator(area, slowCci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hull, decimal fastCci, decimal slowCci, decimal atr, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var diff = candle.HighPrice - hull;
		var crossedUp = _prevHighMinusHull <= 0m && diff > 0m;
		_prevHighMinusHull = diff;

		if (crossedUp && candle.ClosePrice > hull && fastCci > 0m && slowCci > 0m && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);

			_stopLossLevel = candle.ClosePrice - StopLossAtrMultiplier * atr;
			_activationLevel = candle.ClosePrice + TrailingActivationMultiplier * atr;
			_trailingActivated = false;
			_takeProfitLevel = default;
			return;
		}

		if (Position <= 0)
			return;

		if (!_trailingActivated && candle.HighPrice > _activationLevel)
			_trailingActivated = true;

		if (_trailingActivated)
			_takeProfitLevel = ema;

		if (_takeProfitLevel != default && candle.ClosePrice < _takeProfitLevel)
		{
			SellMarket(Position);
			return;
		}

		if (candle.LowPrice <= _stopLossLevel)
			SellMarket(Position);
	}
}
