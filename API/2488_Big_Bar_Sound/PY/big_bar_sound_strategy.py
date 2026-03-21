import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class big_bar_sound_strategy(Strategy):
    def __init__(self):
        super(big_bar_sound_strategy, self).__init__()
        self._bar_point = self.Param("BarPoint", 180) \
            .SetDisplay("Point Threshold", "Number of price steps required to trigger entry", "General")
        self._difference_mode = self.Param("DifferenceMode", 0) \
            .SetDisplay("Difference Mode", "0=OpenClose, 1=HighLow", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
        self._atr_stop_multiplier = self.Param("AtrStopMult", 2.0) \
            .SetDisplay("ATR Stop Mult", "ATR multiplier for stop-loss", "Risk")
        self._atr_tp_multiplier = self.Param("AtrTpMult", 3.0) \
            .SetDisplay("ATR TP Mult", "ATR multiplier for take-profit", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to monitor", "Data")
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._direction = 0

    @property
    def bar_point(self):
        return self._bar_point.Value
    @property
    def difference_mode(self):
        return self._difference_mode.Value
    @property
    def atr_period(self):
        return self._atr_period.Value
    @property
    def atr_stop_multiplier(self):
        return self._atr_stop_multiplier.Value
    @property
    def atr_tp_multiplier(self):
        return self._atr_tp_multiplier.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(big_bar_sound_strategy, self).OnReseted()
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._direction = 0

    def OnStarted(self, time):
        super(big_bar_sound_strategy, self).OnStarted(time)
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        # Manage existing position
        if self.Position > 0 and self._direction > 0:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_profit_price:
                self.SellMarket()
                self._direction = 0
                self._stop_price = 0.0
                self._take_profit_price = 0.0
        elif self.Position < 0 and self._direction < 0:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_profit_price:
                self.BuyMarket()
                self._direction = 0
                self._stop_price = 0.0
                self._take_profit_price = 0.0
        if self.Position != 0:
            return
        if atr_value <= 0:
            return

        if self.difference_mode == 0:
            difference = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        else:
            difference = float(candle.HighPrice) - float(candle.LowPrice)

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and sec.PriceStep > 0 else 1.0
        threshold = step * self.bar_point

        if difference < threshold:
            return

        is_bullish = candle.ClosePrice > candle.OpenPrice
        stop_dist = float(atr_value) * self.atr_stop_multiplier
        tp_dist = float(atr_value) * self.atr_tp_multiplier

        if is_bullish:
            self.BuyMarket()
            self._direction = 1
            self._stop_price = float(candle.ClosePrice) - stop_dist
            self._take_profit_price = float(candle.ClosePrice) + tp_dist
        else:
            self.SellMarket()
            self._direction = -1
            self._stop_price = float(candle.ClosePrice) + stop_dist
            self._take_profit_price = float(candle.ClosePrice) - tp_dist

    def CreateClone(self):
        return big_bar_sound_strategy()
