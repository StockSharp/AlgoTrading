import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, AverageTrueRange

class ard_order_management_stochastic_strategy(Strategy):
    def __init__(self):
        super(ard_order_management_stochastic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._rsi_length = self.Param("RsiLength", 10) \
            .SetDisplay("RSI Length", "RSI period.", "Indicators")
        self._ema_length = self.Param("EmaLength", 14) \
            .SetDisplay("EMA Length", "EMA trend filter period.", "Indicators")
        self._atr_length = self.Param("AtrLength", 10) \
            .SetDisplay("ATR Length", "ATR period for stops.", "Indicators")
        self._buy_threshold = self.Param("BuyThreshold", 30.0) \
            .SetDisplay("Buy Threshold", "RSI oversold level for buy.", "Signals")
        self._sell_threshold = self.Param("SellThreshold", 70.0) \
            .SetDisplay("Sell Threshold", "RSI overbought level for sell.", "Signals")

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
    def BuyThreshold(self):
        return self._buy_threshold.Value

    @property
    def SellThreshold(self):
        return self._sell_threshold.Value

    def OnStarted2(self, time):
        super(ard_order_management_stochastic_strategy, self).OnStarted2(time)

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
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, rsi_val, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        rv = float(rsi_val)

        if self._prev_rsi == 0:
            self._prev_rsi = rv
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rv
            return

        # Entry: RSI crosses threshold
        if self.Position == 0:
            if rv < float(self.BuyThreshold) and self._prev_rsi >= float(self.BuyThreshold):
                self.BuyMarket()
            elif rv > float(self.SellThreshold) and self._prev_rsi <= float(self.SellThreshold):
                self.SellMarket()

        self._prev_rsi = rv

    def OnReseted(self):
        super(ard_order_management_stochastic_strategy, self).OnReseted()
        self._prev_rsi = 0.0

    def CreateClone(self):
        return ard_order_management_stochastic_strategy()
