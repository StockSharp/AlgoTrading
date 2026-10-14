using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 5 EMA strategy.
/// A candle whose close and high are below the EMA marks a long setup, one whose close and low are above it marks a short setup.
/// If price breaks the signal candle's high (long) or low (short) within the next three candles and outside the block window, the
/// strategy enters in that direction with the stop at the signal candle's opposite extreme and the target at TargetRR times the risk.
/// Open positions are closed at ExitHour:ExitMinute.
/// </summary>
public class FiveEmaStrategy : Strategy
{
	private const int _signalBars = 3;

	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _targetRR;
	private readonly StrategyParam<int> _exitHour;
	private readonly StrategyParam<int> _exitMinute;
	private readonly StrategyParam<int> _blockStartHour;
	private readonly StrategyParam<int> _blockStartMinute;
	private readonly StrategyParam<int> _blockEndHour;
	private readonly StrategyParam<int> _blockEndMinute;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _signalHigh;
	private decimal? _signalLow;
	private bool _signalIsLong;
	private int _barsSinceSignal;
	private decimal? _stopPrice;
	private decimal? _targetPrice;

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Reward to risk ratio of the target.
	/// </summary>
	public decimal TargetRR
	{
		get => _targetRR.Value;
		set => _targetRR.Value = value;
	}

	/// <summary>
	/// Hour of the forced exit.
	/// </summary>
	public int ExitHour
	{
		get => _exitHour.Value;
		set => _exitHour.Value = value;
	}

	/// <summary>
	/// Minute of the forced exit.
	/// </summary>
	public int ExitMinute
	{
		get => _exitMinute.Value;
		set => _exitMinute.Value = value;
	}

	/// <summary>
	/// Hour the entry block starts.
	/// </summary>
	public int BlockStartHour
	{
		get => _blockStartHour.Value;
		set => _blockStartHour.Value = value;
	}

	/// <summary>
	/// Minute the entry block starts.
	/// </summary>
	public int BlockStartMinute
	{
		get => _blockStartMinute.Value;
		set => _blockStartMinute.Value = value;
	}

	/// <summary>
	/// Hour the entry block ends.
	/// </summary>
	public int BlockEndHour
	{
		get => _blockEndHour.Value;
		set => _blockEndHour.Value = value;
	}

	/// <summary>
	/// Minute the entry block ends.
	/// </summary>
	public int BlockEndMinute
	{
		get => _blockEndMinute.Value;
		set => _blockEndMinute.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FiveEmaStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "EMA");

		_targetRR = Param(nameof(TargetRR), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Target R:R", "Reward to risk ratio of the target", "Risk");

		_exitHour = Param(nameof(ExitHour), 15)
			.SetRange(0, 23)
			.SetDisplay("Exit Hour", "Hour of the forced exit", "Time");

		_exitMinute = Param(nameof(ExitMinute), 30)
			.SetRange(0, 59)
			.SetDisplay("Exit Minute", "Minute of the forced exit", "Time");

		_blockStartHour = Param(nameof(BlockStartHour), 15)
			.SetRange(0, 23)
			.SetDisplay("Block Start Hour", "Hour the entry block starts", "Time");

		_blockStartMinute = Param(nameof(BlockStartMinute), 0)
			.SetRange(0, 59)
			.SetDisplay("Block Start Minute", "Minute the entry block starts", "Time");

		_blockEndHour = Param(nameof(BlockEndHour), 15)
			.SetRange(0, 23)
			.SetDisplay("Block End Hour", "Hour the entry block ends", "Time");

		_blockEndMinute = Param(nameof(BlockEndMinute), 30)
			.SetRange(0, 59)
			.SetDisplay("Block End Minute", "Minute the entry block ends", "Time");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ResetState()
	{
		_signalHigh = null;
		_signalLow = null;
		_signalIsLong = false;
		_barsSinceSignal = 0;
		_stopPrice = null;
		_targetPrice = null;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!emaValue.IsFormed)
			return;

		var ema = emaValue.ToDecimal();

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		if (_signalHigh != null)
		{
			_barsSinceSignal++;
			if (_barsSinceSignal > _signalBars)
				_signalHigh = _signalLow = null;
		}

		var signalHigh = _signalHigh;
		var signalLow = _signalLow;
		var signalIsLong = _signalIsLong;

		// A new signal candle replaces the pending one and can only be broken by later candles.
		var newSignal = true;
		if (close < ema && high < ema)
			SetSignal(high, low, true);
		else if (close > ema && low > ema)
			SetSignal(high, low, false);
		else
			newSignal = false;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var tod = candle.OpenTime.TimeOfDay;
		var exitTime = new TimeSpan(ExitHour, ExitMinute, 0);
		var frame = CandleType.Arg is TimeSpan tf ? tf : TimeSpan.Zero;

		if (Position != 0 && tod <= exitTime && exitTime < tod + frame)
		{
			ClosePosition();
			return;
		}

		if (Position > 0 && _stopPrice is decimal longStop && _targetPrice is decimal longTarget)
		{
			if (low <= longStop || high >= longTarget)
			{
				ClosePosition();
				return;
			}
		}
		else if (Position < 0 && _stopPrice is decimal shortStop && _targetPrice is decimal shortTarget)
		{
			if (high >= shortStop || low <= shortTarget)
			{
				ClosePosition();
				return;
			}
		}

		if (signalHigh is not decimal sHigh || signalLow is not decimal sLow)
			return;

		var blockStart = new TimeSpan(BlockStartHour, BlockStartMinute, 0);
		var blockEnd = new TimeSpan(BlockEndHour, BlockEndMinute, 0);
		if (tod >= blockStart && tod < blockEnd)
			return;

		var risk = sHigh - sLow;
		if (risk <= 0)
			return;

		if (signalIsLong && high > sHigh && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = sLow;
			_targetPrice = sHigh + risk * TargetRR;
			if (!newSignal)
				_signalHigh = _signalLow = null;
		}
		else if (!signalIsLong && low < sLow && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = sHigh;
			_targetPrice = sLow - risk * TargetRR;
			if (!newSignal)
				_signalHigh = _signalLow = null;
		}
	}

	private void SetSignal(decimal high, decimal low, bool isLong)
	{
		_signalHigh = high;
		_signalLow = low;
		_signalIsLong = isLong;
		_barsSinceSignal = 0;
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);

		_stopPrice = null;
		_targetPrice = null;
	}
}
