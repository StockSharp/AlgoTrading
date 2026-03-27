import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class open_time_two_strategy(Strategy):
    def __init__(self):
        super(open_time_two_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))).SetDisplay("Candle Type", "Base candle type", "General")
        self._trade_volume = self.Param("TradeVolume", 0.1).SetDisplay("Trade Volume", "Volume for each interval", "Risk")
        self._interval_one_buy = self.Param("IntervalOneBuy", True).SetDisplay("Direction #1", "Buy for interval #1", "Opening")
        self._interval_two_buy = self.Param("IntervalTwoBuy", True).SetDisplay("Direction #2", "Buy for interval #2", "Opening")
        self._sl1_pips = self.Param("StopLossOnePips", 30.0).SetDisplay("Stop Loss #1", "Stop loss for interval #1 (pips)", "Risk")
        self._tp1_pips = self.Param("TakeProfitOnePips", 90.0).SetDisplay("Take Profit #1", "Take profit for interval #1 (pips)", "Risk")
        self._sl2_pips = self.Param("StopLossTwoPips", 10.0).SetDisplay("Stop Loss #2", "Stop loss for interval #2 (pips)", "Risk")
        self._tp2_pips = self.Param("TakeProfitTwoPips", 35.0).SetDisplay("Take Profit #2", "Take profit for interval #2 (pips)", "Risk")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(open_time_two_strategy, self).OnReseted()
        self._int1_active = False
        self._int2_active = False
        self._int1_entry = 0
        self._int2_entry = 0
        self._int1_stop = None
        self._int1_take = None
        self._int2_stop = None
        self._int2_take = None

    def OnStarted(self, time):
        super(open_time_two_strategy, self).OnStarted(time)
        self._int1_active = False
        self._int2_active = False
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            step = float(self.Security.PriceStep)
        self._pip_size = step

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        hour = candle.OpenTime.Hour

        # Check SL/TP for interval 1
        if self._int1_active:
            direction = 1 if self._interval_one_buy.Value else -1
            if direction > 0:
                if self._int1_stop is not None and candle.LowPrice <= self._int1_stop:
                    self.SellMarket(self._trade_volume.Value)
                    self._int1_active = False
                elif self._int1_take is not None and candle.HighPrice >= self._int1_take:
                    self.SellMarket(self._trade_volume.Value)
                    self._int1_active = False
            else:
                if self._int1_stop is not None and candle.HighPrice >= self._int1_stop:
                    self.BuyMarket(self._trade_volume.Value)
                    self._int1_active = False
                elif self._int1_take is not None and candle.LowPrice <= self._int1_take:
                    self.BuyMarket(self._trade_volume.Value)
                    self._int1_active = False

        # Check SL/TP for interval 2
        if self._int2_active:
            direction = 1 if self._interval_two_buy.Value else -1
            if direction > 0:
                if self._int2_stop is not None and candle.LowPrice <= self._int2_stop:
                    self.SellMarket(self._trade_volume.Value)
                    self._int2_active = False
                elif self._int2_take is not None and candle.HighPrice >= self._int2_take:
                    self.SellMarket(self._trade_volume.Value)
                    self._int2_active = False
            else:
                if self._int2_stop is not None and candle.HighPrice >= self._int2_stop:
                    self.BuyMarket(self._trade_volume.Value)
                    self._int2_active = False
                elif self._int2_take is not None and candle.LowPrice <= self._int2_take:
                    self.BuyMarket(self._trade_volume.Value)
                    self._int2_active = False

        # Open interval 1 during morning hours
        if not self._int1_active and 9 <= hour < 14:
            self._open_interval(1, close)

        # Open interval 2 during afternoon hours
        if not self._int2_active and 14 <= hour < 20:
            self._open_interval(2, close)

    def _open_interval(self, interval_num, price):
        if interval_num == 1:
            is_buy = self._interval_one_buy.Value
            sl_pips = self._sl1_pips.Value
            tp_pips = self._tp1_pips.Value
            if is_buy:
                self.BuyMarket(self._trade_volume.Value)
                self._int1_stop = price - sl_pips * self._pip_size if sl_pips > 0 else None
                self._int1_take = price + tp_pips * self._pip_size if tp_pips > 0 else None
            else:
                self.SellMarket(self._trade_volume.Value)
                self._int1_stop = price + sl_pips * self._pip_size if sl_pips > 0 else None
                self._int1_take = price - tp_pips * self._pip_size if tp_pips > 0 else None
            self._int1_entry = price
            self._int1_active = True
        else:
            is_buy = self._interval_two_buy.Value
            sl_pips = self._sl2_pips.Value
            tp_pips = self._tp2_pips.Value
            if is_buy:
                self.BuyMarket(self._trade_volume.Value)
                self._int2_stop = price - sl_pips * self._pip_size if sl_pips > 0 else None
                self._int2_take = price + tp_pips * self._pip_size if tp_pips > 0 else None
            else:
                self.SellMarket(self._trade_volume.Value)
                self._int2_stop = price + sl_pips * self._pip_size if sl_pips > 0 else None
                self._int2_take = price - tp_pips * self._pip_size if tp_pips > 0 else None
            self._int2_entry = price
            self._int2_active = True

    def CreateClone(self):
        return open_time_two_strategy()
