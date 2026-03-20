import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class support_resistance_breakout_strategy(Strategy):
    def __init__(self):
        super(support_resistance_breakout_strategy, self).__init__()

        self._range_length = self.Param("RangeLength", 55) \
            .SetDisplay("Range Length", "Candles used to form support/resistance", "General")
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("Range Length", "Candles used to form support/resistance", "General")
        self._stop_loss_points = self.Param("StopLossPoints", 500) \
            .SetDisplay("Range Length", "Candles used to form support/resistance", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 500) \
            .SetDisplay("Range Length", "Candles used to form support/resistance", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Range Length", "Candles used to form support/resistance", "General")

        self._ema = None
        self._highs = new()
        self._lows = new()
        self._support = 0.0
        self._resistance = 0.0
        self._entry_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(support_resistance_breakout_strategy, self).OnReseted()
        self._ema = None
        self._highs = new()
        self._lows = new()
        self._support = 0.0
        self._resistance = 0.0
        self._entry_price = None

    def OnStarted(self, time):
        super(support_resistance_breakout_strategy, self).OnStarted(time)

        self.__ema = ExponentialMovingAverage()
        self.__ema.Length = self.ema_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return support_resistance_breakout_strategy()
