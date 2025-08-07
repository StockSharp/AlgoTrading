import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Array, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class mtf_bb_strategy(Strategy):
    """Multi-timeframe Bollinger Bands strategy.

    Uses Bollinger Bands calculated on both the working timeframe and a
    higher timeframe. Entries occur when price crosses the higher
    timeframe bands and is confirmed by the optional moving average
    filter.
    """

    def __init__(self):
        super(mtf_bb_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)).SetDisplay(
            "Candle type", "Main timeframe", "General"
        )
        self._mtf_candle_type = self.Param("MtfCandleType", tf(60)).SetDisplay(
            "MTF Candle type", "Multi-timeframe for BB", "MTF Bollinger Bands"
        )
        self._bb_length = self.Param("BBLength", 20).SetDisplay(
            "BB Length", "Bollinger Bands period", "MTF Bollinger Bands"
        )
        self._bb_multiplier = self.Param("BBMultiplier", 2.0).SetDisplay(
            "BB StdDev", "Standard deviation multiplier", "MTF Bollinger Bands"
        )
        self._use_ma_filter = self.Param("UseMaFilter", False).SetDisplay(
            "Use MA Filter", "Enable Moving Average filter", "MTF Moving Average Filter"
        )
        self._ma_length = self.Param("MaLength", 200).SetDisplay(
            "MA Length", "Moving Average period", "MTF Moving Average Filter"
        )
        self._show_long = self.Param("ShowLong", True).SetDisplay(
            "Long entries", "Enable long positions", "Strategy"
        )
        self._show_short = self.Param("ShowShort", False).SetDisplay(
            "Short entries", "Enable short positions", "Strategy"
        )
        self._use_sl = self.Param("UseSL", True).SetDisplay(
            "Enable SL", "Enable Stop Loss", "Stop Loss"
        )
        self._sl_percent = self.Param("SLPercent", 2.0).SetDisplay(
            "SL Percent", "Stop loss percentage", "Stop Loss"
        )

        self._bb = None
        self._mtf_bb = None
        self._ma = None
        
        # Store previous indicator values
        self._prev_mtf_bb_upper = 0.0
        self._prev_mtf_bb_lower = 0.0
        self._prev_ma_value = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def mtf_candle_type(self):
        return self._mtf_candle_type.Value

    def OnReseted(self):
        super(mtf_bb_strategy, self).OnReseted()
        self._prev_mtf_bb_upper = 0.0
        self._prev_mtf_bb_lower = 0.0
        self._prev_ma_value = 0.0

    def OnStarted(self, time):
        super(mtf_bb_strategy, self).OnStarted(time)

        self._bb = BollingerBands()
        self._bb.Length = self._bb_length.Value
        self._bb.Width = self._bb_multiplier.Value

        self._mtf_bb = BollingerBands()
        self._mtf_bb.Length = self._bb_length.Value
        self._mtf_bb.Width = self._bb_multiplier.Value

        if self._use_ma_filter.Value:
            self._ma = ExponentialMovingAverage()
            self._ma.Length = self._ma_length.Value

        sub = self.SubscribeCandles(self.candle_type)
        mtf_sub = self.SubscribeCandles(self.mtf_candle_type)

        mtf_sub.BindEx(self._mtf_bb, self.OnProcessMtf).Start()
        if self._use_ma_filter.Value:
            mtf_sub.BindEx(self._ma, self.OnProcessMa).Start()
        sub.BindEx(self._bb, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._bb)
            if self._use_ma_filter.Value and self._ma is not None:
                self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

        if self._use_sl.Value:
            self.StartProtection(Unit(), Unit(self._sl_percent.Value, UnitTypes.Percent))

    def OnProcessMtf(self, candle, mtf_bb_val):
        # Store MTF Bollinger Bands values
        if mtf_bb_val is not None and self._mtf_bb.IsFormed:
            bb_typed = mtf_bb_val  # BollingerBandsValue
            self._prev_mtf_bb_upper = float(bb_typed.UpBand) if bb_typed.UpBand is not None else 0.0
            self._prev_mtf_bb_lower = float(bb_typed.LowBand) if bb_typed.LowBand is not None else 0.0

    def OnProcessMa(self, candle, ma_val):
        # Store MA value
        if ma_val is not None and self._ma.IsFormed:
            self._prev_ma_value = float(ma_val)

    def OnProcess(self, candle, bb_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._bb.IsFormed or not self._mtf_bb.IsFormed:
            return
        if self._use_ma_filter.Value and (self._ma is None or not self._ma.IsFormed):
            return

        bb = bb_val
        upper = float(bb.UpBand) if bb.UpBand is not None else 0.0
        lower = float(bb.LowBand) if bb.LowBand is not None else 0.0
        
        # Use stored MTF Bollinger Bands values instead of GetValue()
        mtf_upper = self._prev_mtf_bb_upper
        mtf_lower = self._prev_mtf_bb_lower

        buy_ma_filter = True
        sell_ma_filter = True
        if self._use_ma_filter.Value and self._ma is not None:
            # Use stored MA value instead of GetValue()
            ma_val = self._prev_ma_value
            buy_ma_filter = candle.ClosePrice > ma_val
            sell_ma_filter = candle.ClosePrice < ma_val

        buy = candle.ClosePrice < mtf_lower and buy_ma_filter
        sell = candle.ClosePrice > mtf_upper and sell_ma_filter

        if self._show_long.Value and buy and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif self._show_long.Value and self.Position > 0 and candle.ClosePrice > upper:
            self.ClosePosition()

        if self._show_short.Value and sell and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif self._show_short.Value and self.Position < 0 and candle.ClosePrice < lower:
            self.ClosePosition()

    def CreateClone(self):
        return mtf_bb_strategy()
