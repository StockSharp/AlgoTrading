using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ALMA based strategy with EMA crossover.
/// Goes long when ALMA crosses above slow EMA, short when crosses below.
/// </summary>
public class AlmaOptimizedStrategy : Strategy
{
	private readonly StrategyParam<int> _almaLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private int _barIndex;
	private int _lastTradeBar;
	private decimal _prevAlma;
	private decimal _prevEma;

	/// <summary>
	/// ALMA period.
	/// </summary>
	public int AlmaLength
	{
		get => _almaLength.Value;
		set => _almaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AlmaOptimizedStrategy"/>.
	/// </summary>
	public AlmaOptimizedStrategy()
	{
		_almaLength = Param(nameof(AlmaLength), 9)
			.SetDisplay("ALMA Length", "ALMA period", "Indicator");

		_emaLength = Param(nameof(EmaLength), 26)
			.SetDisplay("EMA Length", "Slow EMA period", "Indicator");

		_cooldownBars = Param(nameof(CooldownBars), 40)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_barIndex = 0;
		_lastTradeBar = 0;
		_prevAlma = 0;
		_prevEma = 0;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var alma = new ArnaudLegouxMovingAverage { Length = AlmaLength, Offset = 0.65m, Sigma = 6 };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(alma, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, alma);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal almaValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		if (_prevAlma == 0 || _prevEma == 0)
		{
			_prevAlma = almaValue;
			_prevEma = emaValue;
			return;
		}

		var cooldownOk = _barIndex - _lastTradeBar > CooldownBars;

		// ALMA crosses above EMA -> buy signal
		var crossOver = _prevAlma <= _prevEma && almaValue > emaValue;
		// ALMA crosses below EMA -> sell signal
		var crossUnder = _prevAlma >= _prevEma && almaValue < emaValue;

		if (crossOver && Position <= 0 && cooldownOk)
		{
			BuyMarket();
			_lastTradeBar = _barIndex;
		}
		else if (crossUnder && Position >= 0 && cooldownOk)
		{
			SellMarket();
			_lastTradeBar = _barIndex;
		}

		_prevAlma = almaValue;
		_prevEma = emaValue;
	}
}
