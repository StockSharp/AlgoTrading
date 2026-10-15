import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Array
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import IIndicator, SuperTrend, ExponentialMovingAverage, KaufmanAdaptiveMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class adaptive_fibonacci_pullback_strategy(Strategy):
    """
    Adaptive Fibonacci Pullback strategy.
    Three SuperTrend lines with Fibonacci multipliers are averaged and the average is smoothed by an EMA. A long needs the low to dip
    below the average while the close stays above the smoothed line, the previous and current close above the Kaufman AMA midline and
    RSI above RsiBuy; a short mirrors this with RSI below RsiSell. Positions close when the close crosses the smoothed line against them,
    and percent take profit and stop loss protect every trade.
    """

    def __init__(self):
        super(adaptive_fibonacci_pullback_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 8).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period of the SuperTrends", "SuperTrend")
        self._factor1 = self.Param("Factor1", 0.618).SetGreaterThanZero().SetDisplay("Factor 1", "Multiplier of the first SuperTrend", "SuperTrend")
        self._factor2 = self.Param("Factor2", 1.618).SetGreaterThanZero().SetDisplay("Factor 2", "Multiplier of the second SuperTrend", "SuperTrend")
        self._factor3 = self.Param("Factor3", 2.618).SetGreaterThanZero().SetDisplay("Factor 3", "Multiplier of the third SuperTrend", "SuperTrend")
        self._smooth_length = self.Param("SmoothLength", 21).SetGreaterThanZero().SetDisplay("Smooth Length", "EMA length that smooths the SuperTrend average", "SuperTrend")
        self._ama_length = self.Param("AmaLength", 55).SetGreaterThanZero().SetDisplay("AMA Length", "Period of the AMA midline", "AMA")
        self._rsi_length = self.Param("RsiLength", 7).SetGreaterThanZero().SetDisplay("RSI Length", "Period of RSI", "RSI")
        self._rsi_buy = self.Param("RsiBuy", 70.0).SetDisplay("RSI Buy", "RSI level a long requires to exceed", "RSI")
        self._rsi_sell = self.Param("RsiSell", 30.0).SetDisplay("RSI Sell", "RSI level a short requires to stay below", "RSI")
        self._take_profit_percent = self.Param("TakeProfitPercent", 5.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 0.75).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._smooth = None
        self._prev_close = None
        self._prev_smooth = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adaptive_fibonacci_pullback_strategy, self).OnReseted()
        self._smooth = None
        self._prev_close = None
        self._prev_smooth = None

    def OnStarted2(self, time):
        super(adaptive_fibonacci_pullback_strategy, self).OnStarted2(time)

        self._prev_close = None
        self._prev_smooth = None

        st1 = SuperTrend()
        st1.Length = self._atr_period.Value
        st1.Multiplier = Decimal(self._factor1.Value)
        st2 = SuperTrend()
        st2.Length = self._atr_period.Value
        st2.Multiplier = Decimal(self._factor2.Value)
        st3 = SuperTrend()
        st3.Length = self._atr_period.Value
        st3.Multiplier = Decimal(self._factor3.Value)
        ama = KaufmanAdaptiveMovingAverage()
        ama.Length = self._ama_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        self._smooth = ExponentialMovingAverage()
        self._smooth.Length = self._smooth_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(Array[IIndicator]([st1, st2, st3, ama, rsi]), self._process_candle).Start()

        self.StartProtection(Unit(Decimal(self._take_profit_percent.Value), UnitTypes.Percent), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ama)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, values):
        if candle.State != CandleStates.Finished:
            return

        for value in values:
            if not value.IsFormed:
                return

        average = (values[0].GetValue[Decimal](None) + values[1].GetValue[Decimal](None) + values[2].GetValue[Decimal](None)) / Decimal(3)
        mid = values[3].GetValue[Decimal](None)
        rsi = values[4].GetValue[Decimal](None)

        smooth_value = process_value(self._smooth, average, candle.ServerTime, True)
        if not self._smooth.IsFormed:
            return

        smooth = smooth_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        prev_close = self._prev_close
        prev_smooth = self._prev_smooth
        self._prev_close = close
        self._prev_smooth = smooth

        if prev_close is None or prev_smooth is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        long_entry = candle.LowPrice < average and close > smooth and prev_close > mid and close > mid and rsi > Decimal(self._rsi_buy.Value)
        short_entry = candle.HighPrice > average and close < smooth and prev_close < mid and close < mid and rsi < Decimal(self._rsi_sell.Value)

        if long_entry and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_entry and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and prev_close >= prev_smooth and close < smooth:
            self.SellMarket(self.Position)
        elif self.Position < 0 and prev_close <= prev_smooth and close > smooth:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return adaptive_fibonacci_pullback_strategy()
