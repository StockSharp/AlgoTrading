import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class vlado_williams_percent_range_strategy(Strategy):
    """Vlado Williams %R strategy. Trades level breakouts: goes long when WPR rises
    above the threshold, short when it falls below. Exits on opposite signal."""

    def __init__(self):
        super(vlado_williams_percent_range_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary timeframe for the strategy", "General")
        self._wpr_length = self.Param("WprLength", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Williams %R Period", "Lookback period for Williams %R", "Indicators")
        self._wpr_level = self.Param("WprLevel", -50.0) \
            .SetDisplay("Williams %R Level", "Threshold that flips the bias", "Signals")

        self._buy_signal = False
        self._sell_signal = False
        self._last_signal = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def WprLength(self):
        return self._wpr_length.Value

    @property
    def WprLevel(self):
        return self._wpr_level.Value

    def OnReseted(self):
        super(vlado_williams_percent_range_strategy, self).OnReseted()
        self._buy_signal = False
        self._sell_signal = False
        self._last_signal = 0

    def OnStarted2(self, time):
        super(vlado_williams_percent_range_strategy, self).OnStarted2(time)

        williams_r = WilliamsR()
        williams_r.Length = self.WprLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(williams_r, self._process_candle).Start()

    def _process_candle(self, candle, wpr_value):
        if candle.State != CandleStates.Finished:
            return

        wpr = float(wpr_value)
        wpr_level = float(self.WprLevel)

        # Update signals based on Williams %R relative to threshold
        if wpr > wpr_level:
            self._buy_signal = True
            self._sell_signal = False
        elif wpr < wpr_level:
            self._sell_signal = True
            self._buy_signal = False

        if self.Position != 0:
            if self.Position > 0 and self._sell_signal:
                # Exit long on bearish regime
                self.SellMarket()
                return
            if self.Position < 0 and self._buy_signal:
                # Exit short on bullish regime
                self.BuyMarket()
                return
            return

        # No open position - evaluate entries
        if self._sell_signal and self._last_signal != -1:
            self.SellMarket()
            self._last_signal = -1
            return

        if self._buy_signal and self._last_signal != 1:
            self.BuyMarket()
            self._last_signal = 1

    def CreateClone(self):
        return vlado_williams_percent_range_strategy()
