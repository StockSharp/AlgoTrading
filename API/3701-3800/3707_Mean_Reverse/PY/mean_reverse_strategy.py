import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class mean_reverse_strategy(Strategy):

    def __init__(self):
        super(mean_reverse_strategy, self).__init__()

        self._fast_ma_period = self.Param("FastMaPeriod", 20) \
            .SetDisplay("Fast MA Period", "Length of the fast simple moving average", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 50) \
            .SetDisplay("Slow MA Period", "Length of the slow simple moving average", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Number of candles used for ATR calculation", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier applied to ATR for deviation bands", "Indicators")
        self._stop_loss_points = self.Param("StopLossPoints", 500) \
            .SetDisplay("Stop Loss Points", "Stop-loss distance in security steps", "Risk Management")
        self._take_profit_points = self.Param("TakeProfitPoints", 1000) \
            .SetDisplay("Take Profit Points", "Take-profit distance in security steps", "Risk Management")
        self._trade_volume = self.Param("TradeVolume", 1.0) \
            .SetDisplay("Trade Volume", "Volume opened with a new signal", "Execution")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of market data processed by the strategy", "General")

        self._prev_fast_ma = None
        self._prev_slow_ma = None
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value

    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @property
    def AtrMultiplier(self):
        return self._atr_multiplier.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    def OnReseted(self):
        super(mean_reverse_strategy, self).OnReseted()
        self._prev_fast_ma = None
        self._prev_slow_ma = None
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0

    def OnStarted2(self, time):
        super(mean_reverse_strategy, self).OnStarted2(time)

        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self.FastMaPeriod
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = self.SlowMaPeriod
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ma, slow_ma, atr, self._process_candle).Start()

    def _process_candle(self, candle, fast_ma, slow_ma, atr_val):
        if candle.State != CandleStates.Finished:
            return

        fma = float(fast_ma)
        sma_v = float(slow_ma)
        atr_v = float(atr_val)

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            s = float(self.Security.PriceStep)
            if s > 0:
                step = s

        if self._prev_fast_ma is None or self._prev_slow_ma is None:
            self._prev_fast_ma = fma
            self._prev_slow_ma = sma_v
            return

        prev_fast = self._prev_fast_ma
        prev_slow = self._prev_slow_ma

        trend_buy = prev_fast <= prev_slow and fma > sma_v
        trend_sell = prev_fast >= prev_slow and fma < sma_v

        deviation = atr_v * float(self.AtrMultiplier)
        reversion_buy = float(candle.ClosePrice) < sma_v - deviation
        reversion_sell = float(candle.ClosePrice) > sma_v + deviation

        buy_signal = trend_buy or reversion_buy
        sell_signal = trend_sell or reversion_sell

        if self.Position == 0:
            if buy_signal:
                self._enter_long(float(candle.ClosePrice), step)
            elif sell_signal:
                self._enter_short(float(candle.ClosePrice), step)
        elif self.Position > 0:
            self._manage_long(candle)
        else:
            self._manage_short(candle)

        self._prev_fast_ma = fma
        self._prev_slow_ma = sma_v

    def _enter_long(self, entry_price, step):
        self.BuyMarket(self.TradeVolume)
        self._stop_loss_price = entry_price - self.StopLossPoints * step
        self._take_profit_price = entry_price + self.TakeProfitPoints * step

    def _enter_short(self, entry_price, step):
        self.SellMarket(self.TradeVolume)
        self._stop_loss_price = entry_price + self.StopLossPoints * step
        self._take_profit_price = entry_price - self.TakeProfitPoints * step

    def _manage_long(self, candle):
        if float(candle.LowPrice) <= self._stop_loss_price:
            self.SellMarket(self.Position)
            self._clear_risk_levels()
            return
        if self._take_profit_price != 0 and float(candle.HighPrice) >= self._take_profit_price:
            self.SellMarket(self.Position)
            self._clear_risk_levels()

    def _manage_short(self, candle):
        if float(candle.HighPrice) >= self._stop_loss_price:
            self.BuyMarket(abs(self.Position))
            self._clear_risk_levels()
            return
        if self._take_profit_price != 0 and float(candle.LowPrice) <= self._take_profit_price:
            self.BuyMarket(abs(self.Position))
            self._clear_risk_levels()

    def _clear_risk_levels(self):
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0

    def CreateClone(self):
        return mean_reverse_strategy()
