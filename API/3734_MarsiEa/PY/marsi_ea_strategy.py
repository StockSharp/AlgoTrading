import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class marsi_ea_strategy(Strategy):
    """MA + RSI strategy with virtual SL/TP in pips."""

    def __init__(self):
        super(marsi_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Series used for indicator calculations", "General")
        self._ma_period = self.Param("MaPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Simple moving average length", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "RSI lookback length", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 55.0) \
            .SetDisplay("RSI Overbought", "Upper RSI threshold", "Signals")
        self._rsi_oversold = self.Param("RsiOversold", 45.0) \
            .SetDisplay("RSI Oversold", "Lower RSI threshold", "Signals")
        self._stop_loss_pips = self.Param("StopLossPips", 100.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 300.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk")

        self._virtual_stop_price = None
        self._virtual_take_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def RsiOverbought(self):
        return self._rsi_overbought.Value

    @property
    def RsiOversold(self):
        return self._rsi_oversold.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    def OnReseted(self):
        super(marsi_ea_strategy, self).OnReseted()
        self._virtual_stop_price = None
        self._virtual_take_price = None

    def OnStarted2(self, time):
        super(marsi_ea_strategy, self).OnStarted2(time)

        sma = SimpleMovingAverage()
        sma.Length = self.MaPeriod

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(sma, rsi, self._process_candle).Start()

    def _process_candle(self, candle, ma_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        ma_v = float(ma_value)
        rsi_v = float(rsi_value)

        if self.Position > 0:
            if self._virtual_stop_price is not None and float(candle.LowPrice) <= self._virtual_stop_price:
                self.SellMarket(self.Position)
                self._virtual_stop_price = None
                self._virtual_take_price = None
                return
            if self._virtual_take_price is not None and float(candle.HighPrice) >= self._virtual_take_price:
                self.SellMarket(self.Position)
                self._virtual_stop_price = None
                self._virtual_take_price = None
                return
        elif self.Position < 0:
            if self._virtual_stop_price is not None and float(candle.HighPrice) >= self._virtual_stop_price:
                self.BuyMarket(abs(self.Position))
                self._virtual_stop_price = None
                self._virtual_take_price = None
                return
            if self._virtual_take_price is not None and float(candle.LowPrice) <= self._virtual_take_price:
                self.BuyMarket(abs(self.Position))
                self._virtual_stop_price = None
                self._virtual_take_price = None
                return

        if self.Position != 0:
            return

        close_price = float(candle.ClosePrice)
        pip_size = self._get_pip_size()
        if pip_size <= 0:
            pip_size = 1.0

        if close_price > ma_v and rsi_v < float(self.RsiOversold):
            self.BuyMarket()
            self._virtual_stop_price = close_price - float(self.StopLossPips) * pip_size
            self._virtual_take_price = close_price + float(self.TakeProfitPips) * pip_size
        elif close_price < ma_v and rsi_v > float(self.RsiOverbought):
            self.SellMarket()
            self._virtual_stop_price = close_price + float(self.StopLossPips) * pip_size
            self._virtual_take_price = close_price - float(self.TakeProfitPips) * pip_size

    def _get_pip_size(self):
        if self.Security is None or self.Security.PriceStep is None:
            return 0.0

        price_step = float(self.Security.PriceStep)
        if price_step <= 0:
            return 0.0

        decimals = self.Security.Decimals if self.Security.Decimals is not None else 0
        if decimals == 3 or decimals == 5:
            return price_step * 10.0
        return price_step

    def CreateClone(self):
        return marsi_ea_strategy()
