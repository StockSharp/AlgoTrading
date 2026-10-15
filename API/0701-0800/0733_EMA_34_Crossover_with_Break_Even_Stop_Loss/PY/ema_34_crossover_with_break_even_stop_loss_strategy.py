import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema_34_crossover_with_break_even_stop_loss_strategy(Strategy):
    """
    EMA 34 crossover with break even stop loss strategy.
    Long only: buys when the close crosses above the EMA from below. The stop is the previous candle's low, the take profit lies
    TakeProfitMultiplier times the risk above the entry, and the stop moves to the entry price once price has gone
    BreakEvenMultiplier times the risk in favour.
    """

    def __init__(self):
        super(ema_34_crossover_with_break_even_stop_loss_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 34).SetGreaterThanZero().SetDisplay("EMA Period", "EMA period", "Indicators")
        self._take_profit_multiplier = self.Param("TakeProfitMultiplier", 10.0).SetGreaterThanZero().SetDisplay("Take Profit Multiplier", "Take profit distance in multiples of the risk", "Risk")
        self._break_even_multiplier = self.Param("BreakEvenMultiplier", 3.0).SetGreaterThanZero().SetDisplay("Break Even Multiplier", "Favourable move in multiples of the risk that moves the stop to entry", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_ema = None
        self._prev_low = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._break_even_price = 0.0

    def OnReseted(self):
        super(ema_34_crossover_with_break_even_stop_loss_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ema_34_crossover_with_break_even_stop_loss_strategy, self).OnStarted2(time)

        self._reset_state()

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        ema = float(ema_value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        prev_close = self._prev_close
        prev_ema = self._prev_ema
        prev_low = self._prev_low
        self._prev_close = close
        self._prev_ema = ema
        self._prev_low = low

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if low <= self._stop_price or high >= self._take_price:
                self.SellMarket(self.Position)
                return

            if self._stop_price < self._entry_price and high >= self._break_even_price:
                self._stop_price = self._entry_price

            return

        if prev_close is None or prev_ema is None or prev_low is None:
            return

        if prev_close <= prev_ema and close > ema:
            risk = close - prev_low
            if risk <= 0:
                return

            self._entry_price = close
            self._stop_price = prev_low
            self._take_price = close + risk * float(self._take_profit_multiplier.Value)
            self._break_even_price = close + risk * float(self._break_even_multiplier.Value)

            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return ema_34_crossover_with_break_even_stop_loss_strategy()
