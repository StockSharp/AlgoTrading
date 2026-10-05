import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class hull_ma_rsi_strategy(Strategy):
    """
    Hull MA RSI strategy.
    The Hull average turns up when it rises after falling and turns down when it falls after rising. A turn up with RSI below RsiOversold
    goes long and a turn down with RSI above RsiOverbought goes short, reversing an opposite position. A long closes when the Hull average
    falls and a short when it rises. The stop lies StopLossAtr ATR from the entry close and is checked on candle closes.
    """

    def __init__(self):
        super(hull_ma_rsi_strategy, self).__init__()
        self._hma_period = self.Param("HmaPeriod", 9).SetGreaterThanZero().SetDisplay("HMA Period", "Period of the Hull moving average", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "Period of RSI", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI level for longs", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI level for shorts", "Indicators")
        self._stop_loss_atr = self.Param("StopLossAtr", 2.0).SetNotNegative().SetDisplay("Stop Loss ATR", "Stop distance from the entry in ATRs", "Risk")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the stop ATR", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_hull = None
        self._prev_prev_hull = None
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(hull_ma_rsi_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(hull_ma_rsi_strategy, self).OnStarted2(time)

        self._reset_state()

        hull = HullMovingAverage()
        hull.Length = self._hma_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(hull, rsi, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hull)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, hull_value, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not hull_value.IsFormed or not rsi_value.IsFormed or not atr_value.IsFormed:
            return

        hull = hull_value.GetValue[Decimal](None)
        prev_hull = self._prev_hull
        prev_prev_hull = self._prev_prev_hull
        self._prev_prev_hull = prev_hull
        self._prev_hull = hull

        if prev_hull is None or prev_prev_hull is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi = rsi_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        rising = hull > prev_hull
        falling = hull < prev_hull
        turns_up = rising and prev_hull < prev_prev_hull
        turns_down = falling and prev_hull > prev_prev_hull

        stop_atr = Decimal(self._stop_loss_atr.Value)
        if turns_up and rsi < Decimal(self._rsi_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_atr * atr
        elif turns_down and rsi > Decimal(self._rsi_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_atr * atr
        elif self.Position > 0 and (falling or (stop_atr > 0 and close <= self._stop_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (rising or (stop_atr > 0 and close >= self._stop_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return hull_ma_rsi_strategy()
