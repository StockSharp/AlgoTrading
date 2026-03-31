import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy

class morning_evening_mfi_strategy(Strategy):
    def __init__(self):
        super(morning_evening_mfi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._mfi_period = self.Param("MfiPeriod", 14)
        self._mfi_low = self.Param("MfiLow", 40.0)
        self._mfi_high = self.Param("MfiHigh", 60.0)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6)

        self._prev_candle = None
        self._prev_prev_candle = None
        self._candles_since_trade = 6

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MfiPeriod(self):
        return self._mfi_period.Value

    @MfiPeriod.setter
    def MfiPeriod(self, value):
        self._mfi_period.Value = value

    @property
    def MfiLow(self):
        return self._mfi_low.Value

    @MfiLow.setter
    def MfiLow(self, value):
        self._mfi_low.Value = value

    @property
    def MfiHigh(self):
        return self._mfi_high.Value

    @MfiHigh.setter
    def MfiHigh(self, value):
        self._mfi_high.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(morning_evening_mfi_strategy, self).OnReseted()
        self._prev_candle = None
        self._prev_prev_candle = None
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted2(self, time):
        super(morning_evening_mfi_strategy, self).OnStarted2(time)
        self._prev_candle = None
        self._prev_prev_candle = None
        self._candles_since_trade = self.SignalCooldownCandles

        mfi = MoneyFlowIndex()
        mfi.Length = self.MfiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(mfi, self._process_candle).Start()

    def _process_candle(self, candle, mfi_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        mfi_val = float(mfi_value)

        if self._prev_candle is not None and self._prev_prev_candle is not None:
            prev_body = abs(float(self._prev_candle.ClosePrice) - float(self._prev_candle.OpenPrice))
            prev_range = float(self._prev_candle.HighPrice) - float(self._prev_candle.LowPrice)
            is_small_body = prev_range > 0 and prev_body < prev_range * 0.3
            first_midpoint = (float(self._prev_prev_candle.OpenPrice) + float(self._prev_prev_candle.ClosePrice)) / 2.0

            first_bearish = float(self._prev_prev_candle.OpenPrice) > float(self._prev_prev_candle.ClosePrice)
            curr_bullish = float(candle.ClosePrice) > float(candle.OpenPrice)
            is_morning_star = first_bearish and is_small_body and curr_bullish and float(candle.ClosePrice) > first_midpoint

            first_bullish = float(self._prev_prev_candle.ClosePrice) > float(self._prev_prev_candle.OpenPrice)
            curr_bearish = float(candle.OpenPrice) > float(candle.ClosePrice)
            is_evening_star = first_bullish and is_small_body and curr_bearish and float(candle.ClosePrice) < first_midpoint

            if is_morning_star and mfi_val < self.MfiLow and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif is_evening_star and mfi_val > self.MfiHigh and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

        self._prev_prev_candle = self._prev_candle
        self._prev_candle = candle

    def CreateClone(self):
        return morning_evening_mfi_strategy()
