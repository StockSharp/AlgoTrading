import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class macd_pattern_trader_trigger_strategy(Strategy):
    def __init__(self):
        super(macd_pattern_trader_trigger_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Trade Volume", "Order volume for entries", "Trading")
        self._fast_period = self.Param("FastPeriod", 13) \
            .SetDisplay("Trade Volume", "Order volume for entries", "Trading")
        self._slow_period = self.Param("SlowPeriod", 5) \
            .SetDisplay("Trade Volume", "Order volume for entries", "Trading")
        self._signal_period = self.Param("SignalPeriod", 1) \
            .SetDisplay("Trade Volume", "Order volume for entries", "Trading")
        self._bullish_trigger = self.Param("BullishTrigger", 50) \
            .SetDisplay("Trade Volume", "Order volume for entries", "Trading")
        self._bullish_reset = self.Param("BullishReset", 20) \
            .SetDisplay("Trade Volume", "Order volume for entries", "Trading")
        self._bearish_trigger = self.Param("BearishTrigger", 50) \
            .SetDisplay("Trade Volume", "Order volume for entries", "Trading")
        self._bearish_reset = self.Param("BearishReset", 20) \
            .SetDisplay("Trade Volume", "Order volume for entries", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 100) \
            .SetDisplay("Trade Volume", "Order volume for entries", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 300) \
            .SetDisplay("Trade Volume", "Order volume for entries", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Trade Volume", "Order volume for entries", "Trading")

        self._macd = null!
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None
        self._bullish_armed = False
        self._bullish_window = False
        self._bullish_ready = False
        self._bearish_armed = False
        self._bearish_window = False
        self._bearish_ready = False
        self._price_step = 1

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_pattern_trader_trigger_strategy, self).OnReseted()
        self._macd = null!
        self._macd_prev1 = None
        self._macd_prev2 = None
        self._macd_prev3 = None
        self._bullish_armed = False
        self._bullish_window = False
        self._bullish_ready = False
        self._bearish_armed = False
        self._bearish_window = False
        self._bearish_ready = False
        self._price_step = 1

    def OnStarted(self, time):
        super(macd_pattern_trader_trigger_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(_macd, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return macd_pattern_trader_trigger_strategy()
