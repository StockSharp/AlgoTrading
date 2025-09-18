namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Mean reversion and trend following hybrid that reacts to moving average crossovers and ATR deviations.
/// </summary>
public class MeanReverseStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastMa = null!;
	private SimpleMovingAverage _slowMa = null!;
	private AverageTrueRange _atr = null!;

	private decimal? _prevFastMa;
	private decimal? _prevSlowMa;
	private decimal _stopLossPrice;
	private decimal _takeProfitPrice;

	/// <summary>
	/// Fast simple moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow simple moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// ATR lookback period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR for mean reversion envelopes.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in security steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in security steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Volume used for a new position.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public MeanReverseStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 20)
			.SetRange(5, 200)
			.SetDisplay("Fast MA Period", "Length of the fast simple moving average", "Indicators")
			.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 50)
			.SetRange(5, 300)
			.SetDisplay("Slow MA Period", "Length of the slow simple moving average", "Indicators")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetRange(5, 100)
			.SetDisplay("ATR Period", "Number of candles used for ATR calculation", "Indicators")
			.SetCanOptimize(true);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetRange(0.5m, 5m)
			.SetDisplay("ATR Multiplier", "Multiplier applied to ATR for deviation bands", "Indicators")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 500)
			.SetRange(10, 5000)
			.SetDisplay("Stop Loss Points", "Stop-loss distance in security steps", "Risk Management")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1000)
			.SetRange(10, 10000)
			.SetDisplay("Take Profit Points", "Take-profit distance in security steps", "Risk Management")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetRange(0.01m, 100m)
			.SetDisplay("Trade Volume", "Volume opened with a new signal", "Execution")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of market data processed by the strategy", "General");
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

		_prevFastMa = null;
		_prevSlowMa = null;
		_stopLossPrice = default;
		_takeProfitPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_fastMa = new SimpleMovingAverage { Length = FastMaPeriod };
		_slowMa = new SimpleMovingAverage { Length = SlowMaPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMa, decimal slowMa, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_atr.IsFormed)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var step = Security?.Step ?? 1m;

		if (_prevFastMa is null || _prevSlowMa is null)
		{
			_prevFastMa = fastMa;
			_prevSlowMa = slowMa;
			return;
		}

		var prevFast = _prevFastMa.Value;
		var prevSlow = _prevSlowMa.Value;

		var trendBuySignal = prevFast <= prevSlow && fastMa > slowMa;
		var trendSellSignal = prevFast >= prevSlow && fastMa < slowMa;

		var deviation = atr * AtrMultiplier;
		var reversionBuySignal = candle.ClosePrice < slowMa - deviation;
		var reversionSellSignal = candle.ClosePrice > slowMa + deviation;

		var buySignal = trendBuySignal || reversionBuySignal;
		var sellSignal = trendSellSignal || reversionSellSignal;

		if (Position == 0)
		{
			if (buySignal)
			{
				EnterLong(candle.ClosePrice, step);
			}
			else if (sellSignal)
			{
				EnterShort(candle.ClosePrice, step);
			}
		}
		else if (Position > 0)
		{
			ManageLongPosition(candle);
		}
		else
		{
			ManageShortPosition(candle);
		}

		_prevFastMa = fastMa;
		_prevSlowMa = slowMa;
	}

	private void EnterLong(decimal entryPrice, decimal step)
	{
		// Open a new long position and pre-calculate risk levels.
		BuyMarket(TradeVolume);

		_stopLossPrice = entryPrice - StopLossPoints * step;
		_takeProfitPrice = entryPrice + TakeProfitPoints * step;
	}

	private void EnterShort(decimal entryPrice, decimal step)
	{
		// Open a new short position and pre-calculate risk levels.
		SellMarket(TradeVolume);

		_stopLossPrice = entryPrice + StopLossPoints * step;
		_takeProfitPrice = entryPrice - TakeProfitPoints * step;
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		// Exit logic for the long position.
		if (candle.LowPrice <= _stopLossPrice)
		{
			SellMarket(Position);
			ClearRiskLevels();
			return;
		}

		if (_takeProfitPrice != default && candle.HighPrice >= _takeProfitPrice)
		{
			SellMarket(Position);
			ClearRiskLevels();
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		// Exit logic for the short position.
		if (candle.HighPrice >= _stopLossPrice)
		{
			BuyMarket(Math.Abs(Position));
			ClearRiskLevels();
			return;
		}

		if (_takeProfitPrice != default && candle.LowPrice <= _takeProfitPrice)
		{
			BuyMarket(Math.Abs(Position));
			ClearRiskLevels();
		}
	}

	private void ClearRiskLevels()
	{
		// Reset stored risk levels after an exit.
		_stopLossPrice = default;
		_takeProfitPrice = default;
	}
}
