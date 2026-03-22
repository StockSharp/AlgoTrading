import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence, BollingerBands, RelativeStrengthIndex, IndicatorHelper, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class macd_bb_rsi_strategy(Strategy):
    """MACD + Bollinger Bands + RSI Strategy."""

    def __init__(self):
        super(macd_bb_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._bb_length = self.Param("BBLength", 20) \
            .SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands")
        self._bb_width = self.Param("BBWidth", 1.5) \
            .SetDisplay("BB Width", "BB standard deviation multiplier", "Bollinger Bands")
        self._rsi_length = self.Param("RSILength", 14) \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._cooldown_bars = self.Param("CooldownBars", 50) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._macd = None
        self._bollinger = None
        self._rsi = None
        self._prev_macd = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_bb_rsi_strategy, self).OnReseted()
        self._macd = None
        self._bollinger = None
        self._rsi = None
        self._prev_macd = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(macd_bb_rsi_strategy, self).OnStarted(time)

        self._macd = MovingAverageConvergenceDivergence()
        self._bollinger = BollingerBands()
        self._bollinger.Length = int(self._bb_length.Value)
        self._bollinger.Width = float(self._bb_width.Value)
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._on_process).Start()

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(1, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        # Process MACD and BB manually (need CandleIndicatorValue wrapper for Python)
        civ_macd = CandleIndicatorValue(self._macd, candle)
        civ_macd.IsFinal = True
        macd_result = self._macd.Process(civ_macd)

        civ_bb = CandleIndicatorValue(self._bollinger, candle)
        civ_bb.IsFinal = True
        bb_result = self._bollinger.Process(civ_bb)

        if not self._macd.IsFormed or not self._bollinger.IsFormed:
            return

        macd_val = float(IndicatorHelper.ToDecimal(macd_result))

        if bb_result.UpBand is None or bb_result.LowBand is None or bb_result.MovingAverage is None:
            return

        upper = float(bb_result.UpBand)
        lower = float(bb_result.LowBand)
        middle = float(bb_result.MovingAverage)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_macd = macd_val
            return

        close = float(candle.ClosePrice)
        rsi = float(rsi_val)
        cooldown = int(self._cooldown_bars.Value)

        # Buy: price below lower BB + RSI oversold
        if close <= lower and rsi < 30 and self.Position == 0:
            self.BuyMarket()
            self._cooldown_remaining = cooldown
        # Sell: price above upper BB + RSI overbought
        elif close >= upper and rsi > 70 and self.Position == 0:
            self.SellMarket()
            self._cooldown_remaining = cooldown

        self._prev_macd = macd_val

    def CreateClone(self):
        return macd_bb_rsi_strategy()
