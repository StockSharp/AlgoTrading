using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bill Williams Fractal Breakout strategy filtered by Alligator alignment.
/// Buys on up-fractal breakout when price is above alligator teeth,
/// sells on down-fractal breakout when price is below alligator teeth.
/// Exits on opposite signal (reverse position).
/// </summary>
public class FTBillWillamsTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _lipsPeriod;
	private readonly StrategyParam<int> _fractalLen;

	private SMA _jaw = null!;
	private SMA _teeth = null!;
	private SMA _lips = null!;

	private decimal[] _highBuf = Array.Empty<decimal>();
	private decimal[] _lowBuf = Array.Empty<decimal>();
	private int _bufCount;

	private decimal? _pendingBuyLevel;
	private decimal? _pendingSellLevel;
	private decimal _prevJaw;
	private decimal _prevTeeth;
	private decimal _prevLips;
	private decimal _entryPrice;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	public int LipsPeriod
	{
		get => _lipsPeriod.Value;
		set => _lipsPeriod.Value = value;
	}

	public int FractalLen
	{
		get => _fractalLen.Value;
		set => _fractalLen.Value = value;
	}

	public FTBillWillamsTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");

		_jawPeriod = Param(nameof(JawPeriod), 13)
			.SetDisplay("Jaw Period", "Alligator jaw SMA period", "Alligator");

		_teethPeriod = Param(nameof(TeethPeriod), 8)
			.SetDisplay("Teeth Period", "Alligator teeth SMA period", "Alligator");

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
			.SetDisplay("Lips Period", "Alligator lips SMA period", "Alligator");

		_fractalLen = Param(nameof(FractalLen), 5)
			.SetDisplay("Fractal Length", "Number of bars for fractal detection", "Signals");
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_jaw = new SMA { Length = JawPeriod };
		_teeth = new SMA { Length = TeethPeriod };
		_lips = new SMA { Length = LipsPeriod };
		_highBuf = new decimal[FractalLen];
		_lowBuf = new decimal[FractalLen];
		_bufCount = 0;
		_pendingBuyLevel = null;
		_pendingSellLevel = null;
		_prevJaw = 0;
		_prevTeeth = 0;
		_prevLips = 0;
		_entryPrice = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_jaw = new SMA { Length = JawPeriod };
		_teeth = new SMA { Length = TeethPeriod };
		_lips = new SMA { Length = LipsPeriod };

		_highBuf = new decimal[FractalLen];
		_lowBuf = new decimal[FractalLen];
		_bufCount = 0;
		_pendingBuyLevel = null;
		_pendingSellLevel = null;
		_prevJaw = 0;
		_prevTeeth = 0;
		_prevLips = 0;
		_entryPrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_jaw, _teeth, _lips, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _jaw);
			DrawIndicator(area, _teeth);
			DrawIndicator(area, _lips);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal jawVal, decimal teethVal, decimal lipsVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update fractal buffers
		UpdateFractals(candle);

		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		// Manage existing positions - exit on opposite signal
		if (Position > 0)
		{
			// Close long if price breaks below pending sell level or close drops below teeth
			if (_pendingSellLevel is decimal sellLvl && low < sellLvl)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			// Close short if price breaks above pending buy level or close rises above teeth
			if (_pendingBuyLevel is decimal buyLvl && high > buyLvl)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		// Enter long: price breaks above up-fractal level, close above teeth (bullish)
		if (Position <= 0 && _pendingBuyLevel is decimal pendBuy)
		{
			if (high > pendBuy && close > teethVal)
			{
				if (Position < 0)
					BuyMarket(); // close short first

				BuyMarket();
				_entryPrice = close;
			}
		}

		// Enter short: price breaks below down-fractal level, close below teeth (bearish)
		if (Position >= 0 && _pendingSellLevel is decimal pendSell)
		{
			if (low < pendSell && close < teethVal)
			{
				if (Position > 0)
					SellMarket(); // close long first

				SellMarket();
				_entryPrice = close;
			}
		}

		_prevJaw = jawVal;
		_prevTeeth = teethVal;
		_prevLips = lipsVal;
	}

	private void UpdateFractals(ICandleMessage candle)
	{
		var len = _highBuf.Length;
		if (len < 3)
			return;

		// Shift buffers
		Array.Copy(_highBuf, 1, _highBuf, 0, len - 1);
		_highBuf[len - 1] = candle.HighPrice;
		Array.Copy(_lowBuf, 1, _lowBuf, 0, len - 1);
		_lowBuf[len - 1] = candle.LowPrice;

		_bufCount++;
		if (_bufCount < len)
			return;

		var wing = (len - 1) / 2;
		var center = len - 1 - wing;

		// Check up fractal
		var centerHigh = _highBuf[center];
		var isUp = true;
		for (var i = 0; i < len; i++)
		{
			if (i != center && _highBuf[i] >= centerHigh)
			{
				isUp = false;
				break;
			}
		}

		if (isUp)
			_pendingBuyLevel = centerHigh;

		// Check down fractal
		var centerLow = _lowBuf[center];
		var isDown = true;
		for (var i = 0; i < len; i++)
		{
			if (i != center && _lowBuf[i] <= centerLow)
			{
				isDown = false;
				break;
			}
		}

		if (isDown)
			_pendingSellLevel = centerLow;
	}
}
