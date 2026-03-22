import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class forex_fraus4_for_m1s_strategy(Strategy):

    def __init__(self):
        super(forex_fraus4_for_m1s_strategy, self).__init__()

        self._wpr_period = self.Param("WprPeriod", 100) \
            .SetDisplay("Williams %R Period", "Period for Williams %R", "Indicators")
        self._buy_threshold = self.Param("BuyThreshold", -90.0) \
            .SetDisplay("Buy Threshold", "Level crossing up triggers buy", "Trading")
        self._sell_threshold = self.Param("SellThreshold", -10.0) \
            .SetDisplay("Sell Threshold", "Level crossing down triggers sell", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._was_oversold = False
        self._was_overbought = False

    @property
    def WprPeriod(self):
        return self._wpr_period.Value

    @WprPeriod.setter
    def WprPeriod(self, value):
        self._wpr_period.Value = value

    @property
    def BuyThreshold(self):
        return self._buy_threshold.Value

    @BuyThreshold.setter
    def BuyThreshold(self, value):
        self._buy_threshold.Value = value

    @property
    def SellThreshold(self):
        return self._sell_threshold.Value

    @SellThreshold.setter
    def SellThreshold(self, value):
        self._sell_threshold.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(forex_fraus4_for_m1s_strategy, self).OnStarted(time)

        self._was_oversold = False
        self._was_overbought = False

        wpr = WilliamsR()
        wpr.Length = self.WprPeriod

        self.SubscribeCandles(self.CandleType) \
            .BindEx(wpr, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(2.0, UnitTypes.Percent),
            stopLoss=Unit(1.0, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, wpr_value):
        if candle.State != CandleStates.Finished:
            return

        if not wpr_value.IsFormed:
            return

        wpr = float(wpr_value)
        buy_th = float(self.BuyThreshold)
        sell_th = float(self.SellThreshold)

        if wpr < buy_th:
            self._was_oversold = True

        if wpr > sell_th:
            self._was_overbought = True

        if self._was_oversold and wpr > buy_th and self.Position <= 0:
            self._was_oversold = False
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._was_overbought and wpr < sell_th and self.Position >= 0:
            self._was_overbought = False
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def OnReseted(self):
        super(forex_fraus4_for_m1s_strategy, self).OnReseted()
        self._was_oversold = False
        self._was_overbought = False

    def CreateClone(self):
        return forex_fraus4_for_m1s_strategy()
