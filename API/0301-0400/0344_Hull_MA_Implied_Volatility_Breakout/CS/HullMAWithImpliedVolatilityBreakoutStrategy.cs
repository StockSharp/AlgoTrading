namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Hull slope with observed implied volatility breakouts and frozen ATR protection.</summary>
public class HullMAWithImpliedVolatilityBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<int> _iVPeriod;
	private readonly StrategyParam<decimal> _iVMultiplier;
	private readonly StrategyParam<string> _iVHistoryFile;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopLossAtr;
	private readonly StrategyParam<DataType> _candleType;
	private readonly Dictionary<DateTime, decimal> _observations = [];
	private readonly Queue<decimal> _history = [];
	private decimal? _previousHull;
	private Order _signalOrder;

	/// <summary>Hull moving average length.</summary>
	public int HmaPeriod { get => _hmaPeriod.Value; set => _hmaPeriod.Value = value; }

	/// <summary>Prior consecutive implied volatility observations used for the threshold.</summary>
	public int IVPeriod { get => _iVPeriod.Value; set => _iVPeriod.Value = value; }

	/// <summary>Sample standard deviation multiplier for the IV breakout.</summary>
	public decimal IVMultiplier { get => _iVMultiplier.Value; set => _iVMultiplier.Value = value; }

	/// <summary>CSV observed implied volatility in a consistent unit, keyed by candle UTC open time.</summary>
	public string IVHistoryFile { get => _iVHistoryFile.Value; set => _iVHistoryFile.Value = value; }

	/// <summary>Wilder ATR length for entry protection.</summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>Entry-frozen native stop distance in ATR multiples; zero disables it for a fresh run.</summary>
	public decimal StopLossAtr { get => _stopLossAtr.Value; set => _stopLossAtr.Value = value; }

	/// <summary>Completed signal candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Initialize the signal and protection parameters.</summary>
	public HullMAWithImpliedVolatilityBreakoutStrategy()
	{
		_hmaPeriod = Param(nameof(HmaPeriod), 9).SetRange(2, 100000).SetOptimize(5, 15, 2)
			.SetDisplay("HmaPeriod", "Hull moving average length", "Parameters");
		_iVPeriod = Param(nameof(IVPeriod), 20).SetRange(2, 100000).SetOptimize(10, 30, 5)
			.SetDisplay("IVPeriod", "Prior consecutive implied volatility observations used for the threshold", "Parameters");
		_iVMultiplier = Param(nameof(IVMultiplier), 2m).SetGreaterThanZero().SetOptimize(1.5m, 3m, 0.5m)
			.SetDisplay("IVMultiplier", "Sample standard deviation multiplier for the IV breakout", "Parameters");
		_iVHistoryFile = Param<string>(nameof(IVHistoryFile)).SetRequired()
			.SetDisplay("IVHistoryFile", "CSV observed implied volatility in a consistent unit, keyed by candle UTC open time", "Parameters");
		_atrPeriod = Param(nameof(AtrPeriod), 14).SetGreaterThanZero()
			.SetDisplay("AtrPeriod", "Wilder ATR length for entry protection", "Parameters");
		_stopLossAtr = Param(nameof(StopLossAtr), 2m).SetRange(0m, decimal.MaxValue).SetOptimize(1m, 3m, 0.5m)
			.SetDisplay("StopLossAtr", "Entry-frozen native stop distance in ATR multiples; zero disables it for a fresh run", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("CandleType", "Completed signal candle type", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_observations.Clear();
		_history.Clear();
		_previousHull = null;
		_signalOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		LoadObservations();
		base.OnStarted2(time);
		var signal = new HullMovingAverage { Length = HmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(signal, atr, ProcessCandle, allowEmpty: true).Start();
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quote = new Subscription(DataType.Level1, Security);
			quote.MarketData.BuildField = field;
			SubscribeLevel1(quote).Start();
		}
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, signal);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue signalValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished) return;
		var ready = _observations.TryGetValue(candle.OpenTime, out var observation);
		var low = false;
		var high = false;
		if (ready && _history.Count == IVPeriod)
		{
			var mean = _history.Average();
			var deviation = (decimal)Math.Sqrt((double)(_history.Sum(x => (x - mean) * (x - mean)) / (_history.Count - 1)));
			low = observation < mean - IVMultiplier * deviation;
			high = observation > mean + IVMultiplier * deviation;
		}
		if (!ready) _history.Clear();
		else
		{
			_history.Enqueue(observation);
			while (_history.Count > IVPeriod) _history.Dequeue();
		}
		if (signalValue.IsEmpty || !signalValue.IsFormed) return;
		var current = signalValue.GetValue<decimal>();
		var up = _previousHull is decimal previousUp && current > previousUp;
		var down = _previousHull is decimal previousDown && current < previousDown;
		_previousHull = current;
		if (!IsFormedAndOnlineAndAllowTrading() || _signalOrder is { State: not (OrderStates.Done or OrderStates.Failed) }) return;
		if ((Position > 0m && down) || (Position < 0m && up))
		{
			Submit(Position > 0m ? Sides.Sell : Sides.Buy, Math.Abs(Position), "0344Exit");
			return;
		}
		if (Position != 0m || atrValue.IsEmpty || !atrValue.IsFormed) return;
		var atr = atrValue.GetValue<decimal>();
		if (atr <= 0m) return;
		Sides? side = up && high ? Sides.Buy : down && high ? Sides.Sell : null;
		if (side is null) return;
		if (StopLossAtr > 0m)
			StartProtection(new Unit(), new Unit(atr * StopLossAtr, UnitTypes.Absolute), useMarketOrders: true, isLocalStop: true);
		Submit(side.Value, Volume, "0344Entry");
	}

	private void Submit(Sides side, decimal quantity, string comment)
	{
		_signalOrder = CreateOrder(side, 0m, quantity);
		_signalOrder.Type = OrderTypes.Market;
		_signalOrder.Comment = comment;
		RegisterOrder(_signalOrder);
	}

	private void LoadObservations()
	{
		if (string.IsNullOrWhiteSpace(IVHistoryFile))
			throw new InvalidOperationException("IVHistoryFile is required.");
		_observations.Clear();
		var lines = File.ReadLines(IVHistoryFile).GetEnumerator();
		using (lines)
		{
			if (!lines.MoveNext() || lines.Current.Trim() != "SecurityId,Time,Value")
				throw new InvalidOperationException("Option observations must begin with SecurityId,Time,Value.");
			while (lines.MoveNext())
			{
				if (string.IsNullOrWhiteSpace(lines.Current)) continue;
				var row = lines.Current.Split(',');
				if (row.Length != 3) throw new InvalidOperationException("An option observation must have three fields.");
				if (!string.Equals(row[0].Trim(), Security.Id, StringComparison.OrdinalIgnoreCase)) continue;
				var time = DateTime.ParseExact(row[1].Trim(), "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
				var value = decimal.Parse(row[2], CultureInfo.InvariantCulture);
				if (value < 0m || !_observations.TryAdd(time, value))
					throw new InvalidOperationException("Option observations must be nonnegative and unique per timestamp.");
			}
		}
		if (_observations.Count == 0) throw new InvalidOperationException("No option observations match the execution security.");
	}
}
