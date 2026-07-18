import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class super_woodies_cci_strategy(Strategy):
    def __init__(self):
        super(super_woodies_cci_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 50) \
            .SetDisplay("CCI Period", "CCI lookback length", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._allow_long_entry = self.Param("AllowLongEntry", True) \
            .SetDisplay("Allow Long Entry", "Enable opening long positions", "Trading")
        self._allow_short_entry = self.Param("AllowShortEntry", True) \
            .SetDisplay("Allow Short Entry", "Enable opening short positions", "Trading")
        self._allow_long_exit = self.Param("AllowLongExit", True) \
            .SetDisplay("Allow Long Exit", "Enable closing long positions", "Trading")
        self._allow_short_exit = self.Param("AllowShortExit", True) \
            .SetDisplay("Allow Short Exit", "Enable closing short positions", "Trading")
        self._cci = None
        self._has_prev = False
        self._was_positive = False

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def allow_long_entry(self):
        return self._allow_long_entry.Value

    @property
    def allow_short_entry(self):
        return self._allow_short_entry.Value

    @property
    def allow_long_exit(self):
        return self._allow_long_exit.Value

    @property
    def allow_short_exit(self):
        return self._allow_short_exit.Value

    def OnReseted(self):
        super(super_woodies_cci_strategy, self).OnReseted()
        self._has_prev = False
        self._was_positive = False
        self._cci = None

    def OnStarted2(self, time):
        super(super_woodies_cci_strategy, self).OnStarted2(time)
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.cci_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._cci, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._cci)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._cci.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return

        is_positive = float(cci_value) > 0.0

        if self._has_prev and is_positive != self._was_positive:
            if is_positive:
                if self.allow_long_entry and self.Position <= 0:
                    if self.Position < 0:
                        self.BuyMarket()
                    self.BuyMarket()
            else:
                if self.allow_short_entry and self.Position >= 0:
                    if self.Position > 0:
                        self.SellMarket()
                    self.SellMarket()
        else:
            if is_positive:
                if self.allow_short_exit and self.Position < 0:
                    self.BuyMarket()
            else:
                if self.allow_long_exit and self.Position > 0:
                    self.SellMarket()

        self._was_positive = is_positive
        self._has_prev = True

    def CreateClone(self):
        return super_woodies_cci_strategy()
