using System;
using System.Collections.Generic;
using System.Drawing;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dummy strategy based on luminance of a configured color.
/// Buys when the color is light, sells when it is dark.
/// </summary>
public class ColorStrategy : Strategy
{
	private readonly StrategyParam<string> _colorHex;
	private readonly StrategyParam<DataType> _candleType;

	private Color _baseColor;
	private decimal _luminance;

	/// <summary>
	/// Color in HEX format used for decision making.
	/// </summary>
	public string ColorHex
	{
		get => _colorHex.Value;
		set => _colorHex.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ColorStrategy()
	{
		_colorHex = Param(nameof(ColorHex), "#f23645")
			.SetDisplay("Color HEX", "Base color in HEX format", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_baseColor = HexStringToColor(ColorHex);
		_luminance = GetLuminance(_baseColor);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_luminance > 0.5m && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (_luminance <= 0.5m && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}

	private static Color HexStringToColor(string hex)
	{
		if (string.IsNullOrWhiteSpace(hex))
			return Color.Black;

		if (!hex.StartsWith("#", StringComparison.Ordinal))
			hex = "#" + hex;

		return ColorTranslator.FromHtml(hex);
	}

	private static string GetHexString(Color color)
	{
		return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
	}

	private static decimal GetLuminance(Color color)
	{
		return (0.2126m * color.R + 0.7152m * color.G + 0.0722m * color.B) / 255m;
	}
}

