import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, DateTime
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class forex_profit_boost_strategy(Strategy):

    def __init__(self):
        super(forex_profit_boost_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 7) \
            .SetDisplay("Fast EMA Period", "Period of the fast EMA", "Parameters")
        self._slow_period = self.Param("SlowPeriod", 21) \
            .SetDisplay("Slow SMA Period", "Period of the slow SMA", "Parameters")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Stop loss distance in price points", "Risk Management")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Take profit distance in price points", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._was_fast_above_slow = None
        self._entry_price = 0.0
        self._is_long_position = False
        self._last_signal_time = DateTime.MinValue
        self._signal_cooldown = TimeSpan.FromHours(18)

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _check_risk(self, current_price):
        if self.Position == 0 or self._entry_price == 0.0:
            return

        if self._is_long_position:
            if self.StopLoss > 0.0 and current_price <= self._entry_price - self.StopLoss:
                self.SellMarket(self.Position)
                self._entry_price = 0.0
                return
            if self.TakeProfit > 0.0 and current_price >= self._entry_price + self.TakeProfit:
                self.SellMarket(self.Position)
                self._entry_price = 0.0
        else:
            if self.StopLoss > 0.0 and current_price >= self._entry_price + self.StopLoss:
                self.BuyMarket(abs(self.Position))
                self._entry_price = 0.0
                return
            if self.TakeProfit > 0.0 and current_price <= self._entry_price - self.TakeProfit:
                self.BuyMarket(abs(self.Position))
                self._entry_price = 0.0

    def OnStarted2(self, time):
        super(forex_profit_boost_strategy, self).OnStarted2(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastPeriod
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(fast_ema, slow_sma, self.ProcessCandle) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_sma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)
        is_fast_above_slow = fast_val > slow_val

        if self._was_fast_above_slow is None:
            self._was_fast_above_slow = is_fast_above_slow
            return

        is_bullish_signal = self._was_fast_above_slow and not is_fast_above_slow
        is_bearish_signal = not self._was_fast_above_slow and is_fast_above_slow

        if ((is_bullish_signal or is_bearish_signal)
                and self._last_signal_time != DateTime.MinValue
                and candle.CloseTime < self._last_signal_time + self._signal_cooldown):
            self._was_fast_above_slow = is_fast_above_slow
            self._check_risk(float(candle.ClosePrice))
            return

        if is_bullish_signal:
            if self.Position <= 0:
                volume = abs(self.Position) + self.Volume if self.Position < 0 else self.Volume
                self.BuyMarket(volume)
                self._entry_price = float(candle.ClosePrice)
                self._is_long_position = True
                self._last_signal_time = candle.CloseTime
        elif is_bearish_signal:
            if self.Position >= 0:
                volume = self.Position + self.Volume if self.Position > 0 else self.Volume
                self.SellMarket(volume)
                self._entry_price = float(candle.ClosePrice)
                self._is_long_position = False
                self._last_signal_time = candle.CloseTime

        self._was_fast_above_slow = is_fast_above_slow
        self._check_risk(float(candle.ClosePrice))

    def OnReseted(self):
        super(forex_profit_boost_strategy, self).OnReseted()
        self._was_fast_above_slow = None
        self._entry_price = 0.0
        self._is_long_position = False
        self._last_signal_time = DateTime.MinValue

    def CreateClone(self):
        return forex_profit_boost_strategy()
