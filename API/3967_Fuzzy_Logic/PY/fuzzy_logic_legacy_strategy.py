import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, SmoothedMovingAverage, WilliamsR
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class fuzzy_logic_legacy_strategy(Strategy):
    def __init__(self):
        super(fuzzy_logic_legacy_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Candle Type", "Type of candles", "Data")
        self._long_threshold = self.Param("LongThreshold", 0.75) \
            .SetDisplay("Candle Type", "Type of candles", "Data")
        self._short_threshold = self.Param("ShortThreshold", 0.25) \
            .SetDisplay("Candle Type", "Type of candles", "Data")
        self._stop_loss_points = self.Param("StopLossPoints", 60) \
            .SetDisplay("Candle Type", "Type of candles", "Data")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 35) \
            .SetDisplay("Candle Type", "Type of candles", "Data")
        self._fixed_volume = self.Param("FixedVolume", 0.1) \
            .SetDisplay("Candle Type", "Type of candles", "Data")
        self._use_money_management = self.Param("UseMoneyManagement", False) \
            .SetDisplay("Candle Type", "Type of candles", "Data")
        self._percent_money_management = self.Param("PercentMoneyManagement", 8) \
            .SetDisplay("Candle Type", "Type of candles", "Data")
        self._delta_money_management = self.Param("DeltaMoneyManagement", 0) \
            .SetDisplay("Candle Type", "Type of candles", "Data")
        self._initial_balance = self.Param("InitialBalance", 10000) \
            .SetDisplay("Candle Type", "Type of candles", "Data")

        self._williams_indicator = null!
        self._rsi_indicator = null!
        self._jaw = new() { Length = 13 }
        self._teeth = new() { Length = 8 }
        self._lips = new() { Length = 5 }
        self._ao_fast = new() { Length = 5 }
        self._ao_slow = new() { Length = 34 }
        self._ac_average = new() { Length = 5 }
        self._jaw_count = 0.0
        self._teeth_count = 0.0
        self._lips_count = 0.0
        self._ac_count = 0.0
        self._de_max_queue = new()
        self._de_min_queue = new()
        self._de_max_sum = 0.0
        self._de_min_sum = 0.0
        self._previous_high = None
        self._previous_low = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fuzzy_logic_legacy_strategy, self).OnReseted()
        self._williams_indicator = null!
        self._rsi_indicator = null!
        self._jaw = new() { Length = 13 }
        self._teeth = new() { Length = 8 }
        self._lips = new() { Length = 5 }
        self._ao_fast = new() { Length = 5 }
        self._ao_slow = new() { Length = 34 }
        self._ac_average = new() { Length = 5 }
        self._jaw_count = 0.0
        self._teeth_count = 0.0
        self._lips_count = 0.0
        self._ac_count = 0.0
        self._de_max_queue = new()
        self._de_min_queue = new()
        self._de_max_sum = 0.0
        self._de_min_sum = 0.0
        self._previous_high = None
        self._previous_low = None

    def OnStarted(self, time):
        super(fuzzy_logic_legacy_strategy, self).OnStarted(time)
        self.StartProtection(None, None)

        self.__williams_indicator = WilliamsR()
        self.__williams_indicator.Length = 14
        self.__rsi_indicator = RelativeStrengthIndex()
        self.__rsi_indicator.Length = 14

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self.__williams_indicator, self.__rsi_indicator, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return fuzzy_logic_legacy_strategy()
