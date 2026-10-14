import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class color_strategy(Strategy):
    """
    Color strategy.
    The perceived luminance of ColorHex decides the side on every finished candle: a light color (luminance above 0.5) holds a long
    position and a dark one holds a short position, reversing when the color changes.
    """

    def __init__(self):
        super(color_strategy, self).__init__()
        self._color_hex = self.Param("ColorHex", "#f23645").SetDisplay("Color", "Color in #RRGGBB form", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(color_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        luminance = self._get_luminance(self._color_hex.Value)
        if luminance is None:
            return

        if luminance > 0.5:
            if self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
        elif self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    @staticmethod
    def _get_luminance(hex_value):
        text = (hex_value or "").strip().lstrip("#")
        if len(text) != 6:
            return None
        try:
            rgb = int(text, 16)
        except ValueError:
            return None

        r = (rgb >> 16) & 0xFF
        g = (rgb >> 8) & 0xFF
        b = rgb & 0xFF

        # Perceived brightness weights of the RGB channels.
        return (0.299 * r + 0.587 * g + 0.114 * b) / 255.0

    def CreateClone(self):
        return color_strategy()
