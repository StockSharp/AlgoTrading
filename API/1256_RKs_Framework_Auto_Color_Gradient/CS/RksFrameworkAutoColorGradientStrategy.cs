using System;
using System.Drawing;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RK's Framework Auto Color Gradient strategy.
/// Combines Bollinger Bands %B and RSI into a single oscillator, maps it to a red-green gradient and trades around zero.
/// </summary>
public class RksFrameworkAutoColorGradientStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _scaleLength;
	private readonly StrategyParam<bool> _revertScale;
	private readonly StrategyParam<DataType> _candleType;

	private BollingerBands _bb;
	private RelativeStrengthIndex _rsi;
	private Highest _highestBbr;
	private Lowest _lowestBbr;
	private Highest _highestRsi;
	private Lowest _lowestRsi;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public int ScaleLength { get => _scaleLength.Value; set => _scaleLength.Value = value; }
	public bool RevertScale { get => _revertScale.Value; set => _revertScale.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RksFrameworkAutoColorGradientStrategy()
	{
		_length = Param(nameof(Length), 21)
		.SetGreaterThanZero()
		.SetDisplay("Length")
		.SetCanOptimize(true);

		_scaleLength = Param(nameof(ScaleLength), 200)
		.SetGreaterThanZero()
		.SetDisplay("Scale Length");

		_revertScale = Param(nameof(RevertScale), true)
		.SetDisplay("Revert Scale");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type");
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_bb = default;
		_rsi = default;
		_highestBbr = default;
		_lowestBbr = default;
		_highestRsi = default;
		_lowestRsi = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		_bb = new BollingerBands { Length = Length, Width = 2m };
		_rsi = new RelativeStrengthIndex { Length = Length };

		_highestBbr = new Highest { Length = ScaleLength };
		_lowestBbr = new Lowest { Length = ScaleLength };
		_highestRsi = new Highest { Length = ScaleLength };
		_lowestRsi = new Lowest { Length = ScaleLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_bb, _rsi, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, subscription);
		DrawIndicator(area, _bb);
		DrawIndicator(area, _rsi);
		DrawOwnTrades(area);
		}

		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var deviation = upper - middle;
		if (deviation == 0)
		return;

		var bbr = (candle.ClosePrice - middle - deviation) / (2m * deviation);

		var stochBbr = GetStoch(_highestBbr, _lowestBbr, bbr);
		var stochRsi = GetStoch(_highestRsi, _lowestRsi, rsi);
		if (stochBbr == null || stochRsi == null)
		return;

		var avg = ((decimal)stochBbr + (decimal)stochRsi) / 50m - 1m;
		avg = RevertScale ? -avg : avg;

		var ratio = (avg + 1m) / 2m;
		var color = Color.FromArgb((int)(255 * (1 - ratio)), (int)(255 * ratio), 0);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (avg > 0 && Position <= 0)
		{
		var volume = Volume + (Position < 0 ? -Position : 0m);
		BuyMarket(volume);
		}
		else if (avg < 0 && Position >= 0)
		{
		var volume = Volume + (Position > 0 ? Position : 0m);
		SellMarket(volume);
		}

		// Color variable can be used for custom visualization.
	}

	private decimal? GetStoch(Highest highest, Lowest lowest, decimal value)
	{
		var highValue = highest.Process(value);
		var lowValue = lowest.Process(value);

		if (!highValue.IsFinal || !lowValue.IsFinal)
		return null;

		var high = highValue.ToDecimal();
		var low = lowValue.ToDecimal();
		if (high == low)
		return 50m;

		return (value - low) / (high - low) * 100m;
	}
}
