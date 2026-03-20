import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ma_crossover_demand_supply_zones_sltp_strategy(Strategy):
    def __init__(self):
        super(ma_crossover_demand_supply_zones_sltp_strategy, self).__init__()
        self._short_ma_length = self.Param("ShortMaLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Short MA", "Short MA period", "Indicators")
        self._long_ma_length = self.Param("LongMaLength", 21) \
            .SetGreaterThanZero() \
            .SetDisplay("Long MA", "Long MA period", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 7.0) \
            .SetGreaterThanZero() \
            .SetDisplay("SL %", "Stop loss percent", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 10.0) \
            .SetGreaterThanZero() \
            .SetDisplay("TP %", "Take profit percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(20))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._initialized = False
        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(ma_crossover_demand_supply_zones_sltp_strategy, self).OnReseted()
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._initialized = False
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(ma_crossover_demand_supply_zones_sltp_strategy, self).OnStarted(time)
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._initialized = False
        self._entry_price = 0.0
        self._cooldown = 0
        self._short_ma = ExponentialMovingAverage()
        self._short_ma.Length = self._short_ma_length.Value
        self._long_ma = ExponentialMovingAverage()
        self._long_ma.Length = self._long_ma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._short_ma, self._long_ma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._short_ma)
            self.DrawIndicator(area, self._long_ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, short_ma, long_ma):
        if candle.State != CandleStates.Finished:
            return
        if not self._short_ma.IsFormed or not self._long_ma.IsFormed:
            return
        sv = float(short_ma)
        lv = float(long_ma)
        if not self._initialized:
            self._prev_short = sv
            self._prev_long = lv
            self._initialized = True
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_short = sv
            self._prev_long = lv
            return
        close = float(candle.ClosePrice)
        cross_up = self._prev_short <= self._prev_long and sv > lv
        cross_down = self._prev_short >= self._prev_long and sv < lv
        if cross_up and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 10
        elif cross_down and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 10
        sl_pct = float(self._stop_loss_percent.Value) / 100.0
        tp_pct = float(self._take_profit_percent.Value) / 100.0
        if self.Position > 0 and self._entry_price > 0.0:
            sl = self._entry_price * (1.0 - sl_pct)
            tp = self._entry_price * (1.0 + tp_pct)
            if close <= sl or close >= tp:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 15
        elif self.Position < 0 and self._entry_price > 0.0:
            sl = self._entry_price * (1.0 + sl_pct)
            tp = self._entry_price * (1.0 - tp_pct)
            if close >= sl or close <= tp:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 15
        self._prev_short = sv
        self._prev_long = lv

    def CreateClone(self):
        return ma_crossover_demand_supply_zones_sltp_strategy()
