import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Math, TimeSpan, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class euro_surge_simplified_strategy(Strategy):
    def __init__(self):
        super(euro_surge_simplified_strategy, self).__init__()

        self._fixed_volume = self.Param("FixedVolume", 1.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._ma_period = self.Param("MaPeriod", 52)
        self._rsi_period = self.Param("RsiPeriod", 13)
        self._rsi_buy_level = self.Param("RsiBuyLevel", 50.0)
        self._rsi_sell_level = self.Param("RsiSellLevel", 50.0)
        self._use_ma = self.Param("UseMa", True)
        self._use_rsi = self.Param("UseRsi", True)
        self._min_trade_interval_minutes = self.Param("MinTradeIntervalMinutes", 600)

        self._last_trade_time = DateTimeOffset.MinValue
        self._fast_ma = None
        self._slow_ma = None
        self._rsi = None
        self._fast_ma_value = 0.0
        self._slow_ma_value = 0.0
        self._rsi_value = 0.0

    @property
    def FixedVolume(self):
        return self._fixed_volume.Value

    @FixedVolume.setter
    def FixedVolume(self, value):
        self._fixed_volume.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiBuyLevel(self):
        return self._rsi_buy_level.Value

    @RsiBuyLevel.setter
    def RsiBuyLevel(self, value):
        self._rsi_buy_level.Value = value

    @property
    def RsiSellLevel(self):
        return self._rsi_sell_level.Value

    @RsiSellLevel.setter
    def RsiSellLevel(self, value):
        self._rsi_sell_level.Value = value

    @property
    def UseMa(self):
        return self._use_ma.Value

    @UseMa.setter
    def UseMa(self, value):
        self._use_ma.Value = value

    @property
    def UseRsi(self):
        return self._use_rsi.Value

    @UseRsi.setter
    def UseRsi(self, value):
        self._use_rsi.Value = value

    @property
    def MinTradeIntervalMinutes(self):
        return self._min_trade_interval_minutes.Value

    @MinTradeIntervalMinutes.setter
    def MinTradeIntervalMinutes(self, value):
        self._min_trade_interval_minutes.Value = value

    def OnReseted(self):
        super(euro_surge_simplified_strategy, self).OnReseted()
        self._last_trade_time = DateTimeOffset.MinValue
        self._fast_ma_value = 0.0
        self._slow_ma_value = 0.0
        self._rsi_value = 0.0

    def OnStarted2(self, time):
        super(euro_surge_simplified_strategy, self).OnStarted2(time)

        self._fast_ma = SimpleMovingAverage()
        self._fast_ma.Length = 20
        self._slow_ma = SimpleMovingAverage()
        self._slow_ma.Length = self.MaPeriod
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast_ma, self._slow_ma, self._rsi, self._process_candle).Start()

        self.StartProtection(None, None)

    def _process_candle(self, candle, fast_value, slow_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        self._fast_ma_value = float(fast_value)
        self._slow_ma_value = float(slow_value)
        self._rsi_value = float(rsi_value)

        # Check signals
        if self.UseMa and (not self._fast_ma.IsFormed or not self._slow_ma.IsFormed):
            return

        if self.UseRsi and not self._rsi.IsFormed:
            return

        fast_val = self._fast_ma_value
        slow_val = self._slow_ma_value
        rsi_val = self._rsi_value

        ma_buy = (not self.UseMa) or fast_val > slow_val
        ma_sell = (not self.UseMa) or fast_val < slow_val
        rsi_buy = (not self.UseRsi) or rsi_val <= float(self.RsiBuyLevel)
        rsi_sell = (not self.UseRsi) or rsi_val >= float(self.RsiSellLevel)

        is_buy = ma_buy and rsi_buy
        is_sell = ma_sell and rsi_sell

        if not is_buy and not is_sell:
            return

        now = candle.CloseTime
        min_interval = TimeSpan.FromMinutes(float(self.MinTradeIntervalMinutes))
        if self._last_trade_time != DateTimeOffset.MinValue and (now - self._last_trade_time) < min_interval:
            return

        volume = float(self.FixedVolume)
        if volume <= 0:
            return

        pos = float(self.Position)

        if is_buy and pos <= 0:
            order_volume = volume
            if pos < 0:
                order_volume += abs(pos)
            self.BuyMarket(order_volume)
            self._last_trade_time = now
        elif is_sell and pos >= 0:
            order_volume = volume
            if pos > 0:
                order_volume += abs(pos)
            self.SellMarket(order_volume)
            self._last_trade_time = now

    def CreateClone(self):
        return euro_surge_simplified_strategy()
