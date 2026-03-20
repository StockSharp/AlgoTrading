import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, DecimalIndicatorValue, RelativeStrengthIndex, SimpleMovingAverage, SimpleMovingAverage as SMA
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class revised_self_adaptive_ea_strategy(Strategy):
    def __init__(self):
        super(revised_self_adaptive_ea_strategy, self).__init__()

        self._average_body_period = self.Param("AverageBodyPeriod", 3) \
            .SetDisplay("Average body period", "Number of candles used to calculate the average body size filter.", "Pattern")
        self._moving_average_period = self.Param("MovingAveragePeriod", 2) \
            .SetDisplay("Average body period", "Number of candles used to calculate the average body size filter.", "Pattern")
        self._rsi_period = self.Param("RsiPeriod", 6) \
            .SetDisplay("Average body period", "Number of candles used to calculate the average body size filter.", "Pattern")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Average body period", "Number of candles used to calculate the average body size filter.", "Pattern")
        self._volume = self.Param("TradeVolume", 1) \
            .SetDisplay("Average body period", "Number of candles used to calculate the average body size filter.", "Pattern")
        self._max_spread_points = self.Param("MaxSpreadPoints", 20) \
            .SetDisplay("Average body period", "Number of candles used to calculate the average body size filter.", "Pattern")
        self._max_risk_percent = self.Param("MaxRiskPercent", 10) \
            .SetDisplay("Average body period", "Number of candles used to calculate the average body size filter.", "Pattern")
        self._use_trailing_stop = self.Param("UseTrailingStop", True) \
            .SetDisplay("Average body period", "Number of candles used to calculate the average body size filter.", "Pattern")
        self._stop_loss_atr_multiplier = self.Param("StopLossAtrMultiplier", 2) \
            .SetDisplay("Average body period", "Number of candles used to calculate the average body size filter.", "Pattern")
        self._take_profit_atr_multiplier = self.Param("TakeProfitAtrMultiplier", 4) \
            .SetDisplay("Average body period", "Number of candles used to calculate the average body size filter.", "Pattern")
        self._trailing_stop_atr_multiplier = self.Param("TrailingStopAtrMultiplier", 1.5) \
            .SetDisplay("Average body period", "Number of candles used to calculate the average body size filter.", "Pattern")
        self._oversold_level = self.Param("OversoldLevel", 40) \
            .SetDisplay("Average body period", "Number of candles used to calculate the average body size filter.", "Pattern")
        self._overbought_level = self.Param("OverboughtLevel", 60) \
            .SetDisplay("Average body period", "Number of candles used to calculate the average body size filter.", "Pattern")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Average body period", "Number of candles used to calculate the average body size filter.", "Pattern")

        self._rsi = null!
        self._moving_average = null!
        self._atr = null!
        self._body_average = null!
        self._previous_candle = None
        self._last_atr_value = 0.0
        self._average_body_value = 0.0
        self._pip_size = 0.0
        self._pip_size_initialized = False
        self._long_entry_price = None
        self._long_stop_price = None
        self._long_take_profit_price = None
        self._long_trailing_stop_price = None
        self._short_entry_price = None
        self._short_stop_price = None
        self._short_take_profit_price = None
        self._short_trailing_stop_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(revised_self_adaptive_ea_strategy, self).OnReseted()
        self._rsi = null!
        self._moving_average = null!
        self._atr = null!
        self._body_average = null!
        self._previous_candle = None
        self._last_atr_value = 0.0
        self._average_body_value = 0.0
        self._pip_size = 0.0
        self._pip_size_initialized = False
        self._long_entry_price = None
        self._long_stop_price = None
        self._long_take_profit_price = None
        self._long_trailing_stop_price = None
        self._short_entry_price = None
        self._short_stop_price = None
        self._short_take_profit_price = None
        self._short_trailing_stop_price = None

    def OnStarted(self, time):
        super(revised_self_adaptive_ea_strategy, self).OnStarted(time)
        self.StartProtection(None, None)

        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period
        self.__moving_average = SMA()
        self.__moving_average.Length = self.moving_average_period
        self.__atr = AverageTrueRange()
        self.__atr.Length = self.atr_period
        self.__body_average = SMA()
        self.__body_average.Length = self.average_body_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__rsi, self.__moving_average, self.__atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return revised_self_adaptive_ea_strategy()
