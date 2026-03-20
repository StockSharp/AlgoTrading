import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class forex_fraus_slogger_strategy(Strategy):
    def __init__(self):
        super(forex_fraus_slogger_strategy, self).__init__()
        self._envelope_percent = self.Param("EnvelopePercent", 1.0) \
            .SetDisplay("Envelope %", "Envelope percent", "Parameters")
        self._trailing_stop = self.Param("TrailingStop", 500.0) \
            .SetDisplay("Trailing Stop", "Trailing stop distance", "Risk")
        self._trailing_step = self.Param("TrailingStep", 100.0) \
            .SetDisplay("Trailing Step", "Minimum stop move", "Risk")
        self._profit_trailing = self.Param("ProfitTrailing", True) \
            .SetDisplay("Profit Trailing", "Trail only after profit", "Risk")
        self._use_time_filter = self.Param("UseTimeFilter", False) \
            .SetDisplay("Use Time Filter", "Enable trading hours filter", "Parameters")
        self._start_hour = self.Param("StartHour", 7) \
            .SetDisplay("Start Hour", "Trading start hour", "Parameters")
        self._stop_hour = self.Param("StopHour", 17) \
            .SetDisplay("Stop Hour", "Trading stop hour", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Working candle timeframe", "Parameters")
        self._sma = None
        self._was_above_upper = False
        self._was_below_lower = False
        self._entry_price = 0.0
        self._peak_price = 0.0
        self._trough_price = 0.0
        self._current_stop = 0.0

    @property
    def envelope_percent(self):
        return self._envelope_percent.Value

    @property
    def trailing_stop(self):
        return self._trailing_stop.Value

    @property
    def trailing_step(self):
        return self._trailing_step.Value

    @property
    def profit_trailing(self):
        return self._profit_trailing.Value

    @property
    def use_time_filter(self):
        return self._use_time_filter.Value

    @property
    def start_hour(self):
        return self._start_hour.Value

    @property
    def stop_hour(self):
        return self._stop_hour.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(forex_fraus_slogger_strategy, self).OnReseted()
        self._sma = None
        self._was_above_upper = False
        self._was_below_lower = False
        self._entry_price = 0.0
        self._peak_price = 0.0
        self._trough_price = 0.0
        self._current_stop = 0.0

    def OnStarted(self, time):
        super(forex_fraus_slogger_strategy, self).OnStarted(time)
        self._sma = ExponentialMovingAverage()
        self._sma.Length = 14

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, basis):
        if candle.State != CandleStates.Finished:
            return
        if not self._sma.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self.use_time_filter and not self._is_within_trading_hours(candle.OpenTime):
            return

        basis = float(basis)
        pct = float(self.envelope_percent) / 100.0
        upper = basis * (1.0 + pct)
        lower = basis * (1.0 - pct)
        close = float(candle.ClosePrice)
        trailing_stop = float(self.trailing_stop)
        trailing_step = float(self.trailing_step)

        if close > upper:
            self._was_above_upper = True
        if close < lower:
            self._was_below_lower = True

        if self._was_above_upper and close <= upper:
            if self.Position >= 0:
                self.SellMarket()
            self._entry_price = close
            self._trough_price = close
            self._current_stop = close + trailing_stop
            self._was_above_upper = False
            return

        if self._was_below_lower and close >= lower:
            if self.Position <= 0:
                self.BuyMarket()
            self._entry_price = close
            self._peak_price = close
            self._current_stop = close - trailing_stop
            self._was_below_lower = False
            return

        if self.Position > 0:
            high = float(candle.HighPrice)
            if high >= self._peak_price + trailing_step:
                self._peak_price = high
                new_stop = self._peak_price - trailing_stop
                if not self.profit_trailing or new_stop > self._entry_price:
                    self._current_stop = max(self._current_stop, new_stop)
            if close <= self._current_stop:
                self.SellMarket()
        elif self.Position < 0:
            low = float(candle.LowPrice)
            if self._trough_price == 0.0 or low <= self._trough_price - trailing_step:
                self._trough_price = low if self._trough_price == 0.0 else min(self._trough_price, low)
                new_stop = self._trough_price + trailing_stop
                if not self.profit_trailing or new_stop < self._entry_price:
                    self._current_stop = new_stop if self._current_stop == 0.0 else min(self._current_stop, new_stop)
            if self._current_stop != 0.0 and close >= self._current_stop:
                self.BuyMarket()

    def _is_within_trading_hours(self, time):
        hour = time.Hour
        sh = int(self.start_hour)
        eh = int(self.stop_hour)
        if sh < eh:
            return hour >= sh and hour < eh
        return hour >= sh or hour < eh

    def CreateClone(self):
        return forex_fraus_slogger_strategy()
