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

/// <summary>Daily geometric AHR999 accumulation with one budgeted purchase per eligible UTC Monday.</summary>
public class BtcDcaAhr999Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _usdInvest1;
	private readonly StrategyParam<decimal> _usdInvest2;
	private readonly StrategyParam<DateTime> _startDate;
	private readonly StrategyParam<DateTime> _endDate;
	private readonly Queue<decimal> _prices = [];
	private DateTime _lastDate;
	private Order _pending;

	/// <summary>Finished daily candle interval.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Daily geometric mean window.</summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>Budget below AHR999 1.2.</summary>
	public decimal UsdInvest1
	{
		get => _usdInvest1.Value;
		set => _usdInvest1.Value = value;
	}

	/// <summary>Budget below AHR999 0.45.</summary>
	public decimal UsdInvest2
	{
		get => _usdInvest2.Value;
		set => _usdInvest2.Value = value;
	}

	/// <summary>First eligible UTC date.</summary>
	public DateTime StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>Last eligible UTC date.</summary>
	public DateTime EndDate
	{
		get => _endDate.Value;
		set => _endDate.Value = value;
	}

	/// <summary>Latest fully formed AHR999 observation.</summary>
	public decimal? LastAhr999 { get; private set; }

	/// <summary>Initializes the published accumulation parameters.</summary>
	public BtcDcaAhr999Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("CandleType", "Finished daily candle interval", "Parameters");
		_length = Param(nameof(Length), 200).SetGreaterThanZero()
			.SetDisplay("Length", "Daily geometric mean window", "Parameters");
		_usdInvest1 = Param(nameof(UsdInvest1), 100m).SetGreaterThanZero()
			.SetDisplay("UsdInvest1", "Budget below AHR999 1.2", "Parameters");
		_usdInvest2 = Param(nameof(UsdInvest2), 1000m).SetGreaterThanZero()
			.SetDisplay("UsdInvest2", "Budget below AHR999 0.45", "Parameters");
		_startDate = Param(nameof(StartDate), new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc))
			.SetDisplay("StartDate", "First eligible UTC date", "Parameters");
		_endDate = Param(nameof(EndDate), new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc))
			.SetDisplay("EndDate", "Last eligible UTC date", "Parameters");
		OrderRegistering += order => _pending = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prices.Clear();
		_lastDate = default;
		_pending = null;
		LastAhr999 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		if (!CandleType.Equals(TimeSpan.FromDays(1).TimeFrame()) || StartDate.Kind != DateTimeKind.Utc
			|| EndDate.Kind != DateTimeKind.Utc || StartDate > EndDate)
			throw new InvalidOperationException("Daily candles and an ordered UTC date range are required.");
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
		if (candle.State != CandleStates.Finished || candle.ClosePrice <= 0m || candle.OpenTime.Date <= _lastDate)
			return;
		var date = candle.OpenTime.Date;
		_lastDate = date;
		_prices.Enqueue(candle.ClosePrice);
		while (_prices.Count > Length) _prices.Dequeue();
		if (_prices.Count < Length) return;
		var age = (date - new DateTime(2009, 1, 3, 0, 0, 0, DateTimeKind.Utc)).TotalDays;
		if (age <= 0) return;
		var geometric = Math.Exp(_prices.Average(p => Math.Log((double)p)));
		var fitted = Math.Pow(10, 5.84 * Math.Log10(age) - 17.01);
		LastAhr999 = (decimal)((double)candle.ClosePrice / geometric * (double)candle.ClosePrice / fitted);
		if (!IsFormedAndOnlineAndAllowTrading() || date < StartDate.Date || date > EndDate.Date || date.DayOfWeek != DayOfWeek.Monday
			|| _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		var budget = LastAhr999 < 0.45m ? UsdInvest2 : LastAhr999 < 1.2m ? UsdInvest1 : 0m;
		if (budget == 0m) return;
		var step = Security.VolumeStep ?? 1m;
		var quantity = Math.Floor(budget / candle.ClosePrice / step) * step;
		if (quantity < (Security.MinVolume ?? step)) return;
		if (Security.MaxVolume is decimal maximum && quantity > maximum)
			throw new InvalidOperationException("The investment exceeds the instrument's maximum order volume.");
		BuyMarket(quantity);
	}
}
