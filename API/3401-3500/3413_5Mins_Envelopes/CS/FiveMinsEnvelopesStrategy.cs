namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Mean reversion from LWMA envelopes of median candle price, using the last finished candle and live bid.
/// </summary>
public class FiveMinsEnvelopesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _distancePoints;
	private readonly StrategyParam<int> _envelopePeriod;
	private readonly StrategyParam<decimal> _envelopeDeviationPercent;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _maxSpreadPoints;

	private readonly List<decimal> _medianPrices = [];
	private decimal? _previousLow;
	private decimal? _previousHigh;
	private decimal? _previousLowerBand;
	private decimal? _previousUpperBand;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal? _entryPrice;
	private decimal? _trailingStop;
	private bool _entryRequested;
	private bool _exitRequested;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal TradeVolume { get => _tradeVolume.Value; set => _tradeVolume.Value = value; }
	public decimal DistancePoints { get => _distancePoints.Value; set => _distancePoints.Value = value; }
	public int EnvelopePeriod { get => _envelopePeriod.Value; set => _envelopePeriod.Value = value; }
	public decimal EnvelopeDeviationPercent { get => _envelopeDeviationPercent.Value; set => _envelopeDeviationPercent.Value = value; }
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	public decimal TrailingStopPoints { get => _trailingStopPoints.Value; set => _trailingStopPoints.Value = value; }
	public decimal MaxSpreadPoints { get => _maxSpreadPoints.Value; set => _maxSpreadPoints.Value = value; }

	public FiveMinsEnvelopesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
		_tradeVolume = Param(nameof(TradeVolume), 1m).SetGreaterThanZero();
		_distancePoints = Param(nameof(DistancePoints), 140m).SetNotNegative();
		_envelopePeriod = Param(nameof(EnvelopePeriod), 3).SetGreaterThanZero();
		_envelopeDeviationPercent = Param(nameof(EnvelopeDeviationPercent), 0.05m).SetNotNegative();
		_stopLossPoints = Param(nameof(StopLossPoints), 250m).SetNotNegative();
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m).SetNotNegative();
		_trailingStopPoints = Param(nameof(TrailingStopPoints), 120m).SetNotNegative();
		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 25m).SetNotNegative();
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
		_medianPrices.Clear();
		_previousLow = null;
		_previousHigh = null;
		_previousLowerBand = null;
		_previousUpperBand = null;
		_bestBid = null;
		_bestAsk = null;
		_entryPrice = null;
		_trailingStop = null;
		_entryRequested = false;
		_exitRequested = false;
	}

	private decimal Point()
	{
		var point = Security?.PriceStep ?? 0m;
		if (point <= 0m)
			throw new InvalidOperationException("FiveMinsEnvelopesStrategy needs positive Security.PriceStep.");
		return point;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetRuntime();
		var point = Point();
		var takeProfit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * point, UnitTypes.Absolute) : new Unit();
		var stopLoss = StopLossPoints > 0m ? new Unit(StopLossPoints * point, UnitTypes.Absolute) : new Unit();
		StartProtection(takeProfit, stopLoss, useMarketOrders: true, isLocalStop: true);
		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
		SubscribeLevel1().Bind(ProcessLevel1).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_medianPrices.Add((candle.HighPrice + candle.LowPrice) / 2m);
		while (_medianPrices.Count > EnvelopePeriod)
			_medianPrices.RemoveAt(0);
		if (_medianPrices.Count < EnvelopePeriod)
			return;

		var weighted = 0m;
		var totalWeights = EnvelopePeriod * (EnvelopePeriod + 1) / 2m;
		for (var i = 0; i < _medianPrices.Count; i++)
			weighted += _medianPrices[i] * (i + 1);
		var middle = weighted / totalWeights;
		_previousLow = candle.LowPrice;
		_previousHigh = candle.HighPrice;
		_previousLowerBand = middle * (1m - EnvelopeDeviationPercent / 100m);
		_previousUpperBand = middle * (1m + EnvelopeDeviationPercent / 100m);
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
			_bestBid = bid;
		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
			_bestAsk = ask;
		if (_bestBid is not decimal currentBid || _bestAsk is not decimal currentAsk || currentAsk < currentBid)
			return;

		if (Position != 0m)
		{
			ApplyTrailing();
			return;
		}

		if (_entryRequested || _exitRequested || _previousLow is null || _previousHigh is null ||
			_previousLowerBand is null || _previousUpperBand is null || TradeVolume <= 0m)
			return;

		var point = Point();
		if (MaxSpreadPoints > 0m && currentAsk - currentBid >= MaxSpreadPoints * point)
			return;

		var distance = DistancePoints * point;
		if (_previousLow.Value < _previousLowerBand.Value - distance &&
			currentBid < _previousLowerBand.Value - distance)
		{
			_entryRequested = true;
			BuyMarket(TradeVolume);
		}
		else if (_previousHigh.Value > _previousUpperBand.Value + distance &&
			currentBid > _previousUpperBand.Value + distance)
		{
			_entryRequested = true;
			SellMarket(TradeVolume);
		}
	}

	private void ApplyTrailing()
	{
		if (_exitRequested || _entryPrice is null || TrailingStopPoints <= 0m)
			return;
		var price = Position > 0m ? _bestBid : _bestAsk;
		if (price is null)
			return;
		var gap = TrailingStopPoints * Point();
		if (Position > 0m)
		{
			if (price.Value >= _entryPrice.Value + gap)
			{
				var candidate = price.Value - gap;
				if (_trailingStop is null || candidate > _trailingStop)
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
				if (_trailingStop is null || candidate < _trailingStop)
					_trailingStop = candidate;
			}
			if (_trailingStop is decimal stop && price.Value >= stop)
				RequestExit();
		}
	}

	private void RequestExit()
	{
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
			_entryRequested = false;
			_exitRequested = false;
		}
		else if (_entryPrice is null)
		{
			_entryPrice = trade.Trade.Price;
		}
	}
}
