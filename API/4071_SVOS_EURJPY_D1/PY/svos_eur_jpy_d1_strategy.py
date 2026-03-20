import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage

class svos_eur_jpy_d1_strategy(Strategy):
    def __init__(self):
        super(svos_eur_jpy_d1_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._adx_length = self.Param("AdxLength", 14) \
            .SetDisplay("ADX Length", "Period for ADX", "Indicators")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "Period for trend EMA", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetDisplay("ADX Threshold", "ADX level to distinguish trend from range", "Indicators")

        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def AdxLength(self):
        return self._adx_length.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    @property
    def AdxThreshold(self):
        return self._adx_threshold.Value

    def OnStarted(self, time):
        super(svos_eur_jpy_d1_strategy, self).OnStarted(time)

        self._entry_price = 0.0

        self._atr = AverageTrueRange()
        self._atr.Length = self.AdxLength
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self._ema, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, adx_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        adx_val = float(adx_value)
        ema_val = float(ema_value)
        threshold = float(self.AdxThreshold)
        is_trending = adx_val > threshold

        # Position management
        if self.Position > 0:
            if is_trending and close < ema_val:
                self.SellMarket()
            elif not is_trending and close >= ema_val:
                self.SellMarket()
        elif self.Position < 0:
            if is_trending and close > ema_val:
                self.BuyMarket()
            elif not is_trending and close <= ema_val:
                self.BuyMarket()

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Entry
        if self.Position == 0:
            if is_trending:
                if close > ema_val:
                    self._entry_price = close
                    self.BuyMarket()
                elif close < ema_val:
                    self._entry_price = close
                    self.SellMarket()
            else:
                deviation = abs(close - ema_val)
                if deviation > 0 and close < ema_val:
                    self._entry_price = close
                    self.BuyMarket()
                elif deviation > 0 and close > ema_val:
                    self._entry_price = close
                    self.SellMarket()

    def OnReseted(self):
        super(svos_eur_jpy_d1_strategy, self).OnReseted()
        self._entry_price = 0.0

    def CreateClone(self):
        return svos_eur_jpy_d1_strategy()
