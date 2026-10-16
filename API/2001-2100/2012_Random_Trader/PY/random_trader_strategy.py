import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class random_trader_strategy(Strategy):

    def __init__(self):
        super(random_trader_strategy, self).__init__()

        self._take_profit = self.Param("TakeProfit", 10.0) \
            .SetDisplay("Take Profit", "Target profit distance in absolute price units", "Risk")
        self._stop_loss = self.Param("StopLoss", 10.0) \
            .SetDisplay("Stop Loss", "Stop loss distance in absolute price units", "Risk")
        self._cooldown = self.Param("Cooldown", 25) \
            .SetDisplay("Cooldown", "Candles between trades", "General")
        self._signal_seed = self.Param("SignalSeed", 42) \
            .SetDisplay("Signal Seed", "Deterministic seed used for direction selection", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._candle_count = 0
        self._signal_state = 42

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def Cooldown(self):
        return self._cooldown.Value

    @Cooldown.setter
    def Cooldown(self, value):
        self._cooldown.Value = value

    @property
    def SignalSeed(self):
        return self._signal_seed.Value

    @SignalSeed.setter
    def SignalSeed(self, value):
        self._signal_seed.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(random_trader_strategy, self).OnStarted2(time)

        self._candle_count = 0
        self._signal_state = self.SignalSeed

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(self.TakeProfit, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLoss, UnitTypes.Absolute)
        )

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._candle_count += 1

        if self.Position != 0:
            return

        if self._candle_count < self.Cooldown:
            return

        self._candle_count = 0

        self._signal_state = (self._signal_state * 1103515245 + 12345) & 0xFFFFFFFF

        if (self._signal_state & 1) == 0:
            self.BuyMarket()
        else:
            self.SellMarket()

    def OnReseted(self):
        super(random_trader_strategy, self).OnReseted()
        self._candle_count = 0
        self._signal_state = self.SignalSeed

    def CreateClone(self):
        return random_trader_strategy()
