import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage, WeightedMovingAverage, HullMovingAverage, SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy

MA_SMA = 0
MA_EMA = 1
MA_WMA = 2
MA_HMA = 3
MA_SMMA = 4

class flexible_moving_average_strategy(Strategy):
    """
    Flexible Moving Average strategy.
    A long-only position is sized against a moving average of the chosen method. With AllowInitialBuy the full position is bought
    on the first tradable candle. When a candle closes above the average after closing at or below it, the position is restored
    to the full Volume; when it closes below after closing at or above, the position is reduced by SellPercentage percent.
    """

    def __init__(self):
        super(flexible_moving_average_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 200).SetGreaterThanZero().SetDisplay("MA Length", "Moving average length", "Indicators")
        self._sell_percentage = self.Param("SellPercentage", 100.0).SetDisplay("Sell %", "Percent of the position sold on a cross below the average", "Trading")
        self._ma_method = self.Param("MaMethod", MA_SMA).SetDisplay("MA Method", "Moving average method (0 SMA, 1 EMA, 2 WMA, 3 HMA, 4 SMMA)", "Indicators")
        self._allow_initial_buy = self.Param("AllowInitialBuy", True).SetDisplay("Initial Buy", "Buy the full position on the first candle", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_ma = None
        self._initial_done = False

    def OnReseted(self):
        super(flexible_moving_average_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(flexible_moving_average_strategy, self).OnStarted2(time)

        self._reset_state()

        ma = self._create_ma(int(self._ma_method.Value), self._ma_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _create_ma(self, method, length):
        if method == MA_EMA:
            ma = ExponentialMovingAverage()
        elif method == MA_WMA:
            ma = WeightedMovingAverage()
        elif method == MA_HMA:
            ma = HullMovingAverage()
        elif method == MA_SMMA:
            ma = SmoothedMovingAverage()
        else:
            ma = SimpleMovingAverage()
        ma.Length = length
        return ma

    def _process_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        ma = float(ma_value)
        close = float(candle.ClosePrice)

        last_close = self._prev_close
        last_ma = self._prev_ma
        self._prev_close = close
        self._prev_ma = ma

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if not self._initial_done:
            self._initial_done = True
            if self._allow_initial_buy.Value and self.Position < self.Volume:
                self.BuyMarket(self.Volume - self.Position)
                return

        if last_close is None or last_ma is None:
            return

        if last_close <= last_ma and close > ma:
            if self.Position < self.Volume:
                self.BuyMarket(self.Volume - self.Position)
        elif last_close >= last_ma and close < ma and self.Position > 0:
            percent = float(self._sell_percentage.Value)
            if percent >= 100.0:
                volume = float(self.Position)
            else:
                volume = self._round_volume(float(self.Position) * percent / 100.0)
            if volume > 0:
                self.SellMarket(Decimal(volume))

    def _round_volume(self, volume):
        step = float(self.Security.VolumeStep) if self.Security is not None and self.Security.VolumeStep is not None else 0.0
        return math.floor(volume / step) * step if step > 0 else volume

    def CreateClone(self):
        return flexible_moving_average_strategy()
