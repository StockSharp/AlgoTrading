namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy using triple CCI confirmed by MFI and EMA with ATR based trailing exit.
/// </summary>
public class TripleCciMfiConfirmedStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<decimal> _trailingActivationMultiplier;
	private readonly StrategyParam<int> _fastCciPeriod;
	private readonly StrategyParam<int> _middleCciPeriod;
	private readonly StrategyParam<int> _slowCciPeriod;
	private readonly StrategyParam<int> _mfiLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _trailingEmaLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _tradeStart;
	private readonly StrategyParam<DateTimeOffset> _tradeStop;

	private decimal _prevFastCci;
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
	/// Middle CCI period.
	/// </summary>
	public int MiddleCciPeriod
	{
		get => _middleCciPeriod.Value;
		set => _middleCciPeriod.Value = value;
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
	/// MFI length.
	/// </summary>
	public int MfiLength
	{
		get => _mfiLength.Value;
		set => _mfiLength.Value = value;
	}

	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
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
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trading start date.
	/// </summary>
	public DateTimeOffset TradeStart
	{
		get => _tradeStart.Value;
		set => _tradeStart.Value = value;
	}

	/// <summary>
	/// Trading stop date.
	/// </summary>
	public DateTimeOffset TradeStop
	{
		get => _tradeStop.Value;
		set => _tradeStop.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TripleCciMfiConfirmedStrategy()
	{
		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 1.75m)
			.SetRange(0.5m, 5m)
			.SetDisplay("ATR Stop Loss", "ATR multiplier for stop loss", "Risk Management")
			.SetCanOptimize(true);

		_trailingActivationMultiplier = Param(nameof(TrailingActivationMultiplier), 2.25m)
			.SetRange(0.5m, 5m)
			.SetDisplay("ATR Trailing Activation", "ATR multiplier to activate trailing", "Risk Management")
			.SetCanOptimize(true);

		_fastCciPeriod = Param(nameof(FastCciPeriod), 14)
			.SetRange(5, 50)
			.SetDisplay("CCI Fast Length", "Fast CCI period", "Indicators")
			.SetCanOptimize(true);

		_middleCciPeriod = Param(nameof(MiddleCciPeriod), 25)
			.SetRange(5, 100)
			.SetDisplay("CCI Middle Length", "Middle CCI period", "Indicators")
			.SetCanOptimize(true);

		_slowCciPeriod = Param(nameof(SlowCciPeriod), 50)
			.SetRange(10, 150)
			.SetDisplay("CCI Slow Length", "Slow CCI period", "Indicators")
			.SetCanOptimize(true);

		_mfiLength = Param(nameof(MfiLength), 14)
			.SetRange(1, 200)
			.SetDisplay("MFI Length", "Money Flow Index length", "Indicators")
			.SetCanOptimize(true);

		_emaLength = Param(nameof(EmaLength), 50)
			.SetRange(10, 200)
			.SetDisplay("EMA Length", "EMA filter length", "Indicators")
			.SetCanOptimize(true);

		_trailingEmaLength = Param(nameof(TrailingEmaLength), 20)
			.SetRange(10, 100)
			.SetDisplay("Trailing EMA Length", "EMA length for trailing profit", "Indicators")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetRange(5, 50)
			.SetDisplay("ATR Period", "ATR calculation period", "Indicators")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_tradeStart = Param(nameof(TradeStart), new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Trade Start", "Start date for trading", "Time Range");

		_tradeStop = Param(nameof(TradeStop), new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Trade Stop", "Stop date for trading", "Time Range");
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

		_prevFastCci = default;
		_stopLossLevel = default;
		_activationLevel = default;
		_takeProfitLevel = default;
		_trailingActivated = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastCci = new CommodityChannelIndex { Length = FastCciPeriod };
		var middleCci = new CommodityChannelIndex { Length = MiddleCciPeriod };
		var slowCci = new CommodityChannelIndex { Length = SlowCciPeriod };
		var mfi = new MoneyFlowIndex { Length = MfiLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var trailingEma = new ExponentialMovingAverage { Length = TrailingEmaLength };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastCci, middleCci, slowCci, mfi, ema, trailingEma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastCci);
			DrawIndicator(area, middleCci);
			DrawIndicator(area, slowCci);
			DrawIndicator(area, mfi);
			DrawIndicator(area, ema);
			DrawIndicator(area, trailingEma);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal fastCci, decimal middleCci, decimal slowCci, decimal mfi, decimal ema, decimal trailingEma, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.OpenTime < TradeStart || candle.OpenTime > TradeStop)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossedUp = _prevFastCci <= 0m && fastCci > 0m;
		_prevFastCci = fastCci;

		if (crossedUp && candle.ClosePrice > ema && middleCci > 0m && slowCci > 0m && mfi > 50m && Position <= 0)
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
			_takeProfitLevel = trailingEma;

		if (_takeProfitLevel != default && candle.ClosePrice < _takeProfitLevel)
		{
			SellMarket(Position);
			return;
		}

		if (candle.LowPrice <= _stopLossLevel)
			SellMarket(Position);
	}
}
