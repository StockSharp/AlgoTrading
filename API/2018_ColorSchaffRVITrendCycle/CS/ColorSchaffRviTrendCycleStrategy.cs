using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trading strategy based on Color Schaff RVI Trend Cycle indicator.
/// </summary>
public class ColorSchaffRviTrendCycleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastRviLength;
	private readonly StrategyParam<int> _slowRviLength;
	private readonly StrategyParam<int> _cycleLength;
	private readonly StrategyParam<int> _highLevel;
	private readonly StrategyParam<int> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeVigorIndex _fastRvi;
	private RelativeVigorIndex _slowRvi;

	private decimal[] _macd;
	private decimal[] _st;
	private decimal[] _stc;
	private int _index;
	private int _filled;
	private bool _stReady;
	private bool _stcReady;
	private decimal _prevSt;
	private decimal _prevStc;

	/// <summary>
	/// Fast RVI period.
	/// </summary>
	public int FastRviLength
	{
		get => _fastRviLength.Value;
		set => _fastRviLength.Value = value;
	}

	/// <summary>
	/// Slow RVI period.
	/// </summary>
	public int SlowRviLength
	{
		get => _slowRviLength.Value;
		set => _slowRviLength.Value = value;
	}

	/// <summary>
	/// Cycle length for stochastic calculations.
	/// </summary>
	public int CycleLength
	{
		get => _cycleLength.Value;
		set => _cycleLength.Value = value;
	}

	/// <summary>
	/// Upper threshold for STC.
	/// </summary>
	public int HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold for STC.
	/// </summary>
	public int LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	/// <summary>
	/// Candle type used for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ColorSchaffRviTrendCycleStrategy"/>.
	/// </summary>
	public ColorSchaffRviTrendCycleStrategy()
	{
		_fastRviLength = Param(nameof(FastRviLength), 23)
			.SetGreaterThanZero()
			.SetDisplay("Fast RVI Length", "Period for fast RVI", "General")
			.SetCanOptimize(true);

		_slowRviLength = Param(nameof(SlowRviLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow RVI Length", "Period for slow RVI", "General")
			.SetCanOptimize(true);

		_cycleLength = Param(nameof(CycleLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Cycle", "Length of stochastic cycle", "General")
			.SetCanOptimize(true);

		_highLevel = Param(nameof(HighLevel), 60)
			.SetDisplay("High Level", "Upper threshold", "General")
			.SetCanOptimize(true);

		_lowLevel = Param(nameof(LowLevel), -60)
			.SetDisplay("Low Level", "Lower threshold", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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

		_fastRvi = default;
		_slowRvi = default;
		_macd = default;
		_st = default;
		_stc = default;
		_index = 0;
		_filled = 0;
		_stReady = false;
		_stcReady = false;
		_prevSt = 0m;
		_prevStc = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastRvi = new RelativeVigorIndex { Length = FastRviLength };
		_slowRvi = new RelativeVigorIndex { Length = SlowRviLength };

		_macd = new decimal[CycleLength];
		_st = new decimal[CycleLength];
		_stc = new decimal[CycleLength];

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastRvi);
			DrawIndicator(area, _slowRvi);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastValue = _fastRvi.Process(candle);
		var slowValue = _slowRvi.Process(candle);

		if (!fastValue.IsFinal || !slowValue.IsFinal)
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();
		var macd = fast - slow;

		_macd[_index] = macd;

		var len = Math.Max(_filled, 1);
		decimal minMacd = _macd[0], maxMacd = _macd[0];
		for (var i = 1; i < len; i++)
		{
			var v = _macd[i];
			if (v < minMacd)
				minMacd = v;
			if (v > maxMacd)
				maxMacd = v;
		}

		var st = (maxMacd - minMacd) != 0 ? (macd - minMacd) / (maxMacd - minMacd) * 100m : _prevSt;
		if (_stReady)
			st = 0.5m * (st - _prevSt) + _prevSt;
		else
			_stReady = true;

		_prevSt = st;
		_st[_index] = st;

		decimal minSt = _st[0], maxSt = _st[0];
		for (var i = 1; i < len; i++)
		{
			var v = _st[i];
			if (v < minSt)
				minSt = v;
			if (v > maxSt)
				maxSt = v;
		}

		var stc = (maxSt - minSt) != 0 ? (st - minSt) / (maxSt - minSt) * 200m - 100m : _prevStc;
		if (_stcReady)
			stc = 0.5m * (stc - _prevStc) + _prevStc;
		else
			_stcReady = true;

		_prevStc = stc;
		_stc[_index] = stc;

		_index++;
		if (_index >= _macd.Length)
			_index = 0;
		if (_filled < _macd.Length)
			_filled++;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var prevIndex = (_index - 1 + _stc.Length) % _stc.Length;
		var prevStcValue = _stc[prevIndex];
		var delta = stc - prevStcValue;

		if (stc > HighLevel && delta >= 0 && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
		}
		else if (stc < LowLevel && delta <= 0 && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
		}
	}
}
