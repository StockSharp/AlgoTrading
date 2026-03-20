import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class doji_reversal_strategy(Strategy):
    """
    Doji Reversal strategy.
    Looks for doji candlestick patterns after a trend and takes a reversal position.
    Doji after downtrend = buy, doji after uptrend = sell.
    Uses SMA for exit signals.
    """

    def __init__(self):
        super(doji_reversal_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._doji_threshold = self.Param("DojiThreshold", 0.1).SetDisplay("Doji Threshold", "Max body/range ratio for doji", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._bar1 = None
        self._bar2 = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(doji_reversal_strategy, self).OnReseted()
        self._bar1 = None
        self._bar2 = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(doji_reversal_strategy, self).OnStarted(time)

        self._bar1 = None
        self._bar2 = None
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

    def _is_doji(self, candle):
        body_size = abs(float(candle.OpenPrice) - float(candle.ClosePrice))
        total_range = float(candle.HighPrice) - float(candle.LowPrice)
        if total_range == 0:
            return False
        return body_size / total_range < float(self._doji_threshold.Value)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._bar1 = self._bar2
            self._bar2 = candle
            return

        if self._bar1 is not None and self._bar2 is not None:
            is_doji = self._is_doji(candle)

            if is_doji:
                is_downtrend = self._bar2.ClosePrice < self._bar1.ClosePrice
                is_uptrend = self._bar2.ClosePrice > self._bar1.ClosePrice
                cd = self._cooldown_bars.Value

                if self.Position == 0 and is_downtrend:
                    self.BuyMarket()
                    self._cooldown = cd
                elif self.Position == 0 and is_uptrend:
                    self.SellMarket()
                    self._cooldown = cd

            # Exit on SMA cross
            sv = float(sma_val)
            close = float(candle.ClosePrice)
            cd = self._cooldown_bars.Value

            if self.Position > 0 and close < sv:
                self.SellMarket()
                self._cooldown = cd
            elif self.Position < 0 and close > sv:
                self.BuyMarket()
                self._cooldown = cd

        self._bar1 = self._bar2
        self._bar2 = candle

    def CreateClone(self):
        return doji_reversal_strategy()
