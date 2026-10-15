import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class candle_trend_strategy(Strategy):
    def __init__(self):
        super(candle_trend_strategy, self).__init__()
        self._trend_candles = self.Param("TrendCandles", 3) \
            .SetDisplay("Trend Candles", "Number of candles in one direction", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for analysis", "General")
        self._take_profit_percent = self.Param("TakeProfitPercent", 0.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
        self._stop_loss_percent = self.Param("StopLossPercent", 0.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        self._enable_long_entry = self.Param("EnableLongEntry", True) \
            .SetDisplay("Enable Long Entry", "Permission to enter long", "General")
        self._enable_short_entry = self.Param("EnableShortEntry", True) \
            .SetDisplay("Enable Short Entry", "Permission to enter short", "General")
        self._enable_long_exit = self.Param("EnableLongExit", True) \
            .SetDisplay("Enable Long Exit", "Permission to exit long", "General")
        self._enable_short_exit = self.Param("EnableShortExit", True) \
            .SetDisplay("Enable Short Exit", "Permission to exit short", "General")
        self._up_count = 0
        self._down_count = 0

    @property
    def trend_candles(self):
        return self._trend_candles.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def take_profit_percent(self):
        return self._take_profit_percent.Value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @property
    def enable_long_entry(self):
        return self._enable_long_entry.Value

    @property
    def enable_short_entry(self):
        return self._enable_short_entry.Value

    @property
    def enable_long_exit(self):
        return self._enable_long_exit.Value

    @property
    def enable_short_exit(self):
        return self._enable_short_exit.Value

    def OnReseted(self):
        super(candle_trend_strategy, self).OnReseted()
        self._up_count = 0
        self._down_count = 0

    def OnStarted2(self, time):
        super(candle_trend_strategy, self).OnStarted2(time)
        tp_val = float(self.take_profit_percent)
        sl_val = float(self.stop_loss_percent)
        tp = Unit(tp_val, UnitTypes.Percent) if tp_val > 0 else None
        sl = Unit(sl_val, UnitTypes.Percent) if sl_val > 0 else None
        self.StartProtection(tp, sl)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        is_bull = close > open_p
        is_bear = close < open_p
        if is_bull:
            self._up_count += 1
            self._down_count = 0
        elif is_bear:
            self._down_count += 1
            self._up_count = 0
        else:
            self._up_count = 0
            self._down_count = 0
        tc = int(self.trend_candles)
        if self._up_count >= tc:
            if self.Position < 0 and self.enable_long_exit:
                self.BuyMarket()
            if self.Position <= 0 and self.enable_long_entry:
                self.BuyMarket()
        elif self._down_count >= tc:
            if self.Position > 0 and self.enable_short_exit:
                self.SellMarket()
            if self.Position >= 0 and self.enable_short_entry:
                self.SellMarket()

    def CreateClone(self):
        return candle_trend_strategy()
