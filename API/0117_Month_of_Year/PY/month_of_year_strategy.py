import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class month_of_year_strategy(Strategy):
    """
    Month of Year seasonal trading strategy.
    Uses first/second half of month with MA trend filter and cooldown.
    """

    def __init__(self):
        super(month_of_year_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "SMA period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 100).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._prev_ma = 0.0
        self._prev_close = 0.0
        self._last_trade_month = 0
        self._last_trade_half = 0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(month_of_year_strategy, self).OnReseted()
        self._prev_ma = 0.0
        self._prev_close = 0.0
        self._last_trade_month = 0
        self._last_trade_half = 0
        self._cooldown = 0

    def OnStarted(self, time):
        super(month_of_year_strategy, self).OnStarted(time)

        self._prev_ma = 0.0
        self._prev_close = 0.0
        self._last_trade_month = 0
        self._last_trade_half = 0
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        ma = float(ma_val)
        month = candle.OpenTime.Month
        half = 1 if candle.OpenTime.Day <= 15 else 2
        cd = self._cooldown_bars.Value

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_ma = ma
            self._prev_close = close
            return

        # Exit logic: MA cross
        if self.Position > 0 and close < ma and self._prev_ma > 0 and self._prev_close >= self._prev_ma:
            self.SellMarket()
            self._cooldown = cd
            self._last_trade_month = month
            self._last_trade_half = half
        elif self.Position < 0 and close > ma and self._prev_ma > 0 and self._prev_close <= self._prev_ma:
            self.BuyMarket()
            self._cooldown = cd
            self._last_trade_month = month
            self._last_trade_half = half

        # Entry logic: seasonal month-half based
        if self.Position == 0 and (month != self._last_trade_month or half != self._last_trade_half):
            # First half of month: buy if above MA
            if half == 1 and close > ma:
                self.BuyMarket()
                self._cooldown = cd
                self._last_trade_month = month
                self._last_trade_half = half
            # Second half of month: sell if below MA
            elif half == 2 and close < ma:
                self.SellMarket()
                self._cooldown = cd
                self._last_trade_month = month
                self._last_trade_half = half

        self._prev_ma = ma
        self._prev_close = close

    def CreateClone(self):
        return month_of_year_strategy()
