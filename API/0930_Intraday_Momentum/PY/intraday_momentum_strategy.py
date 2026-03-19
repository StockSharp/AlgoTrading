import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, VolumeWeightedAveragePrice
from StockSharp.Algo.Strategies import Strategy

class intraday_momentum_strategy(Strategy):
    """
    Intraday momentum using EMA crossover, RSI and VWAP filters.
    Trades within session hours with percentage-based SL/TP.
    """

    def __init__(self):
        super(intraday_momentum_strategy, self).__init__()
        self._ema_fast_length = self.Param("EmaFastLength", 9) \
            .SetDisplay("Fast EMA Length", "Period for fast EMA", "Indicators")
        self._ema_slow_length = self.Param("EmaSlowLength", 21) \
            .SetDisplay("Slow EMA Length", "Period for slow EMA", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetDisplay("RSI Overbought", "Overbought level", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30) \
            .SetDisplay("RSI Oversold", "Oversold level", "Indicators")
        self._stop_loss_perc = self.Param("StopLossPerc", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        self._take_profit_perc = self.Param("TakeProfitPerc", 2.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Session Start Hour", "Trading session start hour", "Session")
        self._start_minute = self.Param("StartMinute", 0) \
            .SetDisplay("Session Start Minute", "Trading session start minute", "Session")
        self._end_hour = self.Param("EndHour", 23) \
            .SetDisplay("Session End Hour", "Trading session end hour", "Session")
        self._end_minute = self.Param("EndMinute", 59) \
            .SetDisplay("Session End Minute", "Trading session end minute", "Session")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_set = False
        self._entry_price = 0.0
        self._stop_loss = 0.0
        self._take_profit = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(intraday_momentum_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_set = False
        self._entry_price = 0.0
        self._stop_loss = 0.0
        self._take_profit = 0.0

    def OnStarted(self, time):
        super(intraday_momentum_strategy, self).OnStarted(time)

        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = self._ema_fast_length.Value
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = self._ema_slow_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        vwap = VolumeWeightedAveragePrice()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema_fast, ema_slow, rsi, vwap, self._process_candle).Start()

    def _process_candle(self, candle, ema_fast_val, ema_slow_val, rsi_val, vwap_val):
        if candle.State != CandleStates.Finished:
            return

        if ema_fast_val.IsEmpty or ema_slow_val.IsEmpty or rsi_val.IsEmpty or vwap_val.IsEmpty:
            return

        ema_fast = float(ema_fast_val.ToDecimal())
        ema_slow = float(ema_slow_val.ToDecimal())
        rsi = float(rsi_val.ToDecimal())
        vwap = float(vwap_val.ToDecimal())
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        time_of_day = candle.OpenTime.TimeOfDay
        start = TimeSpan(self._start_hour.Value, self._start_minute.Value, 0)
        end = TimeSpan(self._end_hour.Value, self._end_minute.Value, 0)
        in_session = time_of_day >= start and time_of_day <= end

        if not in_session:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            return

        if not self._prev_set:
            self._prev_fast = ema_fast
            self._prev_slow = ema_slow
            self._prev_set = True
            return

        crossover = self._prev_fast <= self._prev_slow and ema_fast > ema_slow
        crossunder = self._prev_fast >= self._prev_slow and ema_fast < ema_slow

        self._prev_fast = ema_fast
        self._prev_slow = ema_slow

        sl_pct = self._stop_loss_perc.Value
        tp_pct = self._take_profit_perc.Value

        long_cond = crossover and rsi < self._rsi_overbought.Value and close > vwap
        short_cond = crossunder and rsi > self._rsi_oversold.Value and close < vwap

        if long_cond and self.Position <= 0:
            self._entry_price = close
            self._stop_loss = close * (1.0 - sl_pct / 100.0)
            self._take_profit = close * (1.0 + tp_pct / 100.0)
            self.BuyMarket()
        elif short_cond and self.Position >= 0:
            self._entry_price = close
            self._stop_loss = close * (1.0 + sl_pct / 100.0)
            self._take_profit = close * (1.0 - tp_pct / 100.0)
            self.SellMarket()

        if self.Position > 0:
            if low <= self._stop_loss or high >= self._take_profit:
                self.SellMarket()
        elif self.Position < 0:
            if high >= self._stop_loss or low <= self._take_profit:
                self.BuyMarket()

    def CreateClone(self):
        return intraday_momentum_strategy()
