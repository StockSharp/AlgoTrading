
using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Color Bears Gap indicator.
/// Opens a long position when the indicator crosses below zero and a short position when it crosses above zero.
/// Closes existing opposite positions on signal change.
/// </summary>
public class ColorBearsGapStrategy : Strategy
{
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<DataType> _candleType;

	private ColorBearsGapIndicator _indicator = default!;
	private decimal _prevValue;

	/// <summary>
	/// First smoothing length.
	/// </summary>
	public int Length1 { get => _length1.Value; set => _length1.Value = value; }

	/// <summary>
	/// Second smoothing length.
	/// </summary>
	public int Length2 { get => _length2.Value; set => _length2.Value = value; }

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }

	/// <summary>
	/// Allow closing long positions on opposite signal.
	/// </summary>
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }

	/// <summary>
	/// Allow closing short positions on opposite signal.
	/// </summary>
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="ColorBearsGapStrategy"/>.
	/// </summary>
	public ColorBearsGapStrategy()
	{
		_length1 = Param(nameof(Length1), 12)
			.SetDisplay("Length 1", "First smoothing length", "Parameters")
			.SetRange(5, 50)
			.SetCanOptimize(true);

		_length2 = Param(nameof(Length2), 5)
			.SetDisplay("Length 2", "Second smoothing length", "Parameters")
			.SetRange(2, 20)
			.SetCanOptimize(true);

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Parameters");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Parameters");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Buy Close", "Allow closing long positions", "Parameters");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Sell Close", "Allow closing short positions", "Parameters");

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(8)))
			.SetDisplay("Candle Type", "Timeframe for candle subscription", "Parameters");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_indicator = new ColorBearsGapIndicator
		{
			Length1 = Length1,
			Length2 = Length2
		};

		_prevValue = 0m;

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

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_indicator?.Reset();
		_prevValue = 0m;
	}

	private void ProcessCandle(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevSignal = _prevValue > 0m ? 1 : _prevValue < 0m ? -1 : 0;
		var signal = value > 0m ? 1 : value < 0m ? -1 : 0;

		if (prevSignal == 1 && signal <= 0)
		{
			if (SellClose && Position < 0)
				BuyMarket();
			if (BuyOpen && Position <= 0)
				BuyMarket();
		}
		else if (prevSignal == -1 && signal >= 0)
		{
			if (BuyClose && Position > 0)
				SellMarket();
			if (SellOpen && Position >= 0)
				SellMarket();
		}

		_prevValue = value;
	}

	private class ColorBearsGapIndicator : Indicator<ICandleMessage>
	{
		public int Length1 { get; set; }
		public int Length2 { get; set; }

		private readonly ExponentialMovingAverage _emaClose = new();
		private readonly ExponentialMovingAverage _emaOpen = new();
		private readonly ExponentialMovingAverage _emaBullsC = new();
		private readonly ExponentialMovingAverage _emaBullsO = new();
		private decimal? _prevXBullsC;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();

			var smoothClose = _emaClose.Process(input.Time, candle.ClosePrice).GetValue<decimal>();
			var smoothOpen = _emaOpen.Process(input.Time, candle.OpenPrice).GetValue<decimal>();

			var bullsC = candle.HighPrice - smoothClose;
			var bullsO = candle.HighPrice - smoothOpen;

			var xbullsC = _emaBullsC.Process(input.Time, bullsC).GetValue<decimal>();
			var xbullsO = _emaBullsO.Process(input.Time, bullsO).GetValue<decimal>();

			var value = 0m;
			if (_prevXBullsC != null)
				value = xbullsO - _prevXBullsC.Value;

			_prevXBullsC = xbullsC;

			IsFormed = _emaBullsO.IsFormed && _prevXBullsC != null;

			return new DecimalIndicatorValue(this, value, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_emaClose.Reset();
			_emaOpen.Reset();
			_emaBullsC.Reset();
			_emaBullsO.Reset();
			_prevXBullsC = null;
		}
	}
}
