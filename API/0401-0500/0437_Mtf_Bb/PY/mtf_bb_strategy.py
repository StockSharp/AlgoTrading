import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import BollingerBands, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class mtf_bb_strategy(Strategy):
    """
    Multi-timeframe Bollinger Bands strategy.
    Bollinger Bands run on the trading timeframe and on MtfCandleType. A long opens when the close is below the
    higher-timeframe lower band and a short when it is above the higher-timeframe upper band; with UseMaFilter the close
    must also be above (long) or below (short) the EMA. A long exits on a close above the trading-timeframe upper band,
    a short on a close below its lower band, and SLPercent sets a percent stop-loss.
    """

    def __init__(self):
        super(mtf_bb_strategy, self).__init__()
        self._bb_length = self.Param("BBLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Multiplier", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")
        self._use_ma_filter = self.Param("UseMaFilter", False) \
            .SetDisplay("Use MA Filter", "Require the close on the trade side of the EMA", "Filter")
        self._ma_length = self.Param("MaLength", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "EMA filter period", "Filter")
        self._sl_percent = self.Param("SLPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("SL %", "Stop-loss percentage, 0 disables", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle type", "Trading timeframe", "General")
        self._mtf_candle_type = self.Param("MtfCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))) \
            .SetDisplay("MTF Candle type", "Higher timeframe for the entry bands", "General")

        self._mtf_upper = None
        self._mtf_lower = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def MtfCandleType(self):
        return self._mtf_candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Security, self.MtfCandleType)]

    def OnReseted(self):
        super(mtf_bb_strategy, self).OnReseted()
        self._mtf_upper = None
        self._mtf_lower = None

    def OnStarted2(self, time):
        super(mtf_bb_strategy, self).OnStarted2(time)

        self._mtf_upper = None
        self._mtf_lower = None

        bollinger = BollingerBands()
        bollinger.Length = self._bb_length.Value
        bollinger.Width = Decimal(self._bb_multiplier.Value)
        mtf_bollinger = BollingerBands()
        mtf_bollinger.Length = self._bb_length.Value
        mtf_bollinger.Width = Decimal(self._bb_multiplier.Value)
        ma = ExponentialMovingAverage()
        ma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bollinger, ma, self._process_candle).Start()

        self.SubscribeCandles(self.MtfCandleType).BindEx(mtf_bollinger, self._process_mtf_candle).Start()

        sl = float(self._sl_percent.Value)
        if sl > 0:
            self.StartProtection(Unit(), Unit(Decimal(sl), UnitTypes.Percent), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_mtf_candle(self, candle, bollinger_value):
        if candle.State != CandleStates.Finished:
            return

        if not bollinger_value.IsFormed:
            return

        if bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        self._mtf_upper = float(bollinger_value.UpBand)
        self._mtf_lower = float(bollinger_value.LowBand)

    def _process_candle(self, candle, bollinger_value, ma_value):
        if candle.State != CandleStates.Finished:
            return

        use_ma = self._use_ma_filter.Value

        if not bollinger_value.IsFormed or (use_ma and not ma_value.IsFormed):
            return

        if bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        if self._mtf_upper is None or self._mtf_lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        upper = float(bollinger_value.UpBand)
        lower = float(bollinger_value.LowBand)
        close = float(candle.ClosePrice)
        ma = float(ma_value.GetValue[Decimal](None)) if use_ma else 0.0

        long_signal = close < self._mtf_lower and (not use_ma or close > ma)
        short_signal = close > self._mtf_upper and (not use_ma or close < ma)

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close > upper:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close < lower:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return mtf_bb_strategy()
