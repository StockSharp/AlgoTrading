import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, JurikMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class color_jjrsx_time_plus_strategy(Strategy):
    def __init__(self):
        super(color_jjrsx_time_plus_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Indicator Timeframe", "Timeframe used for the JJRSX oscillator", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("Indicator Timeframe", "Timeframe used for the JJRSX oscillator", "General")
        self._smoothing_length = self.Param("SmoothingLength", 3) \
            .SetDisplay("Indicator Timeframe", "Timeframe used for the JJRSX oscillator", "General")
        self._signal_shift = self.Param("SignalShift", 1) \
            .SetDisplay("Indicator Timeframe", "Timeframe used for the JJRSX oscillator", "General")
        self._enable_buy_entries = self.Param("EnableBuyEntries", True) \
            .SetDisplay("Indicator Timeframe", "Timeframe used for the JJRSX oscillator", "General")
        self._enable_sell_entries = self.Param("EnableSellEntries", True) \
            .SetDisplay("Indicator Timeframe", "Timeframe used for the JJRSX oscillator", "General")
        self._enable_buy_exit = self.Param("EnableBuyExit", True) \
            .SetDisplay("Indicator Timeframe", "Timeframe used for the JJRSX oscillator", "General")
        self._enable_sell_exit = self.Param("EnableSellExit", True) \
            .SetDisplay("Indicator Timeframe", "Timeframe used for the JJRSX oscillator", "General")
        self._enable_time_exit = self.Param("EnableTimeExit", True) \
            .SetDisplay("Indicator Timeframe", "Timeframe used for the JJRSX oscillator", "General")
        self._holding_minutes = self.Param("HoldingMinutes", 480) \
            .SetDisplay("Indicator Timeframe", "Timeframe used for the JJRSX oscillator", "General")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Indicator Timeframe", "Timeframe used for the JJRSX oscillator", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Indicator Timeframe", "Timeframe used for the JJRSX oscillator", "General")

        self._smoothed_values = new()
        self._rsi = None
        self._smoother = None
        self._entry_time = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_jjrsx_time_plus_strategy, self).OnReseted()
        self._smoothed_values = new()
        self._rsi = None
        self._smoother = None
        self._entry_time = None

    def OnStarted(self, time):
        super(color_jjrsx_time_plus_strategy, self).OnStarted(time)

        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_length
        self.__smoother = JurikMovingAverage()
        self.__smoother.Length = self.smoothing_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return color_jjrsx_time_plus_strategy()
