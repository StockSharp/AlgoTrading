namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Trades a completed daily return anomaly and exits at the next available daily close.</summary>
public class BtcusdMomentumAfterAbnormalDaysStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _abnormalThreshold;
	private readonly StrategyParam<decimal> _capitalPerTrade;
	private readonly Queue<decimal> _returns = [];
	private DateTime _lastDate;
	private DateTime? _entryDay;
	private Order _pending;

	/// <summary>Finished daily signal candle interval.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Prior completed daily returns in the baseline.</summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>Population standard deviation multiplier.</summary>
	public decimal AbnormalThreshold
	{
		get => _abnormalThreshold.Value;
		set => _abnormalThreshold.Value = value;
	}

	/// <summary>Signal-close quote currency budget.</summary>
	public decimal CapitalPerTrade
	{
		get => _capitalPerTrade.Value;
		set => _capitalPerTrade.Value = value;
	}

	/// <summary>Initializes the daily anomaly and capital parameters.</summary>
	public BtcusdMomentumAfterAbnormalDaysStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("CandleType", "Finished daily signal candle interval", "Parameters");
		_lookbackPeriod = Param(nameof(LookbackPeriod), 5).SetGreaterThanZero()
			.SetDisplay("LookbackPeriod", "Prior completed daily returns in the baseline", "Parameters");
		_abnormalThreshold = Param(nameof(AbnormalThreshold), 1.6m).SetGreaterThanZero()
			.SetDisplay("AbnormalThreshold", "Population standard deviation multiplier", "Parameters");
		_capitalPerTrade = Param(nameof(CapitalPerTrade), 1000m).SetGreaterThanZero()
			.SetDisplay("CapitalPerTrade", "Signal-close quote currency budget", "Parameters");
		OrderRegistering += order => _pending = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_returns.Clear();
		_lastDate = default;
		_entryDay = null;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (!CandleType.Equals(TimeSpan.FromDays(1).TimeFrame()))
			throw new InvalidOperationException("Daily candles are required for daily anomaly and exit deadlines.");
		base.OnStarted2(time);
		var candles = SubscribeCandles(CandleType);
		candles.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, candles);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || candle.OpenPrice <= 0m || candle.ClosePrice <= 0m || candle.OpenTime.Date <= _lastDate)
			return;
		_lastDate = candle.OpenTime.Date;
		var sample = _returns.ToArray();
		var value = (candle.ClosePrice - candle.OpenPrice) / candle.OpenPrice;
		_returns.Enqueue(value);
		while (_returns.Count > LookbackPeriod) _returns.Dequeue();
		if (!IsFormedAndOnlineAndAllowTrading() || _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		if (Position != 0m)
		{
			if (_entryDay is DateTime day && candle.OpenTime.Date > day)
			{
				if (Position > 0m) SellMarket(Position);
				else BuyMarket(-Position);
			}
			return;
		}
		if (sample.Length < LookbackPeriod) return;
		var mean = sample.Average();
		var deviation = (decimal)Math.Sqrt((double)sample.Average(r => (r - mean) * (r - mean)));
		var side = value > mean + AbnormalThreshold * deviation ? Sides.Buy
			: value < mean - AbnormalThreshold * deviation ? Sides.Sell : (Sides?)null;
		if (side is null) return;
		var step = Security.VolumeStep ?? 1m;
		var quantity = Math.Floor(CapitalPerTrade / candle.ClosePrice / step) * step;
		if (quantity < (Security.MinVolume ?? step)) return;
		if (Security.MaxVolume is decimal maximum && quantity > maximum)
			throw new InvalidOperationException("CapitalPerTrade exceeds the instrument's maximum order volume.");
		_entryDay = candle.OpenTime.Date;
		if (side == Sides.Buy) BuyMarket(quantity);
		else SellMarket(quantity);
	}
}
