import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math
from StockSharp.Messages import CandleStates, Sides, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, MovingAverageConvergenceDivergence
from datatype_extensions import *


class vela_superada_strategy(Strategy):
    """Vela Superada strategy.

    Trades reversals when a bearish candle is immediately followed by a bullish one
    or vice versa. Entries are filtered with EMA direction, RSI and MACD momentum.
    Trailing stops are updated after entry and optional long/short sides can be
    enabled separately.
    """

    def __init__(self):
        super(vela_superada_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_length = self.Param("EmaLength", 10) \
            .SetDisplay("EMA Length", "EMA period", "Moving Averages")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI calculation length", "RSI")
        self._show_long = self.Param("ShowLong", True) \
            .SetDisplay("Long Entries", "Enable long entries", "Strategy")
        self._show_short = self.Param("ShowShort", False) \
            .SetDisplay("Short Entries", "Enable short entries", "Strategy")
        self._tp_percent = self.Param("TpPercent", 1.2) \
            .SetDisplay("TP Percent", "Take profit percentage", "Risk Management")
        self._sl_percent = self.Param("SlPercent", 1.8) \
            .SetDisplay("SL Percent", "Stop loss percentage", "Risk Management")

        self._ema = None
        self._rsi = None
        self._macd = None

        self._previous_close = 0.0
        self._previous_open = 0.0
        self._previous_macd = 0.0
        self._entry_price = 0.0
        self._trailing_stop_long = 0.0
        self._trailing_stop_short = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def ema_length(self):
        return self._ema_length.Value

    @ema_length.setter
    def ema_length(self, value):
        self._ema_length.Value = value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @rsi_length.setter
    def rsi_length(self, value):
        self._rsi_length.Value = value

    @property
    def show_long(self):
        return self._show_long.Value

    @show_long.setter
    def show_long(self, value):
        self._show_long.Value = value

    @property
    def show_short(self):
        return self._show_short.Value

    @show_short.setter
    def show_short(self, value):
        self._show_short.Value = value

    @property
    def tp_percent(self):
        return self._tp_percent.Value

    @tp_percent.setter
    def tp_percent(self, value):
        self._tp_percent.Value = value

    @property
    def sl_percent(self):
        return self._sl_percent.Value

    @sl_percent.setter
    def sl_percent(self, value):
        self._sl_percent.Value = value

    def OnReseted(self):
        super(vela_superada_strategy, self).OnReseted()
        self._previous_close = 0.0
        self._previous_open = 0.0
        self._previous_macd = 0.0
        self._entry_price = 0.0
        self._trailing_stop_long = 0.0
        self._trailing_stop_short = 0.0

    def OnStarted(self, time):
        super(vela_superada_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_length
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_length
        self._macd = MovingAverageConvergenceDivergence()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._rsi, self._macd, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawIndicator(area, self._rsi)
            self.DrawIndicator(area, self._macd)
            self.DrawOwnTrades(area)

        from StockSharp.Algo import Unit, UnitTypes
        self.StartProtection(Unit(self.tp_percent / 100.0, UnitTypes.Percent), Unit(self.sl_percent / 100.0, UnitTypes.Percent))

    def ProcessCandle(self, candle, ema_value, rsi_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed or not self._rsi.IsFormed or not self._macd.IsFormed:
            return

        price = candle.ClosePrice
        open_price = candle.OpenPrice

        bullish_pattern = self._previous_close < self._previous_open and price > open_price
        bearish_pattern = self._previous_close > self._previous_open and price < open_price

        self._check_entries(candle, float(ema_value), float(rsi_value), float(macd_value), bullish_pattern, bearish_pattern)
        self._update_trailing(candle)

        self._previous_close = price
        self._previous_open = open_price
        self._previous_macd = float(macd_value)

        if self.Position != 0 and self._entry_price == 0:
            self._entry_price = open_price
        elif self.Position == 0:
            self._entry_price = 0
            self._trailing_stop_long = 0
            self._trailing_stop_short = 0

    def _check_entries(self, candle, ema_value, rsi_value, macd_value, bullish_pattern, bearish_pattern):
        price = candle.ClosePrice
        if (self.show_long and bullish_pattern and price > ema_value and self._previous_close > ema_value and
                rsi_value < 65 and macd_value > self._previous_macd and self.Position == 0):
            self.RegisterOrder(self.CreateOrder(Sides.Buy, price, self.Volume))
        if (self.show_short and bearish_pattern and price < ema_value and self._previous_close < ema_value and
                rsi_value > 35 and macd_value < self._previous_macd and self.Position == 0):
            self.RegisterOrder(self.CreateOrder(Sides.Sell, price, self.Volume))

    def _update_trailing(self, candle):
        if self.Position == 0 or self._entry_price == 0:
            return

        price = candle.ClosePrice
        avg_tp_price = (self._entry_price * (1 + self.tp_percent / 100.0) + self._entry_price) / 2

        if self.Position > 0:
            basic_stop = self._entry_price * (1 - self.sl_percent / 100.0)
            if price > avg_tp_price:
                self._trailing_stop_long = self._entry_price * 1.002
            else:
                self._trailing_stop_long = max(self._trailing_stop_long, basic_stop)
            if price <= self._trailing_stop_long and self._trailing_stop_long != 0:
                self.SellMarket(Math.Abs(self.Position))
        else:
            basic_stop = self._entry_price * (1 + self.sl_percent / 100.0)
            avg_tp_price_short = (self._entry_price * (1 - self.tp_percent / 100.0) + self._entry_price) / 2
            if price < avg_tp_price_short:
                self._trailing_stop_short = self._entry_price * 0.998
            else:
                self._trailing_stop_short = self._trailing_stop_short if self._trailing_stop_short != 0 else basic_stop
                self._trailing_stop_short = min(self._trailing_stop_short, basic_stop)
            if price >= self._trailing_stop_short and self._trailing_stop_short != 0:
                self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        return vela_superada_strategy()
