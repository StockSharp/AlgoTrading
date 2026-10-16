using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades the documented inverse RVI/signal-line cross, with separate entry/exit permissions.
/// </summary>
public class ColorZerolagRviStrategy : Strategy
{
	private readonly StrategyParam<int> _rviLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _sellClose;

	private decimal? _prevRvi;
	private decimal? _prevSignal;

	public int RviLength { get => _rviLength.Value; set => _rviLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }

	public ColorZerolagRviStrategy()
	{
		_rviLength = Param(nameof(RviLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RVI Length", "RVI calculation period", "Indicator");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "RVI signal line period", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Buy Open", "Allow long entries", "Trading");
		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Buy Close", "Close longs on a sell signal", "Trading");
		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Sell Open", "Allow short entries", "Trading");
		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Sell Close", "Close shorts on a buy signal", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRvi = null;
		_prevSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var rvi = new RelativeVigorIndex();
		rvi.Average.Length = RviLength;
		rvi.Signal.Length = SignalLength;

		SubscribeCandles(CandleType)
			.BindEx(rvi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rviValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var value = (IRelativeVigorIndexValue)rviValue;
		if (value.Average is not decimal rvi || value.Signal is not decimal signal)
			return;

		if (_prevRvi is null || _prevSignal is null)
		{
			_prevRvi = rvi;
			_prevSignal = signal;
			return;
		}

		var crossUp = _prevRvi < _prevSignal && rvi > signal;
		var crossDown = _prevRvi > _prevSignal && rvi < signal;

		// Downward RVI crossover is the documented buy signal; upward is sell.
		// A disabled opposite-position close also suppresses opening the reverse
		// side, preventing an accidental netting trade.
		if (crossDown)
		{
			var position = Position;
			if (position < 0 && SellClose)
				BuyMarket(-position);
			if (BuyOpen && (position == 0 || (position < 0 && SellClose)))
				BuyMarket(Volume);
		}
		else if (crossUp)
		{
			var position = Position;
			if (position > 0 && BuyClose)
				SellMarket(position);
			if (SellOpen && (position == 0 || (position > 0 && BuyClose)))
				SellMarket(Volume);
		}

		_prevRvi = rvi;
		_prevSignal = signal;
	}
}
