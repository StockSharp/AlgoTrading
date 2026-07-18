import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from indicator_extensions import *

class super_simple_rsi_engulfing_strategy(Strategy):
    """RSI filter combined with engulfing candle pattern for reversals."""

    def __init__(self):
        super(super_simple_rsi_engulfing_strategy, self).__init__()

        self._profit_goal = self.Param("ProfitGoal", 190.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Profit Goal", "Currency profit target to flatten", "Risk")
        self._max_loss = self.Param("MaxLoss", 10.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Loss", "Maximum currency drawdown before flattening", "Risk")
        self._rsi_period = self.Param("RsiPeriod", 7) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "RSI averaging period", "Indicators")
        # RsiPrice: 0=Open, 1=High, 2=Low, 3=Close, 4=Median, 5=Typical, 6=Weighted
        self._rsi_price = self.Param("RsiPrice", 1) \
            .SetDisplay("RSI Price", "Price source for RSI (0=O,1=H,2=L,3=C,4=Med,5=Typ,6=Wt)", "Indicators")
        self._overbought_level = self.Param("OverboughtLevel", 88.0) \
            .SetDisplay("Overbought Level", "RSI threshold for bullish reversals", "Indicators")
        self._oversold_level = self.Param("OversoldLevel", 37.0) \
            .SetDisplay("Oversold Level", "RSI threshold for bearish reversals", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle series to process", "General")

        self._prev_open = None
        self._prev_close = None
        self._prev_prev_open = None
        self._prev_prev_close = None

    @property
    def ProfitGoal(self):
        return float(self._profit_goal.Value)
    @property
    def MaxLoss(self):
        return float(self._max_loss.Value)
    @property
    def RsiPeriod(self):
        return int(self._rsi_period.Value)
    @property
    def RsiPrice(self):
        return int(self._rsi_price.Value)
    @property
    def OverboughtLevel(self):
        return float(self._overbought_level.Value)
    @property
    def OversoldLevel(self):
        return float(self._oversold_level.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(super_simple_rsi_engulfing_strategy, self).OnStarted2(time)

        self._prev_open = None
        self._prev_close = None
        self._prev_prev_open = None
        self._prev_prev_close = None

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def _get_price(self, candle):
        price_type = self.RsiPrice
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        o = float(candle.OpenPrice)

        if price_type == 0:
            return o
        elif price_type == 1:
            return h
        elif price_type == 2:
            return lo
        elif price_type == 3:
            return c
        elif price_type == 4:
            return (h + lo) / 2.0
        elif price_type == 5:
            return (h + lo + c) / 3.0
        elif price_type == 6:
            return (h + lo + 2.0 * c) / 4.0
        return c

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = self._get_price(candle)
        rsi_result = process_float(self._rsi, Decimal(price), candle.ServerTime, True)

        if not self._rsi.IsFormed:
            self._update_history(candle)
            return

        rsi_value = float(rsi_result.Value)

        if (self._prev_open is not None and self._prev_close is not None
                and self._prev_prev_open is not None and self._prev_prev_close is not None):

            prev_open = self._prev_open
            prev_close = self._prev_close
            prev_prev_open = self._prev_prev_open
            prev_prev_close = self._prev_prev_close

            bullish_engulfing = (prev_prev_open > prev_prev_close
                                 and prev_open < prev_close
                                 and prev_prev_open < prev_close)

            bearish_engulfing = (prev_prev_open < prev_prev_close
                                 and prev_open > prev_close
                                 and prev_prev_open > prev_close)

            long_signal = rsi_value > self.OverboughtLevel and bullish_engulfing and self.Position <= 0
            short_signal = rsi_value < self.OversoldLevel and bearish_engulfing and self.Position >= 0

            if long_signal:
                self.BuyMarket()
            elif short_signal:
                self.SellMarket()

        if self.Position != 0:
            total_pnl = float(self.PnL)
            if total_pnl >= self.ProfitGoal or total_pnl <= -self.MaxLoss:
                if self.Position > 0:
                    self.SellMarket()
                elif self.Position < 0:
                    self.BuyMarket()

        self._update_history(candle)

    def _update_history(self, candle):
        self._prev_prev_open = self._prev_open
        self._prev_prev_close = self._prev_close
        self._prev_open = float(candle.OpenPrice)
        self._prev_close = float(candle.ClosePrice)

    def OnReseted(self):
        super(super_simple_rsi_engulfing_strategy, self).OnReseted()
        self._prev_open = None
        self._prev_close = None
        self._prev_prev_open = None
        self._prev_prev_close = None

    def CreateClone(self):
        return super_simple_rsi_engulfing_strategy()
