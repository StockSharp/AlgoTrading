import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class reduce_risks_strategy(Strategy):
    def __init__(self):
        super(reduce_risks_strategy, self).__init__()

        self._stop_loss_pips = self.Param("StopLossPips", 30) \
            .SetDisplay("Stop Loss", "Protective stop distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 60) \
            .SetDisplay("Stop Loss", "Protective stop distance in pips", "Risk")
        self._initial_deposit = self.Param("InitialDeposit", 1000) \
            .SetDisplay("Stop Loss", "Protective stop distance in pips", "Risk")
        self._risk_percent = self.Param("RiskPercent", 5) \
            .SetDisplay("Stop Loss", "Protective stop distance in pips", "Risk")
        self._m1_candle_type = self.Param("M1CandleType", TimeSpan.FromMinutes(1) \
            .SetDisplay("Stop Loss", "Protective stop distance in pips", "Risk")
        self._m15_candle_type = self.Param("M15CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Stop Loss", "Protective stop distance in pips", "Risk")
        self._h1_candle_type = self.Param("H1CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Stop Loss", "Protective stop distance in pips", "Risk")

        self._m1_sma5 = None
        self._m1_sma8 = None
        self._m1_sma13 = None
        self._m1_sma60 = None
        self._m15_sma4 = None
        self._m15_sma5 = None
        self._m15_sma8 = None
        self._h1_sma24 = None
        self._m1_sma5_values = None
        self._m1_sma8_values = None
        self._m1_sma13_values = None
        self._m1_sma60_values = None
        self._m15_sma4_values = None
        self._m15_sma5_values = None
        self._m15_sma8_values = None
        self._h1_sma24_values = None
        self._m1_prev1 = None
        self._m1_prev2 = None
        self._m1_prev3 = None
        self._m15_prev1 = None
        self._m15_prev2 = None
        self._m15_prev3 = None
        self._pip_size = 0.0
        self._price_step = 0.0
        self._risk_threshold = 0.0
        self._risk_exceeded_counter = 0.0
        self._highest_since_entry = None
        self._lowest_since_entry = None
        self._long_entry_time = None
        self._short_entry_time = None
        self._long_bars_since_entry = 0.0
        self._short_bars_since_entry = 0.0
        self._previous_position = 0.0
        self._entry_price = 0.0

    def OnReseted(self):
        super(reduce_risks_strategy, self).OnReseted()
        self._m1_sma5 = None
        self._m1_sma8 = None
        self._m1_sma13 = None
        self._m1_sma60 = None
        self._m15_sma4 = None
        self._m15_sma5 = None
        self._m15_sma8 = None
        self._h1_sma24 = None
        self._m1_sma5_values = None
        self._m1_sma8_values = None
        self._m1_sma13_values = None
        self._m1_sma60_values = None
        self._m15_sma4_values = None
        self._m15_sma5_values = None
        self._m15_sma8_values = None
        self._h1_sma24_values = None
        self._m1_prev1 = None
        self._m1_prev2 = None
        self._m1_prev3 = None
        self._m15_prev1 = None
        self._m15_prev2 = None
        self._m15_prev3 = None
        self._pip_size = 0.0
        self._price_step = 0.0
        self._risk_threshold = 0.0
        self._risk_exceeded_counter = 0.0
        self._highest_since_entry = None
        self._lowest_since_entry = None
        self._long_entry_time = None
        self._short_entry_time = None
        self._long_bars_since_entry = 0.0
        self._short_bars_since_entry = 0.0
        self._previous_position = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(reduce_risks_strategy, self).OnStarted(time)

        self.__m1_sma5 = SimpleMovingAverage()
        self.__m1_sma5.Length = 5
        self.__m1_sma8 = SimpleMovingAverage()
        self.__m1_sma8.Length = 8
        self.__m1_sma13 = SimpleMovingAverage()
        self.__m1_sma13.Length = 13
        self.__m1_sma60 = SimpleMovingAverage()
        self.__m1_sma60.Length = 60
        self.__m15_sma4 = SimpleMovingAverage()
        self.__m15_sma4.Length = 4
        self.__m15_sma5 = SimpleMovingAverage()
        self.__m15_sma5.Length = 5
        self.__m15_sma8 = SimpleMovingAverage()
        self.__m15_sma8.Length = 8
        self.__h1_sma24 = SimpleMovingAverage()
        self.__h1_sma24.Length = 24

        m1_subscription = self.SubscribeCandles(M1self.candle_type)
        m1_subscription.Bind(self._process_candle).Start()

        m15_subscription = self.SubscribeCandles(M15self.candle_type)
        m15_subscription.Bind(self._process_candle).Start()

        h1_subscription = self.SubscribeCandles(H1self.candle_type)
        h1_subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return reduce_risks_strategy()
