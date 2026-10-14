import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class double_rsi_strategy(Strategy):
    """
    Double RSI strategy.
    An RSI on the trading timeframe and another on MTFTimeframe. A long opens when the trading RSI crosses up out of
    the oversold zone while the higher-timeframe RSI is rising (bullish); a short opens when it crosses down out of the
    overbought zone while the higher-timeframe RSI is falling (bearish). The opposite RSI exit closes the position and an
    optional percent take-profit locks in gains.
    """

    def __init__(self):
        super(double_rsi_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle type", "Trading timeframe", "General")
        self._rsi_length = self.Param("RSILength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period on both timeframes", "RSI")
        self._mtf_timeframe = self.Param("MTFTimeframe", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("MTF Timeframe", "Higher timeframe for the confirming RSI", "RSI")
        self._oversold = self.Param("Oversold", 30.0) \
            .SetDisplay("Oversold", "RSI oversold level", "RSI")
        self._overbought = self.Param("Overbought", 70.0) \
            .SetDisplay("Overbought", "RSI overbought level", "RSI")
        self._use_tp = self.Param("UseTP", False) \
            .SetDisplay("Use Take Profit", "Enable the percent take-profit", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit %", "Take-profit percentage from the entry price", "Risk")

        self._prev_rsi = None
        self._mtf_rsi = None
        self._prev_mtf_rsi = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def MTFTimeframe(self):
        return self._mtf_timeframe.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Security, self.MTFTimeframe)]

    def OnReseted(self):
        super(double_rsi_strategy, self).OnReseted()
        self._prev_rsi = None
        self._mtf_rsi = None
        self._prev_mtf_rsi = None

    def OnStarted2(self, time):
        super(double_rsi_strategy, self).OnStarted2(time)

        self._prev_rsi = None
        self._mtf_rsi = None
        self._prev_mtf_rsi = None

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        mtf_rsi = RelativeStrengthIndex()
        mtf_rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(rsi, self._process_candle).Start()

        self.SubscribeCandles(self.MTFTimeframe).BindEx(mtf_rsi, self._process_mtf_candle).Start()

        if self._use_tp.Value:
            self.StartProtection(Unit(Decimal(self._take_profit_percent.Value), UnitTypes.Percent), Unit(), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            rsi_area = self.CreateChartArea()
            if rsi_area is not None:
                self.DrawIndicator(rsi_area, rsi)
                self.DrawIndicator(rsi_area, mtf_rsi)

    def _process_mtf_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not rsi_value.IsFormed:
            return

        self._prev_mtf_rsi = self._mtf_rsi
        self._mtf_rsi = float(rsi_value.GetValue[Decimal](None))

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not rsi_value.IsFormed:
            return

        rsi = float(rsi_value.GetValue[Decimal](None))
        prev_rsi = self._prev_rsi
        self._prev_rsi = rsi

        if prev_rsi is None or self._mtf_rsi is None or self._prev_mtf_rsi is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        oversold = float(self._oversold.Value)
        overbought = float(self._overbought.Value)

        exits_oversold = prev_rsi < oversold and rsi >= oversold
        exits_overbought = prev_rsi > overbought and rsi <= overbought

        long_signal = exits_oversold and self._mtf_rsi > self._prev_mtf_rsi
        short_signal = exits_overbought and self._mtf_rsi < self._prev_mtf_rsi

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and exits_overbought:
            self.SellMarket(self.Position)
        elif self.Position < 0 and exits_oversold:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return double_rsi_strategy()
