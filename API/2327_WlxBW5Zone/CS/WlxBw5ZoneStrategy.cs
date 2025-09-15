using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bill Williams zone strategy based on Awesome and Accelerator oscillators.
/// Opens or reverses positions after five consecutive oscillator moves.
/// </summary>
public class WlxBw5ZoneStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _direct;
	private readonly StrategyParam<int> _signalBar;

	private AwesomeOscillator _ao;
	private AcceleratorOscillator _ac;

	private decimal? _ao0, _ao1, _ao2, _ao3, _ao4;
	private decimal? _ac0, _ac1, _ac2, _ac3, _ac4;
	private bool _flagUp;
	private bool _flagDown;

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Direction flag. If false signals are reversed.
	/// </summary>
	public bool Direct { get => _direct.Value; set => _direct.Value = value; }

	/// <summary>
	/// Signal bar shift.
	/// </summary>
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="WlxBw5ZoneStrategy"/>.
	/// </summary>
	public WlxBw5ZoneStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_direct = Param(nameof(Direct), true)
			.SetDisplay("Direct", "Use direct signals", "General");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetRange(0, 5)
			.SetDisplay("Signal Bar", "Bar shift for signals", "General");
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ao0 = _ao1 = _ao2 = _ao3 = _ao4 = null;
		_ac0 = _ac1 = _ac2 = _ac3 = _ac4 = null;
		_flagUp = false;
		_flagDown = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ao = new AwesomeOscillator();
		_ac = new AcceleratorOscillator();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ao, _ac, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ao, decimal ac)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// shift previous values
		_ao4 = _ao3; _ao3 = _ao2; _ao2 = _ao1; _ao1 = _ao0; _ao0 = ao;
		_ac4 = _ac3; _ac3 = _ac2; _ac2 = _ac1; _ac1 = _ac0; _ac0 = ac;

		if (_ao4 is null || _ac4 is null)
			return; // not enough data

		var isUpSeq = _ao0 > _ao1 && _ao1 > _ao2 && _ao2 > _ao3 && _ao3 > _ao4 &&
			_ac0 > _ac1 && _ac1 > _ac2 && _ac2 > _ac3 && _ac3 > _ac4;

		var isDownSeq = _ao0 < _ao1 && _ao1 < _ao2 && _ao2 < _ao3 && _ao3 < _ao4 &&
			_ac0 < _ac1 && _ac1 < _ac2 && _ac2 < _ac3 && _ac3 < _ac4;

		if (!_flagUp && isUpSeq)
		{
			if (Direct)
			{
				if (Position <= 0)
					BuyMarket();
			}
			else
			{
				if (Position >= 0)
					SellMarket();
			}

			_flagUp = true;
		}

		if (!_flagDown && isDownSeq)
		{
			if (Direct)
			{
				if (Position >= 0)
					SellMarket();
			}
			else
			{
				if (Position <= 0)
					BuyMarket();
			}

			_flagDown = true;
		}

		if (_ao0 <= _ao1 || _ac0 <= _ac1)
			_flagUp = false;

		if (_ao0 >= _ao1 || _ac0 >= _ac1)
			_flagDown = false;
	}
}