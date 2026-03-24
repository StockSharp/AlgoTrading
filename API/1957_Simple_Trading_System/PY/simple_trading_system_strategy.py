import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import (
    DecimalIndicatorValue, SimpleMovingAverage, ExponentialMovingAverage,
    DoubleExponentialMovingAverage, TripleExponentialMovingAverage,
    WeightedMovingAverage, VolumeWeightedMovingAverage
)
from StockSharp.Algo.Strategies import Strategy


# MA type constants
MA_SMA = 0
MA_EMA = 1
MA_DEMA = 2
MA_TEMA = 3
MA_WMA = 4
MA_VWMA = 5

# Price type constants
PRICE_CLOSE = 0
PRICE_HIGH = 1
PRICE_OPEN = 2
PRICE_LOW = 3
PRICE_TYPICAL = 4
PRICE_CENTER = 5


class simple_trading_system_strategy(Strategy):

    def __init__(self):
        super(simple_trading_system_strategy, self).__init__()

        self._ma_type = self.Param("MaType", MA_EMA) \
            .SetDisplay("MA Type", "Moving average type", "Parameters")
        self._ma_period = self.Param("MaPeriod", 4) \
            .SetDisplay("MA Period", "Moving average period", "Parameters")
        self._ma_shift = self.Param("MaShift", 4) \
            .SetDisplay("MA Shift", "Shift for comparisons", "Parameters")
        self._price_type = self.Param("PriceType", PRICE_CLOSE) \
            .SetDisplay("Price Type", "Source price for MA", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 1) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(6))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._buy_open = self.Param("BuyPositionOpen", True) \
            .SetDisplay("Buy Open", "Allow opening long positions", "Trading")
        self._sell_open = self.Param("SellPositionOpen", True) \
            .SetDisplay("Sell Open", "Allow opening short positions", "Trading")
        self._buy_close = self.Param("BuyPositionClose", True) \
            .SetDisplay("Buy Close", "Allow closing longs on sell signal", "Trading")
        self._sell_close = self.Param("SellPositionClose", True) \
            .SetDisplay("Sell Close", "Allow closing shorts on buy signal", "Trading")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Take profit in price units", "Risk")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Stop loss in price units", "Risk")

        self._ma = None
        self._ma_buffer = []
        self._close_buffer = []
        self._sign = 0
        self._bars_since_trade = 0

    @property
    def MaType(self):
        return self._ma_type.Value

    @MaType.setter
    def MaType(self, value):
        self._ma_type.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def MaShift(self):
        return self._ma_shift.Value

    @MaShift.setter
    def MaShift(self, value):
        self._ma_shift.Value = value

    @property
    def PriceType(self):
        return self._price_type.Value

    @PriceType.setter
    def PriceType(self, value):
        self._price_type.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def BuyPositionOpen(self):
        return self._buy_open.Value

    @BuyPositionOpen.setter
    def BuyPositionOpen(self, value):
        self._buy_open.Value = value

    @property
    def SellPositionOpen(self):
        return self._sell_open.Value

    @SellPositionOpen.setter
    def SellPositionOpen(self, value):
        self._sell_open.Value = value

    @property
    def BuyPositionClose(self):
        return self._buy_close.Value

    @BuyPositionClose.setter
    def BuyPositionClose(self, value):
        self._buy_close.Value = value

    @property
    def SellPositionClose(self):
        return self._sell_close.Value

    @SellPositionClose.setter
    def SellPositionClose(self, value):
        self._sell_close.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    def _create_ma(self, ma_type, length):
        if ma_type == MA_SMA:
            ma = SimpleMovingAverage()
        elif ma_type == MA_EMA:
            ma = ExponentialMovingAverage()
        elif ma_type == MA_DEMA:
            ma = DoubleExponentialMovingAverage()
        elif ma_type == MA_TEMA:
            ma = TripleExponentialMovingAverage()
        elif ma_type == MA_WMA:
            ma = WeightedMovingAverage()
        elif ma_type == MA_VWMA:
            ma = VolumeWeightedMovingAverage()
        else:
            ma = SimpleMovingAverage()
        ma.Length = length
        return ma

    def _get_price(self, candle):
        pt = self.PriceType
        if pt == PRICE_CLOSE:
            return float(candle.ClosePrice)
        elif pt == PRICE_HIGH:
            return float(candle.HighPrice)
        elif pt == PRICE_OPEN:
            return float(candle.OpenPrice)
        elif pt == PRICE_LOW:
            return float(candle.LowPrice)
        elif pt == PRICE_TYPICAL:
            return (float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 3.0
        elif pt == PRICE_CENTER:
            return (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        else:
            return float(candle.ClosePrice)

    def OnStarted(self, time):
        super(simple_trading_system_strategy, self).OnStarted(time)

        self._ma = self._create_ma(self.MaType, self.MaPeriod)

        ma_shift = self.MaShift
        self._ma_buffer = [0.0] * (ma_shift + 1)
        self._close_buffer = [0.0] * (self.MaPeriod + ma_shift + 1)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(self.TakeProfit, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLoss, UnitTypes.Absolute))

    def _shift_buffer(self, buf, value):
        for i in range(len(buf) - 1):
            buf[i] = buf[i + 1]
        buf[len(buf) - 1] = value

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = self._get_price(candle)
        mi = DecimalIndicatorValue(self._ma, price, candle.OpenTime)
        mi.IsFinal = True
        ma_result = self._ma.Process(mi)
        ma_value = float(ma_result)

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        self._shift_buffer(self._ma_buffer, ma_value)
        self._shift_buffer(self._close_buffer, float(candle.ClosePrice))

        if self._close_buffer[0] == 0.0:
            return

        ma_shift = self.MaShift
        ma0 = self._ma_buffer[len(self._ma_buffer) - 1]
        ma1 = self._ma_buffer[len(self._ma_buffer) - 1 - ma_shift]

        close = self._close_buffer[len(self._close_buffer) - 1]
        close_shift = self._close_buffer[len(self._close_buffer) - 1 - ma_shift]
        close_sum = self._close_buffer[0]
        open_price = float(candle.OpenPrice)

        buy_signal = (self._sign < 1 and ma0 <= ma1
                      and close >= close_shift and close <= close_sum
                      and close < open_price)
        sell_signal = (self._sign > -1 and ma0 >= ma1
                       and close <= close_shift and close >= close_sum
                       and close > open_price)

        if self._bars_since_trade >= self.CooldownBars and buy_signal:
            if self.BuyPositionOpen and self.IsFormedAndOnlineAndAllowTrading() and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif self.SellPositionClose and self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self._sign = 1
            self._bars_since_trade = 0
        elif self._bars_since_trade >= self.CooldownBars and sell_signal:
            if self.SellPositionOpen and self.IsFormedAndOnlineAndAllowTrading() and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
            elif self.BuyPositionClose and self.Position > 0:
                self.SellMarket(abs(self.Position))
            self._sign = -1
            self._bars_since_trade = 0

    def OnReseted(self):
        super(simple_trading_system_strategy, self).OnReseted()
        self._ma = None
        self._ma_buffer = []
        self._close_buffer = []
        self._sign = 0
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return simple_trading_system_strategy()
