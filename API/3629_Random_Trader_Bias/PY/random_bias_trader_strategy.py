import clr
import random

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class random_bias_trader_strategy(Strategy):
    def __init__(self):
        super(random_bias_trader_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._reward_risk_ratio = self.Param("RewardRiskRatio", 3.0)
        self._atr_multiplier = self.Param("AtrMultiplier", 3.0)
        self._atr_period = self.Param("AtrPeriod", 14)

        self._rng = None
        self._entry_price = 0.0
        self._direction = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RewardRiskRatio(self):
        return self._reward_risk_ratio.Value

    @RewardRiskRatio.setter
    def RewardRiskRatio(self, value):
        self._reward_risk_ratio.Value = value

    @property
    def AtrMultiplier(self):
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    def OnReseted(self):
        super(random_bias_trader_strategy, self).OnReseted()
        self._rng = None
        self._entry_price = 0.0
        self._direction = 0

    def OnStarted(self, time):
        super(random_bias_trader_strategy, self).OnStarted(time)
        self._rng = random.Random(42)
        self._entry_price = 0.0
        self._direction = 0

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self._process_candle).Start()

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        atr_val = float(atr_value)
        if atr_val <= 0:
            return

        close = float(candle.ClosePrice)
        atr_mult = float(self.AtrMultiplier)
        rr_ratio = float(self.RewardRiskRatio)
        stop_distance = atr_val * atr_mult
        take_distance = stop_distance * rr_ratio

        # Check exit for existing position
        if self._direction > 0:
            if close >= self._entry_price + take_distance or close <= self._entry_price - stop_distance:
                self.SellMarket()
                self._direction = 0
            return
        elif self._direction < 0:
            if close <= self._entry_price - take_distance or close >= self._entry_price + stop_distance:
                self.BuyMarket()
                self._direction = 0
            return

        # Open new random position
        if self._rng.randint(0, 3) != 0:
            return

        if self._rng.randint(0, 1) == 0:
            self.BuyMarket()
            self._entry_price = close
            self._direction = 1
        else:
            self.SellMarket()
            self._entry_price = close
            self._direction = -1

    def CreateClone(self):
        return random_bias_trader_strategy()
