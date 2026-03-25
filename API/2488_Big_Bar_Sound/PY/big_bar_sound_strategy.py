import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


OPEN_CLOSE = 0
HIGH_LOW = 1


class big_bar_sound_strategy(Strategy):
    def __init__(self):
        super(big_bar_sound_strategy, self).__init__()
        self._bar_point = self.Param("BarPoint", 180)
        self._difference_mode = self.Param("DifferenceMode", OPEN_CLOSE)
        self._atr_period = self.Param("AtrPeriod", 14)
        self._atr_stop_multiplier = self.Param("AtrStopMultiplier", 2.0)
        self._atr_tp_multiplier = self.Param("AtrTpMultiplier", 3.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._direction = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(big_bar_sound_strategy, self).OnReseted()
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._direction = 0

    def OnStarted(self, time):
        super(big_bar_sound_strategy, self).OnStarted(time)
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._direction = 0

        atr = AverageTrueRange()
        atr.Length = int(self._atr_period.Value)
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self.OnProcess).Start()

    def OnProcess(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        pos = float(self.Position)
        if pos > 0 and self._direction > 0:
            if float(candle.LowPrice) <= self._stop_price or float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket(pos)
                self._direction = 0
                self._stop_price = 0.0
                self._take_profit_price = 0.0
        elif pos < 0 and self._direction < 0:
            if float(candle.HighPrice) >= self._stop_price or float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket(abs(pos))
                self._direction = 0
                self._stop_price = 0.0
                self._take_profit_price = 0.0

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0:
            return

        atr_v = float(atr_value)
        if atr_v <= 0:
            return

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if int(self._difference_mode.Value) == OPEN_CLOSE:
            difference = abs(close - open_p)
        else:
            difference = high - low

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and sec.PriceStep > 0 else 1.0
        threshold = step * float(self._bar_point.Value)

        if difference < threshold:
            return

        is_bullish = close > open_p
        stop_dist = atr_v * float(self._atr_stop_multiplier.Value)
        tp_dist = atr_v * float(self._atr_tp_multiplier.Value)
        vol = float(self.Volume)

        if is_bullish:
            self.BuyMarket(vol)
            self._direction = 1
            self._stop_price = close - stop_dist
            self._take_profit_price = close + tp_dist
        else:
            self.SellMarket(vol)
            self._direction = -1
            self._stop_price = close + stop_dist
            self._take_profit_price = close - tp_dist

    def CreateClone(self):
        return big_bar_sound_strategy()
