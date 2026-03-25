import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class rsi_expert_strategy(Strategy):

    def __init__(self):
        super(rsi_expert_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Length of the RSI indicator", "Indicators")
        self._level_up = self.Param("LevelUp", 70.0) \
            .SetDisplay("RSI Overbought", "Upper RSI level triggering a short", "Indicators")
        self._level_down = self.Param("LevelDown", 30.0) \
            .SetDisplay("RSI Oversold", "Lower RSI level triggering a long", "Indicators")
        self._take_profit_percent = self.Param("TakeProfitPercent", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_rsi = 0.0

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def LevelUp(self):
        return self._level_up.Value

    @LevelUp.setter
    def LevelUp(self, value):
        self._level_up.Value = value

    @property
    def LevelDown(self):
        return self._level_down.Value

    @LevelDown.setter
    def LevelDown(self, value):
        self._level_down.Value = value

    @property
    def TakeProfitPercent(self):
        return self._take_profit_percent.Value

    @TakeProfitPercent.setter
    def TakeProfitPercent(self, value):
        self._take_profit_percent.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(rsi_expert_strategy, self).OnStarted(time)

        self._prev_rsi = 0.0

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        self.SubscribeCandles(self.CandleType) \
            .Bind(rsi, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(float(self.TakeProfitPercent), UnitTypes.Percent),
            stopLoss=Unit(float(self.StopLossPercent), UnitTypes.Percent),
            useMarketOrders=True
        )

    def ProcessCandle(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        rsi_f = float(rsi_val)

        if self._prev_rsi == 0.0:
            self._prev_rsi = rsi_f
            return

        level_up = float(self.LevelUp)
        level_down = float(self.LevelDown)

        cross_up = self._prev_rsi < level_down and rsi_f > level_down
        cross_down = self._prev_rsi > level_up and rsi_f < level_up

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_rsi = rsi_f

    def OnReseted(self):
        super(rsi_expert_strategy, self).OnReseted()
        self._prev_rsi = 0.0

    def CreateClone(self):
        return rsi_expert_strategy()
