import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class heiken_ashi_no_wick_strategy(Strategy):

    def __init__(self):
        super(heiken_ashi_no_wick_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._prev_is_bull = False
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(heiken_ashi_no_wick_strategy, self).OnStarted(time)

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        if self._prev_ha_open == 0.0 and self._prev_ha_close == 0.0:
            ha_open = (o + c) / 2.0
            ha_close = (o + h + l + c) / 4.0
        else:
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2.0
            ha_close = (o + h + l + c) / 4.0

        is_bull = ha_close > ha_open

        if self._has_prev:
            if is_bull and not self._prev_is_bull and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif not is_bull and self._prev_is_bull and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close
        self._prev_is_bull = is_bull
        self._has_prev = True

    def OnReseted(self):
        super(heiken_ashi_no_wick_strategy, self).OnReseted()
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._prev_is_bull = False
        self._has_prev = False

    def CreateClone(self):
        return heiken_ashi_no_wick_strategy()
