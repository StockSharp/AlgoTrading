import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class master_mind3_strategy(Strategy):
    def __init__(self):
        super(master_mind3_strategy, self).__init__()

        self._rsi_period1 = self.Param("RsiPeriod1", 26)
        self._rsi_period2 = self.Param("RsiPeriod2", 27)
        self._rsi_period3 = self.Param("RsiPeriod3", 29)
        self._rsi_period4 = self.Param("RsiPeriod4", 30)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))

        self._was_oversold = False
        self._was_overbought = False

    @property
    def RsiPeriod1(self):
        return self._rsi_period1.Value

    @RsiPeriod1.setter
    def RsiPeriod1(self, value):
        self._rsi_period1.Value = value

    @property
    def RsiPeriod2(self):
        return self._rsi_period2.Value

    @RsiPeriod2.setter
    def RsiPeriod2(self, value):
        self._rsi_period2.Value = value

    @property
    def RsiPeriod3(self):
        return self._rsi_period3.Value

    @RsiPeriod3.setter
    def RsiPeriod3(self, value):
        self._rsi_period3.Value = value

    @property
    def RsiPeriod4(self):
        return self._rsi_period4.Value

    @RsiPeriod4.setter
    def RsiPeriod4(self, value):
        self._rsi_period4.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(master_mind3_strategy, self).OnStarted2(time)

        self._was_oversold = False
        self._was_overbought = False

        rsi1 = RelativeStrengthIndex()
        rsi1.Length = self.RsiPeriod1
        rsi2 = RelativeStrengthIndex()
        rsi2.Length = self.RsiPeriod2
        rsi3 = RelativeStrengthIndex()
        rsi3.Length = self.RsiPeriod3
        rsi4 = RelativeStrengthIndex()
        rsi4.Length = self.RsiPeriod4

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi1, rsi2, rsi3, rsi4, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, rsi1_val, rsi2_val, rsi3_val, rsi4_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        r1 = float(rsi1_val)
        r2 = float(rsi2_val)
        r3 = float(rsi3_val)
        r4 = float(rsi4_val)

        is_oversold = r1 <= 35.0 and r2 <= 35.0 and r3 <= 35.0 and r4 <= 35.0
        is_overbought = r1 >= 65.0 and r2 >= 65.0 and r3 >= 65.0 and r4 >= 65.0

        buy_signal = is_oversold and not self._was_oversold
        sell_signal = is_overbought and not self._was_overbought

        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            self.SellMarket()

        self._was_oversold = is_oversold
        self._was_overbought = is_overbought

    def OnReseted(self):
        super(master_mind3_strategy, self).OnReseted()
        self._was_oversold = False
        self._was_overbought = False

    def CreateClone(self):
        return master_mind3_strategy()
