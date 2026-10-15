import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import TripleExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class profit_labels_strategy(Strategy):
    def __init__(self):
        super(profit_labels_strategy, self).__init__()
        self._tema_period = self.Param("TemaPeriod", 6)
        self._trade_volume = self.Param("TradeVolume", 1.0)
        self._placing_trade = self.Param("PlacingTrade", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._last_signal_time = None
        self._prev_trade_buy = False
        self._prev_trade_sell = False
        self._tema0 = None
        self._tema1 = None
        self._tema2 = None
        self._tema3 = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @TradeVolume.setter
    def TradeVolume(self, value):
        self._trade_volume.Value = value

    @property
    def PlacingTrade(self):
        return self._placing_trade.Value

    @PlacingTrade.setter
    def PlacingTrade(self, value):
        self._placing_trade.Value = value

    def OnReseted(self):
        super(profit_labels_strategy, self).OnReseted()
        self._last_signal_time = None
        self._prev_trade_buy = False
        self._prev_trade_sell = False
        self._tema0 = None
        self._tema1 = None
        self._tema2 = None
        self._tema3 = None

    def OnStarted2(self, time):
        super(profit_labels_strategy, self).OnStarted2(time)

        self.Volume = float(self.TradeVolume)

        tema = TripleExponentialMovingAverage()
        tema.Length = self._tema_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(tema, self._process_candle).Start()

    def _process_candle(self, candle, tema_val):
        if candle.State != CandleStates.Finished:
            return

        self._tema3 = self._tema2
        self._tema2 = self._tema1
        self._tema1 = self._tema0
        self._tema0 = float(tema_val)

        if self._tema3 is None or self._tema2 is None or self._tema1 is None or self._tema0 is None:
            return

        trend_up = self._tema2 < self._tema3 and self._tema0 > self._tema1
        trend_down = self._tema2 > self._tema3 and self._tema0 < self._tema1

        if not self.PlacingTrade:
            return

        if trend_up:
            self._handle_long_signal(candle)
        elif trend_down:
            self._handle_short_signal(candle)

    def _handle_long_signal(self, candle):
        if self._last_signal_time == candle.OpenTime:
            return
        self._last_signal_time = candle.OpenTime

        if self.Position < 0:
            self.BuyMarket(abs(float(self.Position)))
            self._prev_trade_buy = False
            self._prev_trade_sell = False
            return

        if self.Position != 0 or self._prev_trade_buy:
            return

        self.BuyMarket(float(self.TradeVolume))
        self._prev_trade_buy = True
        self._prev_trade_sell = False

    def _handle_short_signal(self, candle):
        if self._last_signal_time == candle.OpenTime:
            return
        self._last_signal_time = candle.OpenTime

        if self.Position > 0:
            self.SellMarket(float(self.Position))
            self._prev_trade_buy = False
            self._prev_trade_sell = False
            return

        if self.Position != 0 or self._prev_trade_sell:
            return

        self.SellMarket(float(self.TradeVolume))
        self._prev_trade_buy = False
        self._prev_trade_sell = True

    def CreateClone(self):
        return profit_labels_strategy()
