import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class scalping_ea_strategy(Strategy):
    def __init__(self):
        super(scalping_ea_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 7).SetGreaterThanZero().SetDisplay("RSI Period", "RSI period", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 20).SetGreaterThanZero().SetDisplay("EMA Period", "EMA trend filter period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period for stops", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(scalping_ea_strategy, self).OnReseted()
        self._entry_price = 0

    def OnStarted2(self, time):
        super(scalping_ea_strategy, self).OnStarted2(time)
        self._entry_price = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = StandardDeviation()
        atr.Length = self._atr_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(rsi, ema, atr, self.OnProcess).Start()

    def OnProcess(self, candle, rsi_val, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if atr_val <= 0:
            return

        close = float(candle.ClosePrice)

        # Buy: RSI oversold
        if rsi_val < 35 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
        # Sell: RSI overbought
        elif rsi_val > 65 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
        # Exit long: price below EMA or stop loss
        elif self.Position > 0:
            if close < ema_val or (self._entry_price > 0 and close <= self._entry_price - atr_val * 2):
                self.SellMarket()
                self._entry_price = 0
        # Exit short: price above EMA or stop loss
        elif self.Position < 0:
            if close > ema_val or (self._entry_price > 0 and close >= self._entry_price + atr_val * 2):
                self.BuyMarket()
                self._entry_price = 0

    def CreateClone(self):
        return scalping_ea_strategy()
