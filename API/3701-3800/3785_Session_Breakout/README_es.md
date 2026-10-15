# Estrategia de ruptura de sesión
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Session Breakout replica el asesor experto MetaTrader "Session breakout". Mira la sesión matutina europea.
y mide su rango de precios. Cuando ese rango es lo suficientemente estrecho, la estrategia se prepara para negociar rupturas durante los EE.UU.
sesión de la tarde usando el nivel alto de StockSharp API. La implementación exige como máximo una entrada larga y una corta por día.
Y adjunta automáticamente órdenes de protección (stop loss y takeprofit) a cada posición.

## Lógica comercial
- Restablezca el estado al comienzo de cada día de negociación y omita los fines de semana. Los lunes son opcionales y están controlados por un parámetro.
- Realice un seguimiento de las velas terminadas durante la sesión europea (predeterminada de 06:00 a 12:00) y registre el máximo más alto y el mínimo más bajo.
- Al inicio de la sesión de EE. UU., el rango capturado se clasifica como "pequeño" cuando su ancho es menor que `SmallSessionThreshol
dPips`.
- Si el rango es pequeño, monitoree las velas de la sesión de EE. UU. (predeterminado de 12:00 a 16:00) y espere hasta que al menos una barra de EE. UU. se haya cerrado (`Eu
ropeSessionStartHour + 5` to `EuropeSessionStartHour + 10`).
- Una ruptura larga se activa cuando toda la vela se mantiene por encima del máximo europeo más un buffer configurable (`BreakoutBuffer
Pipas`). Una ruptura breve requiere que la vela se mantenga por debajo del mínimo europeo menos el colchón.
- Después de entrar en una posición, la estrategia fija niveles de stop-loss y take-profit expresados en pips y evita ene adicionales.
Intenta en la misma dirección durante el resto del día.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `Volume` | Volumen de órdenes utilizado tanto para rupturas largas como cortas. |
| `EuropeSessionStartHour` | Hora en la que comienza el seguimiento del alcance europeo. |
| `EuropeSessionEndHour` | Hora en la que se detiene el seguimiento del alcance europeo. |
| `UsSessionStartHour` | Hora que marca el inicio de la ventana de sesiones de EE.UU. |
| `UsSessionEndHour` | Hora que marca el final de la ventana de sesiones de EE.UU. |
| `SmallSessionThresholdPips` | Ancho máximo (en pips) para que el rango europeo califique como squeeze. |
| `BreakoutBufferPips` | Se agregó un búfer adicional por encima o por debajo del rango antes de desencadenar rupturas. |
| `TradeOnMonday` | Permite operar los lunes. Siempre se saltan los fines de semana. |
| `TakeProfitPips` | Distancia entre el precio de entrada y el nivel de obtención de beneficios. |
| `StopLossPips` | Distancia entre el precio de entrada y el nivel de stop-loss. |
| `CandleType` | Serie de velas utilizadas para todos los cálculos (velas de 15 minutos por defecto). |

## Notas
- El tamaño del pip se deriva del instrumento `PriceStep`. Ajuste los parámetros basados en pips para que coincidan con la especificación del contrato.
s del valor seleccionado.
- Debido a que las órdenes se generan cuando se cierra una vela calificada, los llenados ocurren al precio de cierre de esa vela en las pruebas retrospectivas. Liv
Los rellenos pueden variar dependiendo de las condiciones del mercado.
- Sólo se puede abrir una operación larga y una corta por día. La lógica refleja el comportamiento original del asesor experto al usar S
Ayudantes de gestión de riesgos basados en posiciones de tockSharp.
