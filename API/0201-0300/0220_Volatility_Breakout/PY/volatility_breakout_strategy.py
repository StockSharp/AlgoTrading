import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class volatility_breakout_strategy(Strategy):
    """
    Volatility Breakout strategy.
    A close more than Multiplier times the Period ATR above the Period simple moving average goes long and one that far below it goes short,
    reversing an opposite position. The position stays open until the opposite breakout, or until the stop Multiplier ATR from the entry
    close is hit, checked on candle closes.
    """

    def __init__(self):
        super(volatility_breakout_strategy, self).__init__()
        self._period = self.Param("Period", 20).SetGreaterThanZero().SetDisplay("Period", "Period for SMA and ATR", "Parameters")
        self._multiplier = self.Param("Multiplier", 2.0).SetGreaterThanZero().SetDisplay("Multiplier", "ATR multiplier for the breakout threshold and the stop", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(volatility_breakout_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(volatility_breakout_strategy, self).OnStarted2(time)

        self._reset_state()

        sma = SimpleMovingAverage()
        sma.Length = self._period.Value
        atr = AverageTrueRange()
        atr.Length = self._period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, atr)

    def _process_candle(self, candle, sma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not sma_value.IsFormed or not atr_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        sma = sma_value.GetValue[Decimal](None)
        distance = Decimal(self._multiplier.Value) * atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        if close > sma + distance and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - distance
        elif close < sma - distance and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + distance
        elif self.Position > 0 and close <= self._stop_price:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close >= self._stop_price:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return volatility_breakout_strategy()
