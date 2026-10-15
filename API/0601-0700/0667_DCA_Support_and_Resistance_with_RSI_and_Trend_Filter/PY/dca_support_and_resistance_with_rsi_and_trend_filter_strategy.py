import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class dca_support_and_resistance_with_rsi_and_trend_filter_strategy(Strategy):
    """
    DCA support and resistance strategy with RSI and EMA trend filter.
    Support and resistance are the lowest low and highest high of the previous LookbackPeriod candles.
    When price touches support with RSI below Oversold and the close above the EMA, another long lot is bought;
    when price touches resistance with RSI above Overbought and the close below the EMA, another short lot is sold.
    A long closes at resistance or when RSI rises above Overbought; a short closes at support or when RSI falls below Oversold.
    """

    def __init__(self):
        super(dca_support_and_resistance_with_rsi_and_trend_filter_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 50).SetGreaterThanZero().SetDisplay("Lookback Period", "Candles that define support and resistance", "Levels")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "RSI")
        self._overbought = self.Param("Overbought", 70.0).SetDisplay("Overbought", "RSI overbought level", "RSI")
        self._oversold = self.Param("Oversold", 40.0).SetDisplay("Oversold", "RSI oversold level", "RSI")
        self._ema_period = self.Param("EmaPeriod", 200).SetGreaterThanZero().SetDisplay("EMA Period", "Trend EMA period", "Trend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_support = None
        self._prev_resistance = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(dca_support_and_resistance_with_rsi_and_trend_filter_strategy, self).OnReseted()
        self._prev_support = None
        self._prev_resistance = None

    def OnStarted2(self, time):
        super(dca_support_and_resistance_with_rsi_and_trend_filter_strategy, self).OnStarted2(time)

        self._prev_support = None
        self._prev_resistance = None

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        highest = Highest()
        highest.Length = self._lookback_period.Value
        lowest = Lowest()
        lowest.Length = self._lookback_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, rsi, highest, lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, ema_value, rsi_value, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        # Levels come from the candles before this one.
        support = self._prev_support
        resistance = self._prev_resistance

        if highest_value.IsFormed and lowest_value.IsFormed:
            self._prev_resistance = float(highest_value.GetValue[Decimal](None))
            self._prev_support = float(lowest_value.GetValue[Decimal](None))

        if not ema_value.IsFormed or not rsi_value.IsFormed or support is None or resistance is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ema = float(ema_value.GetValue[Decimal](None))
        rsi = float(rsi_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)
        overbought = float(self._overbought.Value)
        oversold = float(self._oversold.Value)

        at_support = float(candle.LowPrice) <= support
        at_resistance = float(candle.HighPrice) >= resistance

        if self.Position > 0 and (at_resistance or rsi > overbought):
            self.SellMarket(self.Position)
            return

        if self.Position < 0 and (at_support or rsi < oversold):
            self.BuyMarket(-self.Position)
            return

        if at_support and rsi < oversold and close > ema and self.Position >= 0:
            self.BuyMarket()
        elif at_resistance and rsi > overbought and close < ema and self.Position <= 0:
            self.SellMarket()

    def CreateClone(self):
        return dca_support_and_resistance_with_rsi_and_trend_filter_strategy()
