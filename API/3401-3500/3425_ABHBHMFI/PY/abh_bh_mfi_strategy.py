import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy

class abh_bh_mfi_strategy(Strategy):
    def __init__(self):
        super(abh_bh_mfi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._mfi_period = self.Param("MfiPeriod", 14)
        self._oversold = self.Param("Oversold", 40.0)
        self._overbought = self.Param("Overbought", 60.0)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6)

        self._candles = []
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
        super(abh_bh_mfi_strategy, self).OnReseted()
        self._candles.clear()
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted2(self, time):
        super(abh_bh_mfi_strategy, self).OnStarted2(time)
        self._candles.clear()
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

            bullish_harami = (float(prev.OpenPrice) > float(prev.ClosePrice)
                and float(curr.ClosePrice) > float(curr.OpenPrice)
                and float(curr.OpenPrice) > float(prev.ClosePrice)
                and float(curr.ClosePrice) < float(prev.OpenPrice))

            bearish_harami = (float(prev.ClosePrice) > float(prev.OpenPrice)
                and float(curr.OpenPrice) > float(curr.ClosePrice)
                and float(curr.ClosePrice) > float(prev.OpenPrice)
                and float(curr.OpenPrice) < float(prev.ClosePrice))

            if bullish_harami and mfi_val < self.Oversold and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif bearish_harami and mfi_val > self.Overbought and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

    def CreateClone(self):
        return abh_bh_mfi_strategy()
