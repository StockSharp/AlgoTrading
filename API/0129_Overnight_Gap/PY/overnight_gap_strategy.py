import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class overnight_gap_strategy(Strategy):
    """
    Overnight Gap trading strategy.
    Trades on gaps between current open and previous close, using MA trend filter.
    """

    def __init__(self):
        super(overnight_gap_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "Moving average period", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")
        self._cooldown_bars = self.Param("CooldownBars", 30).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._prev_close = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(overnight_gap_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(overnight_gap_strategy, self).OnStarted(time)

        self._prev_close = 0.0
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
        open_price = float(candle.OpenPrice)
        ma = float(ma_val)
        cd = self._cooldown_bars.Value

        if self._prev_close == 0:
            self._prev_close = close
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_close = close
            return

        # Calculate gap
        gap = open_price - self._prev_close

        # Gap up + above MA = Buy
        if gap > 0 and open_price > ma and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        # Gap down + below MA = Sell short
        elif gap < 0 and open_price < ma and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd

        # Exit: MA cross
        if self.Position > 0 and close < ma:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > ma:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_close = close

    def CreateClone(self):
        return overnight_gap_strategy()
