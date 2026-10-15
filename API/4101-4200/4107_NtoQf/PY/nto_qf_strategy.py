import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, AverageTrueRange

class nto_qf_strategy(Strategy):
    def __init__(self):
        super(nto_qf_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period.", "Indicators")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "EMA trend filter.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period for trailing.", "Indicators")
        self._rsi_upper = self.Param("RsiUpper", 70.0) \
            .SetDisplay("RSI Upper", "Overbought level.", "Signals")
        self._rsi_lower = self.Param("RsiLower", 30.0) \
            .SetDisplay("RSI Lower", "Oversold level.", "Signals")

        self._prev_rsi = 0.0

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
    def RsiUpper(self):
        return self._rsi_upper.Value

    @property
    def RsiLower(self):
        return self._rsi_lower.Value

    def OnStarted2(self, time):
        super(nto_qf_strategy, self).OnStarted2(time)

        self._prev_rsi = 0.0

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiLength
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaLength
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self._ema, self._atr, self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(3, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
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

        # Entry: RSI exits extreme zone, confirmed by EMA trend
        if self.Position == 0:
            if self._prev_rsi < float(self.RsiLower) and rv >= float(self.RsiLower) and close > ev:
                self.BuyMarket()
            elif self._prev_rsi > float(self.RsiUpper) and rv <= float(self.RsiUpper) and close < ev:
                self.SellMarket()

        self._prev_rsi = rv

    def OnReseted(self):
        super(nto_qf_strategy, self).OnReseted()
        self._prev_rsi = 0.0

    def CreateClone(self):
        return nto_qf_strategy()
