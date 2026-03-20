import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class twenty_one_hour_session_breakout_strategy(Strategy):
    def __init__(self):
        super(twenty_one_hour_session_breakout_strategy, self).__init__()

        self._first_session_start_hour = self.Param("FirstSessionStartHour", 2)
        self._first_session_stop_hour = self.Param("FirstSessionStopHour", 20)
        self._step_points = self.Param("StepPoints", 40.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 200.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._session_open = None
        self._entry_price = 0.0
        self._in_session = False

    @property
    def FirstSessionStartHour(self):
        return self._first_session_start_hour.Value

    @FirstSessionStartHour.setter
    def FirstSessionStartHour(self, value):
        self._first_session_start_hour.Value = value

    @property
    def FirstSessionStopHour(self):
        return self._first_session_stop_hour.Value

    @FirstSessionStopHour.setter
    def FirstSessionStopHour(self, value):
        self._first_session_stop_hour.Value = value

    @property
    def StepPoints(self):
        return self._step_points.Value

    @StepPoints.setter
    def StepPoints(self, value):
        self._step_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(twenty_one_hour_session_breakout_strategy, self).OnStarted(time)

        self._session_open = None
        self._entry_price = 0.0
        self._in_session = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        hour = candle.OpenTime.Hour
        price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if price_step <= 0.0:
            price_step = 1.0

        start_hour = int(self.FirstSessionStartHour)
        stop_hour = int(self.FirstSessionStopHour)

        if hour >= start_hour and hour < stop_hour:
            if not self._in_session:
                self._session_open = float(candle.OpenPrice)
                self._in_session = True

            if self._session_open is None:
                return

            step_offset = float(self.StepPoints) * price_step
            buy_level = self._session_open + step_offset
            sell_level = self._session_open - step_offset

            high = float(candle.HighPrice)
            low = float(candle.LowPrice)
            close = float(candle.ClosePrice)

            if self.Position == 0:
                if high >= buy_level:
                    self.BuyMarket()
                    self._entry_price = buy_level
                elif low <= sell_level:
                    self.SellMarket()
                    self._entry_price = sell_level

            if self.Position > 0:
                tp = self._entry_price + float(self.TakeProfitPoints) * price_step
                if high >= tp:
                    self.SellMarket()
                    self._session_open = close
            elif self.Position < 0:
                tp = self._entry_price - float(self.TakeProfitPoints) * price_step
                if low <= tp:
                    self.BuyMarket()
                    self._session_open = close
        else:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()

            self._in_session = False
            self._session_open = None

    def OnReseted(self):
        super(twenty_one_hour_session_breakout_strategy, self).OnReseted()
        self._session_open = None
        self._entry_price = 0.0
        self._in_session = False

    def CreateClone(self):
        return twenty_one_hour_session_breakout_strategy()
