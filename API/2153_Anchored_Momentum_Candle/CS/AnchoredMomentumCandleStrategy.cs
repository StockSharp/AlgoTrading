namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on the Anchored Momentum Candle indicator.
/// Opens a long position when the indicator turns bullish and
/// opens a short position when it turns bearish.
/// </summary>
public class AnchoredMomentumCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _momPeriod;
	private readonly StrategyParam<int> _smoothPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private AnchoredMomentumCandle _indicator;
	private decimal? _prevColor;

	/// <summary>
	/// Period for the simple moving averages.
	/// </summary>
	public int MomPeriod
	{
		get => _momPeriod.Value;
		set => _momPeriod.Value = value;
	}

	/// <summary>
	/// Period for the exponential moving averages.
	/// </summary>
	public int SmoothPeriod
	{
		get => _smoothPeriod.Value;
		set => _smoothPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref=\"AnchoredMomentumCandleStrategy\"/>.
	/// </summary>
	public AnchoredMomentumCandleStrategy()
	{
		_momPeriod = Param(nameof(MomPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay(\"Momentum Period\", \"SMA length\", \"Parameters\")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_smoothPeriod = Param(nameof(SmoothPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay(\"Smooth Period\", \"EMA length\", \"Parameters\")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay(\"Candle Type\", \"Working timeframe\", \"General\");
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
		_indicator?.Reset();
		_prevColor = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_indicator = new AnchoredMomentumCandle
		{
			MomPeriod = MomPeriod,
			SmoothPeriod = SmoothPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_indicator, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal color)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_indicator.IsFormed)
			return;

		if (_prevColor == null)
		{
			_prevColor = color;
			return;
		}

		if (color == 2m && _prevColor != 2m)
		{
			if (Position < 0)
				ClosePosition();
			if (Position <= 0)
				BuyMarket();
		}
		else if (color == 0m && _prevColor != 0m)
		{
			if (Position > 0)
				ClosePosition();
			if (Position >= 0)
				SellMarket();
		}

		_prevColor = color;
	}

	private class AnchoredMomentumCandle : Indicator<ICandleMessage>
	{
		public int MomPeriod { get; set; } = 8;
		public int SmoothPeriod { get; set; } = 6;

		private readonly Queue<decimal> _openQueue = new();
		private readonly Queue<decimal> _closeQueue = new();
		private decimal _sumOpen;
		private decimal _sumClose;
		private decimal _emaOpen;
		private decimal _emaClose;
		private bool _emaOpenInit;
		private bool _emaCloseInit;
		private decimal _lastColor = 1m;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			if (!input.IsFinal)
				return new DecimalIndicatorValue(this, _lastColor, input.Time);

			var open = candle.OpenPrice;
			var close = candle.ClosePrice;

			_sumOpen += open;
			_openQueue.Enqueue(open);
			if (_openQueue.Count > MomPeriod)
				_sumOpen -= _openQueue.Dequeue();

			_sumClose += close;
			_closeQueue.Enqueue(close);
			if (_closeQueue.Count > MomPeriod)
				_sumClose -= _closeQueue.Dequeue();

			var k = 2m / (SmoothPeriod + 1);

			if (!_emaOpenInit)
			{
				_emaOpen = open;
				_emaOpenInit = true;
			}
			else
				_emaOpen = k * open + (1 - k) * _emaOpen;

			if (!_emaCloseInit)
			{
				_emaClose = close;
				_emaCloseInit = true;
			}
			else
				_emaClose = k * close + (1 - k) * _emaClose;

			if (_openQueue.Count < MomPeriod || _closeQueue.Count < MomPeriod)
			{
				IsFormed = false;
				_lastColor = 1m;
				return new DecimalIndicatorValue(this, _lastColor, input.Time);
			}

			var smaOpen = _sumOpen / MomPeriod;
			var smaClose = _sumClose / MomPeriod;

			var openMomentum = smaOpen == 0m ? 0m : 100m * (_emaOpen / smaOpen - 1m);
			var closeMomentum = smaClose == 0m ? 0m : 100m * (_emaClose / smaClose - 1m);

			var color = openMomentum < closeMomentum ? 2m : openMomentum > closeMomentum ? 0m : 1m;

			IsFormed = true;
			_lastColor = color;
			return new DecimalIndicatorValue(this, color, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_openQueue.Clear();
			_closeQueue.Clear();
			_sumOpen = 0m;
			_sumClose = 0m;
			_emaOpen = 0m;
			_emaClose = 0m;
			_emaOpenInit = false;
			_emaCloseInit = false;
			_lastColor = 1m;
		}
	}
}
