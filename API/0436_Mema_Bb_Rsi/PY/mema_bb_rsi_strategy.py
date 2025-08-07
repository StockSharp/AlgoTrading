import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Array, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, BollingerBands, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class mema_bb_rsi_strategy(Strategy):
    """Multi-timeframe EMA + Bollinger Bands + RSI strategy.

    Buys when price closes above the fast EMA after touching the lower
    Bollinger Band. Sells short when price closes below the fast EMA after
    touching the upper band and RSI is above 50. Optional profit taking
    after N bars if price moves in favor.
    """

    def __init__(self):
        super(mema_bb_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)).SetDisplay(
            "Candle type", "Candle type for strategy calculation.", "General"
        )
        self._ma1_period = self.Param("Ma1Period", 10).SetDisplay(
            "MA1 Period", "First EMA period", "Moving Average"
        )
        self._ma2_period = self.Param("Ma2Period", 55).SetDisplay(
            "MA2 Period", "Second EMA period", "Moving Average"
        )
        self._bb_length = self.Param("BBLength", 20).SetDisplay(
            "BB Length", "Bollinger Bands period", "Bollinger Bands"
        )
        self._bb_multiplier = self.Param("BBMultiplier", 2.0).SetDisplay(
            "BB StdDev", "Standard deviation multiplier", "Bollinger Bands"
        )
        self._rsi_length = self.Param("RSILength", 14).SetDisplay(
            "RSI Length", "RSI period", "RSI"
        )
        self._rsi_oversold = self.Param("RSIOversold", 71).SetDisplay(
            "RSI Oversold", "RSI oversold level", "RSI"
        )
        self._show_long = self.Param("ShowLong", True).SetDisplay(
            "Long entries", "Enable long positions", "Strategy"
        )
        self._show_short = self.Param("ShowShort", False).SetDisplay(
            "Short entries", "Enable short positions", "Strategy"
        )
        self._close_after_x = self.Param("CloseAfterXBars", False).SetDisplay(
            "Close after X bars", "Close position after X bars if in profit", "Strategy"
        )
        self._x_bars = self.Param("XBars", 12).SetDisplay(
            "# bars", "Number of bars", "Strategy"
        )

        self._ma1 = None
        self._ma2 = None
        self._bb = None
        self._rsi = None
        self._bars_in_position = 0
        self._entry_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mema_bb_rsi_strategy, self).OnReseted()
        self._bars_in_position = 0
        self._entry_price = None

    def OnStarted(self, time):
        super(mema_bb_rsi_strategy, self).OnStarted(time)

        self._ma1 = ExponentialMovingAverage()
        self._ma1.Length = self._ma1_period.Value
        self._ma2 = ExponentialMovingAverage()
        self._ma2.Length = self._ma2_period.Value
        self._bb = BollingerBands()
        self._bb.Length = self._bb_length.Value
        self._bb.Width = self._bb_multiplier.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value

        sub = self.SubscribeCandles(self.candle_type)
        sub.BindEx(self._ma1, self._ma2, self._bb, self._rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._ma1)
            self.DrawIndicator(area, self._ma2)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ma1_val, ma2_val, bb_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._ma1.IsFormed or not self._ma2.IsFormed or not self._bb.IsFormed or not self._rsi.IsFormed:
            return

        ma1 = float(ma1_val)
        ma2 = float(ma2_val)
        rsi = float(rsi_val)
        bb = bb_val
        upper = bb.UpBand
        lower = bb.LowBand

        entry_long = candle.ClosePrice > ma1 and candle.LowPrice < lower
        entry_short = candle.ClosePrice < ma1 and candle.HighPrice > upper and rsi > 50
        exit_long = rsi > self._rsi_oversold.Value
        exit_short = candle.ClosePrice < lower

        if self.Position != 0:
            self._bars_in_position += 1
        else:
            self._bars_in_position = 0
            self._entry_price = None

        if self._close_after_x.Value and self._entry_price is not None and self._bars_in_position >= self._x_bars.Value:
            if self.Position > 0 and candle.ClosePrice > self._entry_price:
                exit_long = True
            elif self.Position < 0 and candle.ClosePrice < self._entry_price:
                exit_short = True

        if self._show_long.Value and exit_long and self.Position > 0:
            self.ClosePosition()
        elif self._show_short.Value and exit_short and self.Position < 0:
            self.ClosePosition()
        elif self._show_long.Value and entry_long and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self._entry_price = candle.ClosePrice
            self._bars_in_position = 0
        elif self._show_short.Value and entry_short and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self._entry_price = candle.ClosePrice
            self._bars_in_position = 0

    def CreateClone(self):
        return mema_bb_rsi_strategy()
