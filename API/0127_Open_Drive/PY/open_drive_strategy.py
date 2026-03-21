import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class open_drive_strategy(Strategy):
    """
    Open Drive trading strategy.
    Trades on strong momentum candles (body exceeds ATR * multiplier).
    Bullish momentum + above MA = Buy, Bearish momentum + below MA = Sell.
    """

    def __init__(self):
        super(open_drive_strategy, self).__init__()
        self._atr_multiplier = self.Param("AtrMultiplier", 0.3).SetDisplay("ATR Multiplier", "Multiplier for ATR to define gap size", "Strategy")
        self._atr_period = self.Param("AtrPeriod", 14).SetDisplay("ATR Period", "Period for ATR calculation", "Strategy")
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")
        self._cooldown_bars = self.Param("CooldownBars", 30).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._prev_close_price = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(open_drive_strategy, self).OnReseted()
        self._prev_close_price = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(open_drive_strategy, self).OnStarted(time)

        self._prev_close_price = 0.0
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        sma = float(sma_val)
        atr = float(atr_val)
        cd = self._cooldown_bars.Value
        mult = float(self._atr_multiplier.Value)

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_close_price = close
            return

        # Detect strong momentum candle (body exceeds ATR * multiplier)
        if self._prev_close_price > 0 and atr > 0:
            body = close - open_price
            body_size = abs(body)

            if body_size > atr * mult and self.Position == 0:
                # Bullish momentum + above MA = Buy
                if body > 0 and close > sma:
                    self.BuyMarket()
                    self._cooldown = cd
                # Bearish momentum + below MA = Sell short
                elif body < 0 and close < sma:
                    self.SellMarket()
                    self._cooldown = cd

        self._prev_close_price = close

    def CreateClone(self):
        return open_drive_strategy()
