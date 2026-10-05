using System;
using System.Collections.Generic;
using System.Globalization;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Color strategy.
/// The perceived luminance of ColorHex decides the side on every finished candle: a light color (luminance above 0.5) holds a long
/// position and a dark one holds a short position, reversing when the color changes.
/// </summary>
public class ColorStrategy : Strategy
{
	private readonly StrategyParam<string> _colorHex;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Color in #RRGGBB form.
	/// </summary>
	public string ColorHex
	{
		get => _colorHex.Value;
		set => _colorHex.Value = value;
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
	public ColorStrategy()
	{
		_colorHex = Param(nameof(ColorHex), "#f23645")
			.SetDisplay("Color", "Color in #RRGGBB form", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		if (GetLuminance(ColorHex) is not decimal luminance)
			return;

		if (luminance > 0.5m)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}

	private static decimal? GetLuminance(string hex)
	{
		var text = (hex ?? string.Empty).Trim().TrimStart('#');

		if (text.Length != 6 || !int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
			return null;

		var r = (rgb >> 16) & 0xFF;
		var g = (rgb >> 8) & 0xFF;
		var b = rgb & 0xFF;

		// Perceived brightness weights of the RGB channels.
		return (0.299m * r + 0.587m * g + 0.114m * b) / 255m;
	}
}
