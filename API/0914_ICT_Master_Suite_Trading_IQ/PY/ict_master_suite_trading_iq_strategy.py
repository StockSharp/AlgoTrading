import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class ict_master_suite_trading_iq_strategy(Strategy):
    def __init__(self):
        super(ict_master_suite_trading_iq_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR calculation period", "Risk Management")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "ATR multiplier for stop", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(240))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._session_high = 0.0
        self._session_low = 0.0
        self._session_date = None
        self._entry_price = 0.0
        self._stop_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(ict_master_suite_trading_iq_strategy, self).OnReseted()
        self._session_high = 0.0
        self._session_low = 0.0
        self._session_date = None
        self._entry_price = 0.0
        self._stop_price = 0.0

    def OnStarted(self, time):
        super(ict_master_suite_trading_iq_strategy, self).OnStarted(time)
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return
        atr_v = float(atr_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        current_date = candle.OpenTime.Date
        mult = float(self._atr_multiplier.Value)
        if self._session_date is None or self._session_date != current_date:
            self._session_date = current_date
            self._session_high = high
            self._session_low = low
            return
        if self.Position <= 0 and close > self._session_high:
            self._entry_price = close
            self._stop_price = self._entry_price - atr_v * mult
            self.BuyMarket()
            return
        if self.Position >= 0 and close < self._session_low:
            self._entry_price = close
            self._stop_price = self._entry_price + atr_v * mult
            self.SellMarket()
            return
        if high > self._session_high:
            self._session_high = high
        if low < self._session_low:
            self._session_low = low
        if self.Position > 0:
            new_stop = close - atr_v * mult
            if new_stop > self._stop_price:
                self._stop_price = new_stop
            if low <= self._stop_price:
                self.SellMarket()
        elif self.Position < 0:
            new_stop = close + atr_v * mult
            if new_stop < self._stop_price:
                self._stop_price = new_stop
            if high >= self._stop_price:
                self.BuyMarket()

    def CreateClone(self):
        return ict_master_suite_trading_iq_strategy()
