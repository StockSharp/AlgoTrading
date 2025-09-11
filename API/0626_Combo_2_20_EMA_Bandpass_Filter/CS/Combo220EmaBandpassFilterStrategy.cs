using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining EMA crossover with bandpass filter signals.
/// </summary>
public class Combo220EmaBandpassFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _bpfLength;
	private readonly StrategyParam<decimal> _bpfDelta;
	private readonly StrategyParam<decimal> _bpfSellZone;
	private readonly StrategyParam<decimal> _bpfBuyZone;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _beta;
	private decimal _gamma;
	private decimal _alpha;
	private decimal _bpPrev1;
	private decimal _bpPrev2;
	private decimal _hlPrev1;
	private decimal _hlPrev2;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }

	/// <summary>
	/// Bandpass filter length.
	/// </summary>
	public int BpfLength { get => _bpfLength.Value; set => _bpfLength.Value = value; }

	/// <summary>
	/// Bandpass delta parameter.
	/// </summary>
	public decimal BpfDelta { get => _bpfDelta.Value; set => _bpfDelta.Value = value; }

	/// <summary>
	/// Bandpass sell zone threshold.
	/// </summary>
	public decimal BpfSellZone { get => _bpfSellZone.Value; set => _bpfSellZone.Value = value; }

	/// <summary>
	/// Bandpass buy zone threshold.
	/// </summary>
	public decimal BpfBuyZone { get => _bpfBuyZone.Value; set => _bpfBuyZone.Value = value; }

	/// <summary>
	/// Reverse signals flag.
	/// </summary>
	public bool Reverse { get => _reverse.Value; set => _reverse.Value = value; }

	/// <summary>
	/// Start date filter.
	/// </summary>
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }

	/// <summary>
	/// Candle type used by strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public Combo220EmaBandpassFilterStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 2)
			.SetRange(1, 10)
			.SetDisplay("Fast EMA Length", "Length for fast EMA", "EMA Settings")
			.SetCanOptimize(true);

		_slowEmaLength = Param(nameof(SlowEmaLength), 20)
			.SetRange(5, 50)
			.SetDisplay("Slow EMA Length", "Length for slow EMA", "EMA Settings")
			.SetCanOptimize(true);

		_bpfLength = Param(nameof(BpfLength), 20)
			.SetRange(5, 50)
			.SetDisplay("Bandpass Length", "Length for bandpass filter", "BP Filter")
			.SetCanOptimize(true);

		_bpfDelta = Param(nameof(BpfDelta), 0.5m)
			.SetRange(0.1m, 2m)
			.SetDisplay("BP Delta", "Delta for bandpass filter", "BP Filter")
			.SetCanOptimize(true);

		_bpfSellZone = Param(nameof(BpfSellZone), 5m)
			.SetRange(1m, 10m)
			.SetDisplay("BP Sell Zone", "Sell zone for bandpass filter", "BP Filter")
			.SetCanOptimize(true);

		_bpfBuyZone = Param(nameof(BpfBuyZone), -5m)
			.SetRange(-10m, -1m)
			.SetDisplay("BP Buy Zone", "Buy zone for bandpass filter", "BP Filter")
			.SetCanOptimize(true);

		_reverse = Param(nameof(Reverse), false)
			.SetDisplay("Reverse Signals", "Trade opposite signals", "Misc");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2005, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Trading start date", "Time Filter");

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

		_beta = default;
		_gamma = default;
		_alpha = default;
		_bpPrev1 = default;
		_bpPrev2 = default;
		_hlPrev1 = default;
		_hlPrev2 = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_beta = (decimal)Math.Cos(Math.PI * (360.0 / BpfLength) / 180.0);
		_gamma = 1m / (decimal)Math.Cos(Math.PI * (720.0 * (double)BpfDelta / BpfLength) / 180.0);
		_alpha = _gamma - (decimal)Math.Sqrt((double)(_gamma * _gamma - 1m));

		var emaFast = new ExponentialMovingAverage { Length = FastEmaLength };
		var emaSlow = new ExponentialMovingAverage { Length = SlowEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(emaFast, emaSlow, ProcessIndicators)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessIndicators(ICandleMessage candle, decimal emaFastValue, decimal emaSlowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hl = (candle.HighPrice + candle.LowPrice) / 2m;

		if (_hlPrev1 == 0m)
		{
			_hlPrev1 = hl;
			return;
		}

		if (_hlPrev2 == 0m)
		{
			_hlPrev2 = _hlPrev1;
			_hlPrev1 = hl;
			return;
		}

		var prevBp1 = _bpPrev1;
		var prevBp2 = _bpPrev2;
		var bp = 0.5m * (1m - _alpha) * (hl - _hlPrev2) + _beta * (1m + _alpha) * prevBp1 - _alpha * prevBp2;
		var bpfSignal = bp > BpfSellZone ? 1m : bp < BpfBuyZone ? -1m : prevBp1;

		_bpPrev2 = prevBp1;
		_bpPrev1 = bp;
		_hlPrev2 = _hlPrev1;
		_hlPrev1 = hl;

		var emaSignal = emaFastValue > emaSlowValue ? 1m : emaFastValue < emaSlowValue ? -1m : 0m;
		var useTime = candle.OpenTime >= StartDate;
		var sig = emaSignal == bpfSignal && emaSignal != 0m && useTime ? emaSignal : 0m;

		if (Reverse && sig != 0m)
			sig = sig == 1m ? -1m : 1m;

		if (sig == 1m && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (sig == -1m && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		else if (sig == 0m)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));
		}
	}
}
