//@version=4
study("Average Force", "AF")

af(Series, High, Low, Period, PostSmooth) =>
    period       =     max(1, int(Period))
    highestHigh  = highest(High,  period)
    lowestLow    =  lowest( Low,  period)
    HHminusLL    = highestHigh - lowestLow
    averageForce = HHminusLL==0.0 ? 0.0 : (Series - lowestLow) / HHminusLL - 0.5
    sma(averageForce, max(1, int(PostSmooth)))

period = input(18, "Period", input.integer, minval=1)
smooth = input( 6, "Smooth", input.integer, minval=1)

AF = af(close, high, low, period, smooth)

Color = AF>0.0 ? color.yellow : color.fuchsia
plot(AF, color=Color, style=plot.style_columns)