namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class ScalpingStrategyByTradingConTotoStrategy : Strategy
{
	private const int RightBars = 10;

	private readonly StrategyParam<int> _pivot;
	private readonly StrategyParam<decimal> _pips;
	private readonly StrategyParam<decimal> _spread;
	private readonly StrategyParam<string> _session;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _highBuffer = Array.Empty<decimal>();
	private decimal[] _lowBuffer = Array.Empty<decimal>();
	private int _barIndex;

	private decimal? _auxHigh;
	private int _auxHighBar;
	private decimal? _auxLow;
	private int _auxLowBar;

	private decimal? _highLineStart;
	private int _highLineBar;
	private decimal _highLineSlope;

	private decimal? _lowLineStart;
	private int _lowLineBar;
	private decimal _lowLineSlope;

	private decimal _prevFast;
	private decimal _prevSlow;

	public int Pivot { get => _pivot.Value; set => _pivot.Value = value; }
	public decimal Pips { get => _pips.Value; set => _pips.Value = value; }
	public decimal Spread { get => _spread.Value; set => _spread.Value = value; }
	public string Session { get => _session.Value; set => _session.Value = value; }
	public bool EnableLong { get => _enableLong.Value; set => _enableLong.Value = value; }
	public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ScalpingStrategyByTradingConTotoStrategy()
	{
		_pivot = Param(nameof(Pivot), 16)
			.SetGreaterThanZero()
			.SetDisplay("Pivot", "Left bars for pivot detection", "Parameters");

		_pips = Param(nameof(Pips), 64m)
			.SetDisplay("Pips", "Stop loss and take profit size", "Parameters")
			.SetNotNegative();

		_spread = Param(nameof(Spread), 0m)
			.SetDisplay("Spread", "Additional spread in ticks", "Parameters")
			.SetNotNegative();

		_session = Param(nameof(Session), "0830-0930")
			.SetDisplay("Session", "Trading session", "General");

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long trades", "General");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_highBuffer = Array.Empty<decimal>();
		_lowBuffer = Array.Empty<decimal>();
		_barIndex = 0;
		_auxHigh = _auxLow = null;
		_auxHighBar = _auxLowBar = 0;
		_highLineStart = _lowLineStart = null;
		_prevFast = _prevSlow = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var size = Pivot + RightBars + 1;
		_highBuffer = new decimal[size];
		_lowBuffer = new decimal[size];
		_barIndex = 0;

		var slowEma = new ExponentialMovingAverage { Length = 100 };
		var fastEma = new ExponentialMovingAverage { Length = 25 };

		var sub = SubscribeCandles(CandleType);
		sub.Bind(slowEma, fastEma, ProcessCandle)
			.Start();

		StartProtection(new Unit(Pips + Spread, UnitTypes.Step), new Unit(Pips, UnitTypes.Step));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, slowEma);
			DrawIndicator(area, fastEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal slowEma, decimal fastEma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var high = candle.HighPrice;
		var low = candle.LowPrice;

		var idx = _barIndex % _highBuffer.Length;
		_highBuffer[idx] = high;
		_lowBuffer[idx] = low;
		_barIndex++;

		if (_barIndex >= _highBuffer.Length)
		{
			var centerIndex = (_barIndex - RightBars - 1) % _highBuffer.Length;
			var pivotHigh = _highBuffer[centerIndex];
			var pivotLow = _lowBuffer[centerIndex];

			var isPivotHigh = true;
			var isPivotLow = true;

			for (var i = 1; i <= Pivot; i++)
			{
				var left = (centerIndex - i + _highBuffer.Length) % _highBuffer.Length;
				if (_highBuffer[left] >= pivotHigh)
					isPivotHigh = false;
				if (_lowBuffer[left] <= pivotLow)
					isPivotLow = false;
			}

			for (var i = 1; i <= RightBars; i++)
			{
				var right = (centerIndex + i) % _highBuffer.Length;
				if (_highBuffer[right] > pivotHigh)
					isPivotHigh = false;
				if (_lowBuffer[right] < pivotLow)
					isPivotLow = false;
			}

			var pivotBar = _barIndex - RightBars - 1;

			if (isPivotHigh && fastEma > slowEma)
			{
				if (_auxHigh is decimal prev && pivotHigh < prev)
				{
					_highLineStart = prev;
					_highLineBar = _auxHighBar;
					_highLineSlope = (pivotHigh - prev) / (pivotBar - _auxHighBar);
				}
				_auxHigh = pivotHigh;
				_auxHighBar = pivotBar;
			}

			if (isPivotLow && fastEma < slowEma)
			{
				if (_auxLow is decimal prevL && pivotLow > prevL)
				{
					_lowLineStart = prevL;
					_lowLineBar = _auxLowBar;
					_lowLineSlope = (pivotLow - prevL) / (pivotBar - _auxLowBar);
				}
				_auxLow = pivotLow;
				_auxLowBar = pivotBar;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFast = fastEma;
			_prevSlow = slowEma;
			return;
		}

		ParseSession(Session, out var start, out var end);
		var t = candle.OpenTime.TimeOfDay;
		var inSession = t >= start && t <= end;
		var currentBar = _barIndex - 1;

		if (fastEma > slowEma && _highLineStart is decimal lineStart && EnableLong && inSession && Position <= 0)
		{
			var price = lineStart + _highLineSlope * (currentBar - _highLineBar);
			if (candle.ClosePrice > price && candle.OpenPrice < price)
			{
				BuyMarket();
				_highLineStart = null;
			}
		}

		if (fastEma < slowEma && _lowLineStart is decimal lineStart2 && EnableShort && inSession && Position >= 0)
		{
			var price = lineStart2 + _lowLineSlope * (currentBar - _lowLineBar);
			if (candle.ClosePrice < price && candle.OpenPrice > price)
			{
				SellMarket();
				_lowLineStart = null;
			}
		}

		var crossed = (_prevFast <= _prevSlow && fastEma > slowEma) || (_prevFast >= _prevSlow && fastEma < slowEma);
		if (crossed)
		{
			_auxHigh = _auxLow = null;
			_auxHighBar = _auxLowBar = 0;
			_highLineStart = _lowLineStart = null;
		}

		_prevFast = fastEma;
		_prevSlow = slowEma;
	}

	private static void ParseSession(string input, out TimeSpan start, out TimeSpan end)
	{
		start = TimeSpan.Zero;
		end = TimeSpan.FromHours(24);
		if (string.IsNullOrWhiteSpace(input))
			return;
		var parts = input.Split('-');
		if (parts.Length != 2)
			return;
		TimeSpan.TryParseExact(parts[0], "hhmm", null, out start);
		TimeSpan.TryParseExact(parts[1], "hhmm", null, out end);
	}
}
