namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Finished-bar RSI threshold crossings confirmed by open-price SMMA/EMA and two typical-price CCIs.
/// </summary>
public class FiveMinuteRsiCciStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _fastSmmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _fastCciPeriod;
	private readonly StrategyParam<int> _slowCciPeriod;
	private readonly StrategyParam<decimal> _bullishRsiLevel;
	private readonly StrategyParam<decimal> _bearishRsiLevel;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _lotCoefficient;
	private readonly StrategyParam<decimal> _equityDivisor;
	private readonly StrategyParam<decimal> _maxSpreadPoints;

	private RelativeStrengthIndex _rsi;
	private SmoothedMovingAverage _smma;
	private ExponentialMovingAverage _ema;
	private CommodityChannelIndex _fastCci;
	private CommodityChannelIndex _slowCci;
	private decimal? _previousRsi;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal? _entryPrice;
	private decimal? _trailingStop;
	private bool _exitRequested;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int FastSmmaPeriod { get => _fastSmmaPeriod.Value; set => _fastSmmaPeriod.Value = value; }
	public int SlowEmaPeriod { get => _slowEmaPeriod.Value; set => _slowEmaPeriod.Value = value; }
	public int FastCciPeriod { get => _fastCciPeriod.Value; set => _fastCciPeriod.Value = value; }
	public int SlowCciPeriod { get => _slowCciPeriod.Value; set => _slowCciPeriod.Value = value; }
	public decimal BullishRsiLevel { get => _bullishRsiLevel.Value; set => _bullishRsiLevel.Value = value; }
	public decimal BearishRsiLevel { get => _bearishRsiLevel.Value; set => _bearishRsiLevel.Value = value; }
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	public decimal TrailingStopPoints { get => _trailingStopPoints.Value; set => _trailingStopPoints.Value = value; }
	public decimal LotCoefficient { get => _lotCoefficient.Value; set => _lotCoefficient.Value = value; }
	public decimal EquityDivisor { get => _equityDivisor.Value; set => _equityDivisor.Value = value; }
	public decimal MaxSpreadPoints { get => _maxSpreadPoints.Value; set => _maxSpreadPoints.Value = value; }

	public FiveMinuteRsiCciStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetGreaterThanZero();
		_fastSmmaPeriod = Param(nameof(FastSmmaPeriod), 2).SetGreaterThanZero();
		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 6).SetGreaterThanZero();
		_fastCciPeriod = Param(nameof(FastCciPeriod), 34).SetGreaterThanZero();
		_slowCciPeriod = Param(nameof(SlowCciPeriod), 175).SetGreaterThanZero();
		_bullishRsiLevel = Param(nameof(BullishRsiLevel), 55m);
		_bearishRsiLevel = Param(nameof(BearishRsiLevel), 45m);
		_stopLossPoints = Param(nameof(StopLossPoints), 60m).SetNotNegative();
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m).SetNotNegative();
		_trailingStopPoints = Param(nameof(TrailingStopPoints), 20m).SetNotNegative();
		_lotCoefficient = Param(nameof(LotCoefficient), 0.01m).SetGreaterThanZero();
		_equityDivisor = Param(nameof(EquityDivisor), 10m).SetGreaterThanZero();
		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 18m).SetNotNegative();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetRuntime();
	}

	private void ResetRuntime()
	{
		_previousRsi = null;
		_bestBid = null;
		_bestAsk = null;
		_entryPrice = null;
		_trailingStop = null;
		_exitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetRuntime();

		_rsi = new() { Length = RsiPeriod };
		_smma = new() { Length = FastSmmaPeriod };
		_ema = new() { Length = SlowEmaPeriod };
		_fastCci = new() { Length = FastCciPeriod };
		_slowCci = new() { Length = SlowCciPeriod };

		var point = GetPoint();
		var takeProfit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * point, UnitTypes.Absolute) : new Unit();
		var stopLoss = StopLossPoints > 0m ? new Unit(StopLossPoints * point, UnitTypes.Absolute) : new Unit();
		StartProtection(takeProfit, stopLoss, useMarketOrders: true, isLocalStop: true);

		SubscribeLevel1().Bind(ProcessLevel1).Start();
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
	}

	private decimal GetPoint()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			throw new InvalidOperationException("Positive Security.PriceStep is required.");
		return Security.Decimals is 3 or 5 ? step * 10m : step;
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
			_bestBid = bid;
		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
			_bestAsk = ask;

		if (Position == 0m || _entryPrice is null || TrailingStopPoints <= 0m || _exitRequested)
			return;

		var gap = TrailingStopPoints * GetPoint();
		var price = Position > 0m ? _bestBid : _bestAsk;
		if (price is null)
			return;

		if (Position > 0m)
		{
			if (price.Value >= _entryPrice.Value + gap)
			{
				var candidate = price.Value - gap;
				if (_trailingStop is null || candidate > _trailingStop.Value)
					_trailingStop = candidate;
			}
			if (_trailingStop is decimal stop && price.Value <= stop)
				RequestExit();
		}
		else
		{
			if (price.Value <= _entryPrice.Value - gap)
			{
				var candidate = price.Value + gap;
				if (_trailingStop is null || candidate < _trailingStop.Value)
					_trailingStop = candidate;
			}
			if (_trailingStop is decimal stop && price.Value >= stop)
				RequestExit();
		}
	}

	private void RequestExit()
	{
		if (_exitRequested || Position == 0m)
			return;
		_exitRequested = true;
		ClosePosition();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		if (Position == 0m)
		{
			_entryPrice = null;
			_trailingStop = null;
			_exitRequested = false;
		}
		else if (_entryPrice is null)
		{
			_entryPrice = trade.Trade.Price;
		}
	}

	private bool IsSpreadAllowed()
		=> _bestBid is decimal bid && _bestAsk is decimal ask
			&& ask >= bid && ask - bid < MaxSpreadPoints * GetPoint();

	private decimal GetVolume()
	{
		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (equity <= 0m || EquityDivisor <= 0m)
			return 0m;

		var raw = LotCoefficient * (decimal)Math.Sqrt((double)(equity / EquityDivisor));
		var minimum = Security?.MinVolume ?? 0m;
		var maximum = Security?.MaxVolume ?? decimal.MaxValue;
		var step = Security?.VolumeStep ?? 0m;
		if (maximum < minimum)
			return 0m;

		if (step > 0m)
		{
			var first = Math.Ceiling(minimum / step) * step;
			var last = Math.Floor(maximum / step) * step;
			if (first > last)
				return 0m;
			return Math.Clamp(Math.Floor(raw / step) * step, first, last);
		}

		return Math.Clamp(raw, minimum, maximum);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rsi = _rsi.Process(candle.ClosePrice, candle.OpenTime, true);
		var smma = _smma.Process(candle.OpenPrice, candle.OpenTime, true);
		var ema = _ema.Process(candle.OpenPrice, candle.OpenTime, true);
		var fastCci = _fastCci.Process(candle);
		var slowCci = _slowCci.Process(candle);

		if (!rsi.IsFormed || !smma.IsFormed || !ema.IsFormed || !fastCci.IsFormed || !slowCci.IsFormed)
			return;

		var currentRsi = rsi.ToDecimal();
		if (_previousRsi is decimal previous && Position == 0m && !_exitRequested && IsSpreadAllowed())
		{
			var volume = GetVolume();
			if (volume > 0m)
			{
				if (previous <= BullishRsiLevel && currentRsi > BullishRsiLevel &&
					smma.ToDecimal() > ema.ToDecimal() &&
					fastCci.ToDecimal() > 0m && slowCci.ToDecimal() > 0m)
					BuyMarket(volume);
				else if (previous >= BearishRsiLevel && currentRsi < BearishRsiLevel &&
					smma.ToDecimal() < ema.ToDecimal() &&
					fastCci.ToDecimal() < 0m && slowCci.ToDecimal() < 0m)
					SellMarket(volume);
			}
		}

		_previousRsi = currentRsi;
	}
}
