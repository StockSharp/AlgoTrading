import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy

class engulfing_mfi_confirmation_strategy(Strategy):
    def __init__(self):
        super(engulfing_mfi_confirmation_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._mfi_period = self.Param("MfiPeriod", 14)
        self._oversold = self.Param("Oversold", 30.0)
        self._overbought = self.Param("Overbought", 70.0)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6)

        self._candles = []
        self._prev_mfi = 0.0
        self._has_prev_mfi = False
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
    def Oversold(self):
        return self._oversold.Value

    @Oversold.setter
    def Oversold(self, value):
        self._oversold.Value = value

    @property
    def Overbought(self):
        return self._overbought.Value

    @Overbought.setter
    def Overbought(self, value):
        self._overbought.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(engulfing_mfi_confirmation_strategy, self).OnReseted()
        self._candles.clear()
        self._prev_mfi = 0.0
        self._has_prev_mfi = False
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted(self, time):
        super(engulfing_mfi_confirmation_strategy, self).OnStarted(time)
        self._candles.clear()
        self._has_prev_mfi = False
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

        self._candles.append(candle)
        if len(self._candles) > 5:
            self._candles.pop(0)

        if len(self._candles) >= 2:
            curr = self._candles[-1]
            prev = self._candles[-2]

            if curr is None or prev is None:
                return

            bullish_engulfing = (float(prev.OpenPrice) > float(prev.ClosePrice)
                and float(curr.ClosePrice) > float(curr.OpenPrice)
                and float(curr.OpenPrice) <= float(prev.ClosePrice)
                and float(curr.ClosePrice) >= float(prev.OpenPrice))

            bearish_engulfing = (float(prev.ClosePrice) > float(prev.OpenPrice)
                and float(curr.OpenPrice) > float(curr.ClosePrice)
                and float(curr.OpenPrice) >= float(prev.ClosePrice)
                and float(curr.ClosePrice) <= float(prev.OpenPrice))

            if bullish_engulfing and mfi_val < self.Oversold and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif bearish_engulfing and mfi_val > self.Overbought and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

        if self._has_prev_mfi:
            if self.Position > 0 and self._prev_mfi >= self.Overbought and mfi_val < self.Overbought and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0
            elif self.Position < 0 and self._prev_mfi <= self.Oversold and mfi_val > self.Oversold and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0

        self._prev_mfi = mfi_val
        self._has_prev_mfi = True

    def CreateClone(self):
        return engulfing_mfi_confirmation_strategy()
