import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage

class open_time_daily_window_strategy(Strategy):
    def __init__(self):
        super(open_time_daily_window_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period for direction", "Indicators")
        self._trade_hour = self.Param("TradeHour", 10) \
            .SetDisplay("Trade Hour", "Hour when trading window opens", "Schedule")
        self._window_minutes = self.Param("WindowMinutes", 120) \
            .SetDisplay("Window Minutes", "Duration of trading window", "Schedule")
        self._close_hour = self.Param("CloseHour", 20) \
            .SetDisplay("Close Hour", "Hour to close positions", "Schedule")

        self._prev_ema = 0.0
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    @property
    def TradeHour(self):
        return self._trade_hour.Value

    @property
    def WindowMinutes(self):
        return self._window_minutes.Value

    @property
    def CloseHour(self):
        return self._close_hour.Value

    def OnStarted(self, time):
        super(open_time_daily_window_strategy, self).OnStarted(time)

        self._prev_ema = 0.0
        self._entry_price = 0.0

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._ema, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        ev = float(ema_val)
        hour = candle.OpenTime.Hour
        minute = candle.OpenTime.Minute
        total_minutes = hour * 60 + minute
        trade_start = self.TradeHour * 60
        trade_end = trade_start + self.WindowMinutes
        close_start = self.CloseHour * 60

        close = float(candle.ClosePrice)

        # Close position at close hour
        if self.Position != 0 and total_minutes >= close_start and total_minutes < close_start + 30:
            if self.Position > 0:
                self.SellMarket()
            else:
                self.BuyMarket()
            self._entry_price = 0.0

        if self._prev_ema == 0:
            self._prev_ema = ev
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_ema = ev
            return

        # Trade within window
        in_window = total_minutes >= trade_start and total_minutes < trade_end

        if self.Position == 0 and in_window:
            ema_rising = ev > self._prev_ema
            ema_falling = ev < self._prev_ema

            if ema_rising and close > ev:
                self._entry_price = close
                self.BuyMarket()
            elif ema_falling and close < ev:
                self._entry_price = close
                self.SellMarket()

        self._prev_ema = ev

    def OnReseted(self):
        super(open_time_daily_window_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return open_time_daily_window_strategy()
