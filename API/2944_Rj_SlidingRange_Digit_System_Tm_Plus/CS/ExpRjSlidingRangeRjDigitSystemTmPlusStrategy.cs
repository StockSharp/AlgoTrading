using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Sliding range breakout strategy based on the Rj_SlidingRangeRj_Digit indicator.
/// </summary>
public class ExpRjSlidingRangeRjDigitSystemTmPlusStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExits;
	private readonly StrategyParam<bool> _enableSellExits;
	private readonly StrategyParam<bool> _useTimeExit;
	private readonly StrategyParam<int> _exitMinutes;
	private readonly StrategyParam<int> _upCalcPeriodRange;
	private readonly StrategyParam<int> _upCalcPeriodShift;
	private readonly StrategyParam<int> _upDigit;
	private readonly StrategyParam<int> _dnCalcPeriodRange;
	private readonly StrategyParam<int> _dnCalcPeriodShift;
	private readonly StrategyParam<int> _dnDigit;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<CandleSnapshot> _candleBuffer = new();
	private readonly List<int> _colorHistory = new();

	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private DateTimeOffset? _entryTime;

	public bool EnableBuyEntries { get => _enableBuyEntries.Value; set => _enableBuyEntries.Value = value; }
	public bool EnableSellEntries { get => _enableSellEntries.Value; set => _enableSellEntries.Value = value; }
	public bool EnableBuyExits { get => _enableBuyExits.Value; set => _enableBuyExits.Value = value; }
	public bool EnableSellExits { get => _enableSellExits.Value; set => _enableSellExits.Value = value; }
	public bool UseTimeExit { get => _useTimeExit.Value; set => _useTimeExit.Value = value; }
	public int ExitMinutes { get => _exitMinutes.Value; set => _exitMinutes.Value = value; }
	public int UpCalcPeriodRange { get => _upCalcPeriodRange.Value; set => _upCalcPeriodRange.Value = value; }
	public int UpCalcPeriodShift { get => _upCalcPeriodShift.Value; set => _upCalcPeriodShift.Value = value; }
	public int UpDigit { get => _upDigit.Value; set => _upDigit.Value = value; }
	public int DnCalcPeriodRange { get => _dnCalcPeriodRange.Value; set => _dnCalcPeriodRange.Value = value; }
	public int DnCalcPeriodShift { get => _dnCalcPeriodShift.Value; set => _dnCalcPeriodShift.Value = value; }
	public int DnDigit { get => _dnDigit.Value; set => _dnDigit.Value = value; }
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }
	public int StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
	public int TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExpRjSlidingRangeRjDigitSystemTmPlusStrategy()
	{
		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
			.SetDisplay("Enable Buy Entries", "Allow long entries", "Trading");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
			.SetDisplay("Enable Sell Entries", "Allow short entries", "Trading");

		_enableBuyExits = Param(nameof(EnableBuyExits), true)
			.SetDisplay("Enable Buy Exits", "Allow long exit signals", "Trading");

		_enableSellExits = Param(nameof(EnableSellExits), true)
			.SetDisplay("Enable Sell Exits", "Allow short exit signals", "Trading");

		_useTimeExit = Param(nameof(UseTimeExit), true)
			.SetDisplay("Use Time Exit", "Close positions after holding time", "Risk");

		_exitMinutes = Param(nameof(ExitMinutes), 1920)
			.SetGreaterOrEqualToZero()
			.SetDisplay("Exit Minutes", "Maximum holding time in minutes", "Risk");

		_upCalcPeriodRange = Param(nameof(UpCalcPeriodRange), 5)
			.SetGreaterThanZero()
			.SetDisplay("Upper Range", "Window for averaging highs", "Indicator");

		_upCalcPeriodShift = Param(nameof(UpCalcPeriodShift), 0)
			.SetGreaterOrEqualToZero()
			.SetDisplay("Upper Shift", "Shift applied to upper window", "Indicator");

		_upDigit = Param(nameof(UpDigit), 2)
			.SetGreaterOrEqualToZero()
			.SetDisplay("Upper Digits", "Rounding digits for upper band", "Indicator");

		_dnCalcPeriodRange = Param(nameof(DnCalcPeriodRange), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lower Range", "Window for averaging lows", "Indicator");

		_dnCalcPeriodShift = Param(nameof(DnCalcPeriodShift), 0)
			.SetGreaterOrEqualToZero()
			.SetDisplay("Lower Shift", "Shift applied to lower window", "Indicator");

		_dnDigit = Param(nameof(DnDigit), 2)
			.SetGreaterOrEqualToZero()
			.SetDisplay("Lower Digits", "Rounding digits for lower band", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterOrEqualToZero()
			.SetDisplay("Signal Bar", "Bar offset used for signals", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetGreaterOrEqualToZero()
			.SetDisplay("Stop Loss (pts)", "Stop loss in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetGreaterOrEqualToZero()
			.SetDisplay("Take Profit (pts)", "Take profit in points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for signals", "Data");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		AddCandle(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!TryCalculateBands(out var upperBand, out var lowerBand))
			return;

		var color = CalculateColor(candle, upperBand, lowerBand);
		_colorHistory.Add(color);

		TrimColorHistory();

		if (_colorHistory.Count <= SignalBar)
			return;

		var signalIndex = _colorHistory.Count - SignalBar - 1;
		if (signalIndex < 0)
			return;

		var signalColor = _colorHistory[signalIndex];
		var prevColor = signalIndex > 0 ? _colorHistory[signalIndex - 1] : (int?)null;

		var buyEntrySignal = EnableBuyEntries && IsUpperBreakout(signalColor) && !IsUpperBreakout(prevColor);
		var sellEntrySignal = EnableSellEntries && IsLowerBreakout(signalColor) && !IsLowerBreakout(prevColor);
		var buyExitSignal = EnableBuyExits && IsLowerBreakout(signalColor);
		var sellExitSignal = EnableSellExits && IsUpperBreakout(signalColor);

		var step = Security?.PriceStep ?? 1m;

		if (Position > 0)
		{
			var exitByTime = UseTimeExit && _entryTime.HasValue && candle.CloseTime - _entryTime.Value >= TimeSpan.FromMinutes(ExitMinutes);
			var exitByStop = _stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value;
			var exitByTarget = _takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value;
			if (exitByTime || buyExitSignal || exitByStop || exitByTarget)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}
		else if (Position < 0)
		{
			var exitByTime = UseTimeExit && _entryTime.HasValue && candle.CloseTime - _entryTime.Value >= TimeSpan.FromMinutes(ExitMinutes);
			var exitByStop = _stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value;
			var exitByTarget = _takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value;
			if (exitByTime || sellExitSignal || exitByStop || exitByTarget)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
			}
		}

		if (buyEntrySignal && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0)
			{
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
				_entryTime = candle.CloseTime;
				_stopLossPrice = StopLossPoints > 0 ? _entryPrice - StopLossPoints * step : null;
				_takeProfitPrice = TakeProfitPoints > 0 ? _entryPrice + TakeProfitPoints * step : null;
			}
		}
		else if (sellEntrySignal && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			if (volume > 0)
			{
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
				_entryTime = candle.CloseTime;
				_stopLossPrice = StopLossPoints > 0 ? _entryPrice + StopLossPoints * step : null;
				_takeProfitPrice = TakeProfitPoints > 0 ? _entryPrice - TakeProfitPoints * step : null;
			}
		}
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
		_entryTime = null;
	}

	private void AddCandle(ICandleMessage candle)
	{
		_candleBuffer.Insert(0, new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice));

		var limit = Math.Max(GetIndicatorRequirement(), SignalBar + 5);
		if (_candleBuffer.Count > limit)
			_candleBuffer.RemoveAt(_candleBuffer.Count - 1);
	}

	private void TrimColorHistory()
	{
		var limit = SignalBar + 5;
		if (_colorHistory.Count > limit)
			_colorHistory.RemoveRange(0, _colorHistory.Count - limit);
	}

	private bool TryCalculateBands(out decimal upperBand, out decimal lowerBand)
	{
		upperBand = 0m;
		lowerBand = 0m;

		if (UpCalcPeriodRange <= 0 || DnCalcPeriodRange <= 0)
			return false;

		var required = GetIndicatorRequirement();
		if (_candleBuffer.Count < required)
			return false;

		upperBand = CalculateAverageMaximum(UpCalcPeriodRange, UpCalcPeriodShift, UpDigit);
		lowerBand = CalculateAverageMinimum(DnCalcPeriodRange, DnCalcPeriodShift, DnDigit);
		return true;
	}

	private decimal CalculateAverageMaximum(int range, int shift, int digits)
	{
		var sum = 0m;
		var iii = shift + range - 1;
		var end = shift;

		while (iii >= end)
		{
			var max = decimal.MinValue;
			for (var offset = 0; offset < range; offset++)
			{
				var index = iii + offset;
				if (index >= _candleBuffer.Count)
					return 0m;

				var high = _candleBuffer[index].High;
				if (high > max)
					max = high;
			}

			sum += max;
			iii--;
		}

		var average = sum / range;
		return RoundToDigits(average, digits);
	}

	private decimal CalculateAverageMinimum(int range, int shift, int digits)
	{
		var sum = 0m;
		var iii = shift + range - 1;
		var end = shift;

		while (iii >= end)
		{
			var min = decimal.MaxValue;
			for (var offset = 0; offset < range; offset++)
			{
				var index = iii + offset;
				if (index >= _candleBuffer.Count)
					return 0m;

				var low = _candleBuffer[index].Low;
				if (low < min)
					min = low;
			}

			sum += min;
			iii--;
		}

		var average = sum / range;
		return RoundToDigits(average, digits);
	}

	private int GetIndicatorRequirement()
	{
		var upperRequirement = UpCalcPeriodShift + 2 * UpCalcPeriodRange;
		var lowerRequirement = DnCalcPeriodShift + 2 * DnCalcPeriodRange;
		return Math.Max(upperRequirement, lowerRequirement);
	}

	private static decimal RoundToDigits(decimal value, int digits)
	{
		if (digits <= 0)
			return value;

		var factor = (decimal)Math.Pow(10, digits);
		return factor == 0m ? value : Math.Round(value * factor, MidpointRounding.AwayFromZero) / factor;
	}

	private static bool IsUpperBreakout(int? color)
	{
		return color is 2 or 3;
	}

	private static bool IsLowerBreakout(int? color)
	{
		return color is 0 or 1;
	}

	private static int CalculateColor(ICandleMessage candle, decimal upperBand, decimal lowerBand)
	{
		const int neutralColor = 4;

		if (candle.ClosePrice > upperBand)
			return candle.ClosePrice >= candle.OpenPrice ? 3 : 2;

		if (candle.ClosePrice < lowerBand)
			return candle.ClosePrice <= candle.OpenPrice ? 0 : 1;

		return neutralColor;
	}

	private readonly record struct CandleSnapshot(decimal Open, decimal High, decimal Low, decimal Close);
}
