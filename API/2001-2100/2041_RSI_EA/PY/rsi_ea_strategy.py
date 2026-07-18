import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_ea_strategy(Strategy):

    def __init__(self):
        super(rsi_ea_strategy, self).__init__()

        self._stop_loss = self.Param("StopLoss", 500.0) \
            .SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
        self._take_profit = self.Param("TakeProfit", 1000.0) \
            .SetDisplay("Take Profit", "Take profit in price units", "Risk")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI length", "Indicator")
        self._buy_level = self.Param("BuyLevel", 30.0) \
            .SetDisplay("Buy Level", "RSI oversold threshold", "Indicator")
        self._sell_level = self.Param("SellLevel", 70.0) \
            .SetDisplay("Sell Level", "RSI overbought threshold", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._prev_rsi = 0.0
        self._has_prev_rsi = False

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def BuyLevel(self):
        return self._buy_level.Value

    @BuyLevel.setter
    def BuyLevel(self, value):
        self._buy_level.Value = value

    @property
    def SellLevel(self):
        return self._sell_level.Value

    @SellLevel.setter
    def SellLevel(self, value):
        self._sell_level.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(rsi_ea_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(rsi, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            stopLoss=Unit(self.StopLoss, UnitTypes.Absolute),
            takeProfit=Unit(self.TakeProfit, UnitTypes.Absolute)
        )

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi = float(rsi_value)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rsi
            self._has_prev_rsi = True
            return

        if not self._has_prev_rsi:
            self._prev_rsi = rsi
            self._has_prev_rsi = True
            return

        buy_cross = rsi > float(self.BuyLevel) and self._prev_rsi <= float(self.BuyLevel)
        sell_cross = rsi < float(self.SellLevel) and self._prev_rsi >= float(self.SellLevel)

        if buy_cross and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif sell_cross and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_rsi = rsi

    def OnReseted(self):
        super(rsi_ea_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev_rsi = False

    def CreateClone(self):
        return rsi_ea_strategy()
