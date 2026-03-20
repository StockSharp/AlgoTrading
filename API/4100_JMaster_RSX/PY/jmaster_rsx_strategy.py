import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, AverageTrueRange

class jmaster_rsx_strategy(Strategy):
    def __init__(self):
        super(jmaster_rsx_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period.", "Indicators")
        self._ema_length = self.Param("EmaLength", 30) \
            .SetDisplay("EMA Length", "EMA trend filter.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period for stops.", "Indicators")
        self._overbought = self.Param("Overbought", 75.0) \
            .SetDisplay("Overbought", "RSI overbought level.", "Signals")
        self._oversold = self.Param("Oversold", 25.0) \
            .SetDisplay("Oversold", "RSI oversold level.", "Signals")

        self._prev_rsi = 0.0
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @property
    def Overbought(self):
        return self._overbought.Value

    @property
    def Oversold(self):
        return self._oversold.Value

    def OnStarted(self, time):
        super(jmaster_rsx_strategy, self).OnStarted(time)

        self._prev_rsi = 0.0
        self._entry_price = 0.0

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiLength
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self._ema, self._atr, self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, rsi_val, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        rv = float(rsi_val)
        ev = float(ema_val)
        av = float(atr_val)

        if self._prev_rsi == 0 or av <= 0:
            self._prev_rsi = rv
            return

        close = float(candle.ClosePrice)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rv
            return

        # Entry
        if self.Position == 0:
            if self._prev_rsi < float(self.Oversold) and rv >= float(self.Oversold) and close > ev:
                self._entry_price = close
                self.BuyMarket()
            elif self._prev_rsi > float(self.Overbought) and rv <= float(self.Overbought) and close < ev:
                self._entry_price = close
                self.SellMarket()

        self._prev_rsi = rv

    def OnReseted(self):
        super(jmaster_rsx_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return jmaster_rsx_strategy()
