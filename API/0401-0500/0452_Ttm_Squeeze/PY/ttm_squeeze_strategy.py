import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, SimpleMovingAverage, Highest, Lowest, LinearReg
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

BOLLINGER_WIDTH = 2.0
KELTNER_MULTIPLIER = 1.5


class ttm_squeeze_strategy(Strategy):
    """
    TTM Squeeze Strategy.
    Bollinger Bands (2 deviations) and Keltner Channels (1.5 average true ranges) span SqueezeLength bars. When the squeeze is
    off (the bands lie outside the channels) a long opens if the linear regression momentum is below zero and rising with RSI
    above 30, and a short opens if momentum is above zero and falling with RSI below 70. An opposite signal reverses the
    position; UseTP enables a TpPercent take profit.
    """

    def __init__(self):
        super(ttm_squeeze_strategy, self).__init__()
        self._squeeze_length = self.Param("SqueezeLength", 20).SetGreaterThanZero().SetDisplay("Squeeze Length", "Length of the bands, the channels and the momentum", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "Indicators")
        self._use_tp = self.Param("UseTP", False).SetDisplay("Use TP", "Enable the take profit", "Risk")
        self._tp_percent = self.Param("TpPercent", 1.2).SetGreaterThanZero().SetDisplay("TP %", "Take profit percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._close_sma = None
        self._range_sma = None
        self._highest = None
        self._lowest = None
        self._momentum = None
        self._prev_close = None
        self._prev_momentum = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ttm_squeeze_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_momentum = None

    def OnStarted2(self, time):
        super(ttm_squeeze_strategy, self).OnStarted2(time)

        self._prev_close = None
        self._prev_momentum = None

        length = self._squeeze_length.Value
        bollinger = BollingerBands()
        bollinger.Length = length
        bollinger.Width = Decimal(BOLLINGER_WIDTH)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        self._close_sma = SimpleMovingAverage()
        self._close_sma.Length = length
        self._range_sma = SimpleMovingAverage()
        self._range_sma.Length = length
        self._highest = Highest()
        self._highest.Length = length
        self._lowest = Lowest()
        self._lowest.Length = length
        self._momentum = LinearReg()
        self._momentum.Length = length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, rsi, self._process_candle).Start()

        if self._use_tp.Value:
            self.StartProtection(Unit(Decimal(float(self._tp_percent.Value)), UnitTypes.Percent), Unit(), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, bollinger_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        time = candle.OpenTime
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self._prev_close is not None:
            pc = self._prev_close
            true_range = max(high - low, abs(high - pc), abs(low - pc))
        else:
            true_range = high - low
        self._prev_close = close

        sma = float(process_float(self._close_sma, candle.ClosePrice, time, True).GetValue[Decimal](None))
        range_average = float(process_float(self._range_sma, true_range, time, True).GetValue[Decimal](None))
        highest = float(process_float(self._highest, candle.HighPrice, time, True).GetValue[Decimal](None))
        lowest = float(process_float(self._lowest, candle.LowPrice, time, True).GetValue[Decimal](None))

        if not self._close_sma.IsFormed or not self._range_sma.IsFormed or not self._highest.IsFormed or not self._lowest.IsFormed:
            return

        # Momentum is the linear regression of the close's distance from the mean of the Donchian midline and the SMA.
        basis = ((highest + lowest) / 2.0 + sma) / 2.0
        momentum_value = process_float(self._momentum, close - basis, time, True)
        if not self._momentum.IsFormed:
            return

        momentum = float(momentum_value.GetValue[Decimal](None))
        previous = self._prev_momentum
        self._prev_momentum = momentum

        if previous is None:
            return

        if not bollinger_value.IsFormed or not rsi_value.IsFormed:
            return

        if bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        bb_upper = float(bollinger_value.UpBand)
        bb_lower = float(bollinger_value.LowBand)
        kc_upper = sma + KELTNER_MULTIPLIER * range_average
        kc_lower = sma - KELTNER_MULTIPLIER * range_average
        squeeze_off = bb_upper > kc_upper and bb_lower < kc_lower

        if not squeeze_off:
            return

        rsi = float(rsi_value.GetValue[Decimal](None))

        if momentum < 0 and momentum > previous and rsi > 30 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif momentum > 0 and momentum < previous and rsi < 70 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return ttm_squeeze_strategy()
