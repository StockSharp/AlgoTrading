import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Indicators import SmoothedMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType
from System import TimeSpan


class improve_ma_rsi_hedge_strategy(Strategy):
    def __init__(self):
        super(improve_ma_rsi_hedge_strategy, self).__init__()

        self._profit_target = self.Param("ProfitTarget", 50.0)
        self._fast_period = self.Param("FastMaPeriod", 8)
        self._slow_period = self.Param("SlowMaPeriod", 21)
        self._rsi_period = self.Param("RsiPeriod", 21)
        self._oversold_level = self.Param("OversoldLevel", 30.0)
        self._overbought_level = self.Param("OverboughtLevel", 70.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._fast_ma = None
        self._slow_ma = None
        self._rsi = None
        self._base_last_close = 0.0
        self._base_entry_price = 0.0
        self._pair_direction = 0

    @property
    def ProfitTarget(self):
        return self._profit_target.Value

    @property
    def FastMaPeriod(self):
        return self._fast_period.Value

    @property
    def SlowMaPeriod(self):
        return self._slow_period.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def OversoldLevel(self):
        return self._oversold_level.Value

    @property
    def OverboughtLevel(self):
        return self._overbought_level.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(improve_ma_rsi_hedge_strategy, self).OnStarted(time)

        self._fast_ma = SmoothedMovingAverage()
        self._fast_ma.Length = self.FastMaPeriod
        self._slow_ma = SmoothedMovingAverage()
        self._slow_ma.Length = self.SlowMaPeriod
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._fast_ma, self._slow_ma, self._rsi, self._process_candle).Start()

    def _process_candle(self, candle, fast_val, slow_val, rsi_val):
        fast_value = float(fast_val)
        slow_value = float(slow_val)
        rsi_value = float(rsi_val)

        self._base_last_close = float(candle.ClosePrice)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if not self._fast_ma.IsFormed or not self._slow_ma.IsFormed or not self._rsi.IsFormed:
            return

        if self._pair_direction != 0:
            if self._pair_direction > 0:
                pnl = (self._base_last_close - self._base_entry_price) * float(self.Volume)
            else:
                pnl = (self._base_entry_price - self._base_last_close) * float(self.Volume)
            if pnl >= self.ProfitTarget:
                if self.Position > 0:
                    self.SellMarket(self.Position)
                elif self.Position < 0:
                    self.BuyMarket(abs(self.Position))
                self._pair_direction = 0
                self._base_entry_price = 0.0
            return

        if slow_value > fast_value and rsi_value <= self.OversoldLevel:
            self.BuyMarket()
            self._pair_direction = 1
            self._base_entry_price = self._base_last_close
        elif slow_value < fast_value and rsi_value >= self.OverboughtLevel:
            self.SellMarket()
            self._pair_direction = -1
            self._base_entry_price = self._base_last_close

    def OnReseted(self):
        super(improve_ma_rsi_hedge_strategy, self).OnReseted()
        self._fast_ma = None
        self._slow_ma = None
        self._rsi = None
        self._base_last_close = 0.0
        self._base_entry_price = 0.0
        self._pair_direction = 0

    def CreateClone(self):
        return improve_ma_rsi_hedge_strategy()
