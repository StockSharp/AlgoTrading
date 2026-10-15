using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on a custom XdinMA indicator derived from two moving averages.
/// The line is calculated as <c>ma_main * 2 - ma_plus</c> and orders are generated when its slope changes direction.
/// </summary>
public class ColorXdinMAStrategy : Strategy
{
	private readonly StrategyParam<int> _mainLength;
	private readonly StrategyParam<int> _plusLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalCooldownBars;

	private SMA _mainMa = null!;
	private SMA _plusMa = null!;

	private bool _isInitialized;
	private decimal _prev;
	private decimal _prevPrev;
	private int _cooldownRemaining;

	/// <summary>
	/// Period of the main moving average.
	/// </summary>
	public int MainLength
	{
		get => _mainLength.Value;
		set => _mainLength.Value = value;
	}

	/// <summary>
	/// Period of the additional moving average.
	/// </summary>
	public int PlusLength
	{
		get => _plusLength.Value;
		set => _plusLength.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of closed candles to wait before a new reversal trade.
	/// </summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ColorXdinMAStrategy"/>.
	/// </summary>
	public ColorXdinMAStrategy()
	{
		_mainLength = Param(nameof(MainLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Main MA Length", "Period of the main moving average", "Indicator")
			
			.SetOptimize(5, 20, 1);

		_plusLength = Param(nameof(PlusLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Additional MA Length", "Period of the additional moving average", "Indicator")
			
			.SetOptimize(10, 40, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_signalCooldownBars = Param(nameof(SignalCooldownBars), 3)
			.SetNotNegative()
			.SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new reversal", "General");
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

		_mainMa = null!;
		_plusMa = null!;
		_isInitialized = false;
		_prev = 0m;
		_prevPrev = 0m;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_cooldownRemaining = 0;

		_mainMa = new SMA { Length = MainLength };
		_plusMa = new SMA { Length = PlusLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_mainMa, _plusMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _mainMa);
			DrawIndicator(area, _plusMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal main, decimal plus)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var xdin = main * 2m - plus;

		if (!_isInitialized)
		{
			if (_mainMa.IsFormed && _plusMa.IsFormed)
			{
				_prevPrev = _prev = xdin;
				_isInitialized = true;
			}
			return;
		}

		if (_cooldownRemaining == 0 && _prev < _prevPrev && xdin > _prev && Position <= 0)
		{
			BuyMarket(Volume + (Position < 0 ? -Position : 0m));
			_cooldownRemaining = SignalCooldownBars;
		}
		else if (_cooldownRemaining == 0 && _prev > _prevPrev && xdin < _prev && Position >= 0)
		{
			SellMarket(Volume + (Position > 0 ? Position : 0m));
			_cooldownRemaining = SignalCooldownBars;
		}

		_prevPrev = _prev;
		_prev = xdin;
	}
}
