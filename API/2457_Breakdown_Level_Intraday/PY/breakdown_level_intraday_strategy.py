import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class breakdown_level_intraday_strategy(Strategy):
    def __init__(self):
        super(breakdown_level_intraday_strategy, self).__init__()

        self._lookback = self.Param("Lookback", 60)
        self._stop_loss_pct = self.Param("StopLossPct", 0.5)
        self._take_profit_pct = self.Param("TakeProfitPct", 1.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    @property
    def Lookback(self):
        return self._lookback.Value

    @Lookback.setter
    def Lookback(self, value):
        self._lookback.Value = value

    @property
    def StopLossPct(self):
        return self._stop_loss_pct.Value

    @StopLossPct.setter
    def StopLossPct(self, value):
        self._stop_loss_pct.Value = value

    @property
    def TakeProfitPct(self):
        return self._take_profit_pct.Value

    @TakeProfitPct.setter
    def TakeProfitPct(self, value):
        self._take_profit_pct.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(breakdown_level_intraday_strategy, self).OnStarted2(time)

        self._has_prev = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0

        highest = Highest()
        highest.Length = self.Lookback
        lowest = Lowest()
        lowest.Length = self.Lookback

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, high_val, low_val):
        if candle.State != CandleStates.Finished:
            return

        high = float(high_val)
        low = float(low_val)
        close = float(candle.ClosePrice)

        sl_pct = float(self.StopLossPct)
        tp_pct = float(self.TakeProfitPct)

        if self._has_prev:
            if self.Position == 0:
                if close > self._prev_high * 1.002:
                    self.BuyMarket()
                    self._entry_price = close
                    self._stop_price = close * (1.0 - sl_pct / 100.0)
                    self._take_price = close * (1.0 + tp_pct / 100.0)
                elif close < self._prev_low * 0.998:
                    self.SellMarket()
                    self._entry_price = close
                    self._stop_price = close * (1.0 + sl_pct / 100.0)
                    self._take_price = close * (1.0 - tp_pct / 100.0)
            elif self.Position > 0:
                if close >= self._take_price or close <= self._stop_price:
                    self.SellMarket()
            elif self.Position < 0:
                if close <= self._take_price or close >= self._stop_price:
                    self.BuyMarket()

        self._prev_high = high
        self._prev_low = low
        self._has_prev = True

    def OnReseted(self):
        super(breakdown_level_intraday_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    def CreateClone(self):
        return breakdown_level_intraday_strategy()
