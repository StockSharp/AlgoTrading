import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class i_cho__trend_cci_dual_on_ma__filter_strategy(Strategy):
    def __init__(self):
        super(i_cho__trend_cci_dual_on_ma__filter_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._cci_length = self.Param("CciLength", 14)
        self._cci_level = self.Param("CciLevel", 100.0)
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4)

        self._prev_cci = 0.0
        self._prev_prev_cci = 0.0
        self._candles_since_trade = 4
        self._has_two = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CciLength(self):
        return self._cci_length.Value

    @CciLength.setter
    def CciLength(self, value):
        self._cci_length.Value = value

    @property
    def CciLevel(self):
        return self._cci_level.Value

    @CciLevel.setter
    def CciLevel(self, value):
        self._cci_level.Value = value

    @property
    def SignalCooldownCandles(self):
        return self._signal_cooldown_candles.Value

    @SignalCooldownCandles.setter
    def SignalCooldownCandles(self, value):
        self._signal_cooldown_candles.Value = value

    def OnReseted(self):
        super(i_cho__trend_cci_dual_on_ma__filter_strategy, self).OnReseted()
        self._prev_cci = 0.0
        self._prev_prev_cci = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_two = False

    def OnStarted2(self, time):
        super(i_cho__trend_cci_dual_on_ma__filter_strategy, self).OnStarted2(time)
        self._prev_cci = 0.0
        self._prev_prev_cci = 0.0
        self._candles_since_trade = self.SignalCooldownCandles
        self._has_two = False

        cci = CommodityChannelIndex()
        cci.Length = self.CciLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(cci, self._process_candle).Start()

    def _process_candle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self.SignalCooldownCandles:
            self._candles_since_trade += 1

        cci_val = float(cci_value)
        level = float(self.CciLevel)

        if self._has_two:
            # Both prev_prev and prev were below -level, now crossing above -level
            if self._prev_prev_cci < -level and self._prev_cci < -level and cci_val > -level and self.Position <= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.BuyMarket()
                self._candles_since_trade = 0
            # Both prev_prev and prev were above +level, now crossing below +level
            elif self._prev_prev_cci > level and self._prev_cci > level and cci_val < level and self.Position >= 0 and self._candles_since_trade >= self.SignalCooldownCandles:
                self.SellMarket()
                self._candles_since_trade = 0

        self._prev_prev_cci = self._prev_cci
        self._prev_cci = cci_val
        if not self._has_two:
            if self._prev_prev_cci != 0.0 or self._prev_cci != 0.0:
                self._has_two = True

    def CreateClone(self):
        return i_cho__trend_cci_dual_on_ma__filter_strategy()
