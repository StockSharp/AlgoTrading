import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class bitcoin_bullish_percent_index_strategy(Strategy):
    """
    Bitcoin Bullish Percent Index strategy.
    RSI approximates the bullish percent index: RSI crossing above Oversold goes long, RSI crossing below Overbought goes short,
    and the opposite signal reverses the position.
    """

    def __init__(self):
        super(bitcoin_bullish_percent_index_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "RSI period", "Indicators")
        self._overbought = self.Param("Overbought", 70.0).SetDisplay("Overbought", "RSI level whose downward cross opens a short", "Indicators")
        self._oversold = self.Param("Oversold", 30.0).SetDisplay("Oversold", "RSI level whose upward cross opens a long", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_rsi = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bitcoin_bullish_percent_index_strategy, self).OnReseted()
        self._prev_rsi = None

    def OnStarted2(self, time):
        super(bitcoin_bullish_percent_index_strategy, self).OnStarted2(time)

        self._prev_rsi = None

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, rsi):
        if candle.State != CandleStates.Finished:
            return

        prev = self._prev_rsi
        self._prev_rsi = rsi

        if prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        oversold = Decimal(self._oversold.Value)
        overbought = Decimal(self._overbought.Value)

        if prev <= oversold and rsi > oversold and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif prev >= overbought and rsi < overbought and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return bitcoin_bullish_percent_index_strategy()
