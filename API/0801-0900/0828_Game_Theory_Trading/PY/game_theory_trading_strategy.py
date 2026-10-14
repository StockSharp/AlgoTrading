import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, AccumulationDistributionLine, SimpleMovingAverage, StandardDeviation, Highest, Lowest, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class game_theory_trading_strategy(Strategy):
    """
    Game theory trading strategy.
    Herd behaviour is an RSI extreme (above 70 or below 30) on volume above HerdThreshold times its VolumeMaLength average. A liquidity
    trap is a sweep beyond the LiquidityLookback high or low that closes back inside. Institutional flow is volume above
    InstVolumeMultiplier times its average, and the smart money bias is the accumulation/distribution line against its InstMaLength
    average. The Nash equilibrium is the NashPeriod SMA with bands one standard deviation away.
    Longs come from herd selling into a bear trap or accumulation (contrarian), institutional buying with accumulation above the
    InstMaLength SMA (momentum), or a close back above the lower Nash band (reversion); shorts mirror them. The size is increased by
    half on institutional volume and halved when price is within NashDeviation of the equilibrium. Opposite signals reverse, and
    optional percent stop loss and take profit close the position.
    """

    RSI_OVERBOUGHT = 70
    RSI_OVERSOLD = 30

    def __init__(self):
        super(game_theory_trading_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "Herd")
        self._volume_ma_length = self.Param("VolumeMaLength", 20).SetGreaterThanZero().SetDisplay("Volume MA Length", "Length of the volume average", "Herd")
        self._herd_threshold = self.Param("HerdThreshold", 2.0).SetGreaterThanZero().SetDisplay("Herd Threshold", "Volume to average ratio that marks herd behaviour", "Herd")
        self._liquidity_lookback = self.Param("LiquidityLookback", 50).SetGreaterThanZero().SetDisplay("Liquidity Lookback", "Candles whose high and low define liquidity pools", "Liquidity")
        self._inst_volume_multiplier = self.Param("InstVolumeMultiplier", 2.5).SetGreaterThanZero().SetDisplay("Inst Volume Multiplier", "Volume to average ratio that marks institutional flow", "Institutional")
        self._inst_ma_length = self.Param("InstMaLength", 21).SetGreaterThanZero().SetDisplay("Inst MA Length", "Length of the institutional trend and A/D averages", "Institutional")
        self._nash_period = self.Param("NashPeriod", 100).SetGreaterThanZero().SetDisplay("Nash Period", "Period of the Nash equilibrium average and deviation", "Nash")
        self._nash_deviation = self.Param("NashDeviation", 0.02).SetNotNegative().SetDisplay("Nash Deviation", "Relative distance from the equilibrium regarded as near it", "Nash")
        self._use_stop_loss = self.Param("UseStopLoss", True).SetDisplay("Use Stop Loss", "Enable the stop loss", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._use_take_profit = self.Param("UseTakeProfit", True).SetDisplay("Use Take Profit", "Enable the take profit", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 5.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_ma = None
        self._ad_ma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_highest = None
        self._prev_lowest = None
        self._prev_close = None
        self._prev_upper_band = None
        self._prev_lower_band = None

    def OnReseted(self):
        super(game_theory_trading_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(game_theory_trading_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        ad = AccumulationDistributionLine()
        nash_ma = SimpleMovingAverage()
        nash_ma.Length = self._nash_period.Value
        nash_std = StandardDeviation()
        nash_std.Length = self._nash_period.Value
        inst_ma = SimpleMovingAverage()
        inst_ma.Length = self._inst_ma_length.Value
        highest = Highest()
        highest.Length = self._liquidity_lookback.Value
        lowest = Lowest()
        lowest.Length = self._liquidity_lookback.Value

        self._volume_ma = SimpleMovingAverage()
        self._volume_ma.Length = self._volume_ma_length.Value
        self._ad_ma = SimpleMovingAverage()
        self._ad_ma.Length = self._inst_ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ad, nash_ma, nash_std, inst_ma, highest, lowest, self._process_candle).Start()

        take = Unit(Decimal(self._take_profit_percent.Value), UnitTypes.Percent) if self._use_take_profit.Value and self._take_profit_percent.Value > 0 else Unit()
        stop = Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent) if self._use_stop_loss.Value and self._stop_loss_percent.Value > 0 else Unit()
        self.StartProtection(take, stop, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, nash_ma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, rsi, ad, nash_ma, nash_std, inst_ma, highest, lowest):
        if candle.State != CandleStates.Finished:
            return

        volume_input = DecimalIndicatorValue(self._volume_ma, candle.TotalVolume, candle.OpenTime)
        volume_input.IsFinal = True
        volume_value = self._volume_ma.Process(volume_input)
        ad_input = DecimalIndicatorValue(self._ad_ma, ad, candle.OpenTime)
        ad_input.IsFinal = True
        ad_value = self._ad_ma.Process(ad_input)

        close = candle.ClosePrice
        upper_band = nash_ma + nash_std
        lower_band = nash_ma - nash_std

        # Liquidity pools are the extremes of the candles before this one.
        pool_high = self._prev_highest
        pool_low = self._prev_lowest
        prev_close = self._prev_close
        prev_upper = self._prev_upper_band
        prev_lower = self._prev_lower_band

        self._prev_highest = highest
        self._prev_lowest = lowest
        self._prev_close = close
        self._prev_upper_band = upper_band
        self._prev_lower_band = lower_band

        if not self._volume_ma.IsFormed or not self._ad_ma.IsFormed or pool_high is None or pool_low is None or prev_close is None:
            return
        if prev_upper is None or prev_lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        volume_avg = volume_value.GetValue[Decimal](None)
        ad_avg = ad_value.GetValue[Decimal](None)
        volume = candle.TotalVolume

        volume_spike = volume_avg > 0 and volume > volume_avg * Decimal(self._herd_threshold.Value)
        herd_buying = rsi > self.RSI_OVERBOUGHT and volume_spike
        herd_selling = rsi < self.RSI_OVERSOLD and volume_spike

        bull_trap = candle.HighPrice > pool_high and close < pool_high
        bear_trap = candle.LowPrice < pool_low and close > pool_low

        institutional = volume_avg > 0 and volume > volume_avg * Decimal(self._inst_volume_multiplier.Value)
        accumulation = ad > ad_avg
        distribution = ad < ad_avg

        contrarian_long = herd_selling and (bear_trap or accumulation)
        contrarian_short = herd_buying and (bull_trap or distribution)

        momentum_long = institutional and accumulation and close > inst_ma and close > candle.OpenPrice
        momentum_short = institutional and distribution and close < inst_ma and close < candle.OpenPrice

        nash_long = prev_close < prev_lower and close > lower_band
        nash_short = prev_close > prev_upper and close < upper_band

        size = self.Volume
        if institutional:
            size = size * Decimal(1.5)
        if nash_ma > 0 and abs(close - nash_ma) / nash_ma <= Decimal(self._nash_deviation.Value):
            size = size / Decimal(2)

        if (contrarian_long or momentum_long or nash_long) and self.Position <= 0:
            self.BuyMarket(size + abs(self.Position))
        elif (contrarian_short or momentum_short or nash_short) and self.Position >= 0:
            self.SellMarket(size + abs(self.Position))

    def CreateClone(self):
        return game_theory_trading_strategy()
