namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Heikin Ashi band reversal with a real half-position target and a causal trailing remainder.</summary>
public class BollingerHeikinAshiEntryStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<DataType> _candleType;
	private BollingerBands _bands;
	private Order _pending;
	private bool _haInitialized, _targetTaken, _invalidFill;
	private decimal _haOpen, _haClose, _previousLow, _previousHigh;
	private decimal _stop, _target, _entryCost, _entryQuantity;
	private int _redTouches, _greenTouches;

	/// <summary>Initializes the published Heikin Ashi and Bollinger parameters.</summary>
	public BollingerHeikinAshiEntryStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20).SetGreaterThanZero().SetOptimize(10, 50, 5)
			.SetDisplay("BollingerPeriod", "Heikin Ashi close window", "Parameters");
		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m).SetGreaterThanZero().SetOptimize(1m, 3m, 0.5m)
			.SetDisplay("BollingerDeviation", "Bollinger standard deviation multiplier", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("CandleType", "Finished signal candle interval", "Parameters");
	}

	/// <summary>Heikin Ashi close window.</summary>
	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }

	/// <summary>Bollinger standard deviation multiplier.</summary>
	public decimal BollingerDeviation { get => _bollingerDeviation.Value; set => _bollingerDeviation.Value = value; }

	/// <summary>Finished signal candle interval.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	private bool IsPending => _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed);

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_bands = null;
		_pending = null;
		_haInitialized = _targetTaken = _invalidFill = false;
		_haOpen = _haClose = _previousLow = _previousHigh = 0m;
		_stop = _target = _entryCost = _entryQuantity = 0m;
		_redTouches = _greenTouches = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_bands = new BollingerBands { Length = BollingerPeriod, Width = BollingerDeviation };
		Indicators.Add(_bands);
		var candles = SubscribeCandles(CandleType);
		candles.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, candles);
			DrawIndicator(area, _bands);
			DrawOwnTrades(area);
		}
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quote = new Subscription(DataType.Level1, Security);
			quote.MarketData.BuildField = field;
			SubscribeLevel1(quote).Bind(ProcessQuote).Start();
		}
	}

	private void Submit(Sides side, decimal quantity, string reason)
	{
		_pending = CreateOrder(side, 0m, quantity);
		_pending.Type = OrderTypes.Market;
		_pending.Comment = "0554" + reason;
		RegisterOrder(_pending);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var open = _haInitialized ? (_haOpen + _haClose) / 2m : (candle.OpenPrice + candle.ClosePrice) / 2m;
		var close = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		_haOpen = open;
		_haClose = close;
		_haInitialized = true;
		var input = new DecimalIndicatorValue(_bands, close, candle.OpenTime) { IsFinal = true };
		var bands = (BollingerBandsValue)_bands.Process(input);
		Sides? signal = null;
		if (_bands.IsFormed && bands.LowBand is decimal lower && bands.UpBand is decimal upper)
		{
			var redValid = _redTouches >= 2;
			var greenValid = _greenTouches >= 2;
			if (close > open && close > lower && redValid)
				signal = Sides.Buy;
			else if (close < open && close < upper && greenValid)
				signal = Sides.Sell;
			_redTouches = close < open && Math.Min(candle.LowPrice, Math.Min(open, close)) <= lower ? _redTouches + 1 : 0;
			_greenTouches = close > open && Math.Max(candle.HighPrice, Math.Max(open, close)) >= upper ? _greenTouches + 1 : 0;
		}
		if (Position != 0m && _targetTaken)
			_stop = Position > 0m ? Math.Max(_stop, candle.LowPrice) : Math.Min(_stop, candle.HighPrice);
		var setupStop = signal == Sides.Buy ? _previousLow : _previousHigh;
		_previousLow = candle.LowPrice;
		_previousHigh = candle.HighPrice;
		if (signal is null || Position != 0m || IsPending || !IsFormedAndOnlineAndAllowTrading())
			return;
		_stop = setupStop;
		_entryCost = _entryQuantity = 0m;
		_targetTaken = _invalidFill = false;
		Submit(signal.Value, Volume, "Entry");
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);
		if (trade.Order.Comment == "0554Entry")
		{
			_entryCost += trade.Trade.Price * trade.Trade.Volume;
			_entryQuantity += trade.Trade.Volume;
			var entry = _entryCost / _entryQuantity;
			var risk = trade.Order.Side == Sides.Buy ? entry - _stop : _stop - entry;
			_invalidFill = risk <= 0m;
			_target = trade.Order.Side == Sides.Buy ? entry + risk : entry - risk;
		}
		else if (trade.Order.Comment == "0554Target")
		{
			_targetTaken = true;
			if (Position != 0m)
				_stop = Position > 0m ? Math.Max(_stop, _previousLow) : Math.Min(_stop, _previousHigh);
		}
	}

	private void ProcessQuote(Level1ChangeMessage quote)
	{
		if (Position == 0m || _entryQuantity == 0m || IsPending || !IsFormedAndOnlineAndAllowTrading())
			return;
		var field = Position > 0m ? Level1Fields.BestBidPrice : Level1Fields.BestAskPrice;
		if (!quote.Changes.TryGetValue(field, out var raw) || raw is not decimal price || price <= 0m)
			return;
		var side = Position > 0m ? Sides.Sell : Sides.Buy;
		if (_invalidFill || (Position > 0m ? price <= _stop : price >= _stop))
			Submit(side, Math.Abs(Position), "Stop");
		else if (!_targetTaken && (Position > 0m ? price >= _target : price <= _target))
		{
			var quantity = Math.Abs(Position);
			var step = Security.VolumeStep ?? 1m;
			var minimum = Security.MinVolume ?? step;
			var half = Math.Floor(quantity / 2m / step) * step;
			// A sub-lot remainder cannot be managed: take the whole position at 1R instead.
			Submit(side, half >= minimum && quantity - half >= minimum ? half : quantity, "Target");
		}
	}
}
