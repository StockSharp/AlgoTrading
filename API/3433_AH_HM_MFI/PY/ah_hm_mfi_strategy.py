import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class ah_hm_mfi_strategy(Strategy):
    def __init__(self):
        super(ah_hm_mfi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._mfi_period = self.Param("MfiPeriod", 14)
        self._mfi_low = self.Param("MfiLow", 35.0)
        self._mfi_high = self.Param("MfiHigh", 65.0)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 8)

        self._candles_since_trade = 8

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
        super(ah_hm_mfi_strategy, self).OnReseted()
        self._candles_since_trade = self.SignalCooldownCandles

    def OnStarted(self, time):
        super(ah_hm_mfi_strategy, self).OnStarted(time)
        self._candles_since_trade = self.SignalCooldownCandles

        rsi = RelativeStrengthIndex()
        rsi.Length = self.MfiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, mfi_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        mfi_val = float(mfi_value)

        body = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        rng = float(candle.HighPrice) - float(candle.LowPrice)
        if rng <= 0 or body <= 0:
            return

        upper_shadow = float(candle.HighPrice) - max(float(candle.OpenPrice), float(candle.ClosePrice))
        lower_shadow = min(float(candle.OpenPrice), float(candle.ClosePrice)) - float(candle.LowPrice)

        is_hammer = lower_shadow > body * 2.5 and upper_shadow < body * 0.5
        is_hanging_man = upper_shadow > body * 2.5 and lower_shadow < body * 0.5

        if is_hammer and mfi_val < self.MfiLow and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.BuyMarket()
            self._candles_since_trade = 0
        elif is_hanging_man and mfi_val > self.MfiHigh and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
            self.SellMarket()
            self._candles_since_trade = 0

    def CreateClone(self):
        return ah_hm_mfi_strategy()
