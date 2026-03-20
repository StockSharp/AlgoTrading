import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class parent_session_sweeps_alert_strategy(Strategy):
    def __init__(self):
        super(parent_session_sweeps_alert_strategy, self).__init__()
        self._min_risk_reward = self.Param("MinRiskReward", 1.0)
        self._use_candle_filter = self.Param("UseCandleFilter", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._prev_high = None
        self._prev_low = None
        self._session_high = 0.0
        self._session_low = 0.0
        self._current_session_date = None
        self._stop_price = None
        self._target_price = None
        self._is_long = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(parent_session_sweeps_alert_strategy, self).OnReseted()
        self._prev_high = None
        self._prev_low = None
        self._session_high = 0.0
        self._session_low = 0.0
        self._current_session_date = None
        self._stop_price = None
        self._target_price = None

    def OnStarted(self, time):
        super(parent_session_sweeps_alert_strategy, self).OnStarted(time)
        self._prev_high = None
        self._prev_low = None
        self._session_high = 0.0
        self._session_low = 0.0
        self._current_session_date = None
        self._stop_price = None
        self._target_price = None
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        date = candle.OpenTime.Date
        if self._current_session_date is None or self._current_session_date != date:
            if self._session_high != 0.0:
                self._prev_high = self._session_high
                self._prev_low = self._session_low
            self._session_high = high
            self._session_low = low
            self._current_session_date = date
            return
        self._session_high = max(self._session_high, high)
        self._session_low = min(self._session_low, low)
        if self.Position == 0 and self._prev_high is not None and self._prev_low is not None:
            ph = self._prev_high
            pl = self._prev_low
            use_filter = self._use_candle_filter.Value
            if high > ph and (not use_filter or close < ph):
                entry = close
                stop = high
                risk = stop - entry
                if risk > 0:
                    rr = float(self._min_risk_reward.Value)
                    target = entry - risk * rr
                    self.SellMarket()
                    self._is_long = False
                    self._stop_price = stop
                    self._target_price = target
            elif low < pl and (not use_filter or close > pl):
                entry = close
                stop = low
                risk = entry - stop
                if risk > 0:
                    rr = float(self._min_risk_reward.Value)
                    target = entry + risk * rr
                    self.BuyMarket()
                    self._is_long = True
                    self._stop_price = stop
                    self._target_price = target
        elif self.Position != 0 and self._stop_price is not None and self._target_price is not None:
            if self._is_long:
                if low <= self._stop_price or high >= self._target_price:
                    self.SellMarket()
                    self._stop_price = None
                    self._target_price = None
            else:
                if high >= self._stop_price or low <= self._target_price:
                    self.BuyMarket()
                    self._stop_price = None
                    self._target_price = None

    def CreateClone(self):
        return parent_session_sweeps_alert_strategy()
