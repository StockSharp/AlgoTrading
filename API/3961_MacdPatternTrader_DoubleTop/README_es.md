# Estrategia Macd Pattern Trader DoubleTop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Puerto del asesor experto MetaTrader 4 **MacdPatternTraderv04cb**. La estrategia escanea una línea principal MACD configurable para
Patrones de doble techo bajista y doble fondo alcista. Cuando el segundo swing no logra superar al primero mientras el MACD
permanece más allá de un nivel de activación positivo o negativo, la estrategia abre una posición de mercado en la dirección del
reversión anticipada. Las órdenes de protección reproducen las distancias fijas originales de stop-loss de 100 pips y de toma de ganancias de 300 pips.

## Reglas comerciales

1. Suscríbase a la serie de velas seleccionada (predeterminado: período de tiempo de 30 minutos) y calcule la línea principal MACD con el
configurados períodos rápido, lento y de señal (predeterminados: 5, 13 y 1).
2. Realice un seguimiento de los últimos tres valores MACD terminados. Una configuración bajista se arma una vez que el MACD se mantiene por encima del `TriggerLevel`,
forma un máximo local y luego desciende. La configuración se valida cuando el siguiente máximo MACD es inferior al almacenado previamente
alto mientras el MACD todavía está por encima del disparador. En ese momento se envía una venta de mercado.
3. Refleje la misma lógica bajo cero. Cuando el MACD permanece por debajo de `-TriggerLevel`, forma una depresión y la siguiente depresión
es mayor que el anterior, la estrategia abre una compra de mercado.
4. Restablezca los picos y valles almacenados cada vez que la línea MACD vuelva a cruzar dentro del `[-TriggerLevel, TriggerLevel]`
rango. Esto coincide con el comportamiento original EA que cancela la búsqueda de patrones cuando el impulso pierde fuerza.
5. Los tamaños de posición comienzan desde el `TradeVolume` configurado. Al cambiar de dirección, la estrategia agrega suficiente volumen para
aplanar la exposición opuesta antes de establecer la nueva operación.
6. Llame a `StartProtection` una vez al inicio para que tanto el stop-loss de 100 pips como el take-profit de 300 pips sean gestionados por el
plataforma incluso después de reiniciar.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `FastPeriod` | Longitud rápida de EMA utilizada por MACD. |
| `SlowPeriod` | Longitud lenta de EMA utilizada por MACD. |
| `SignalPeriod` | Longitud de suavizado de la línea de señal para MACD. |
| `TriggerLevel` | Nivel absoluto de MACD requerido para activar la detección de doble techo/doble fondo. |
| `StopLossPips` | Distancia del tope de protección en pips (por defecto 100). |
| `TakeProfitPips` | Distancia de la toma de ganancias en pips (por defecto 300). |
| `TradeVolume` | Volumen base de pedidos para nuevas posiciones. |
| `CandleType` | Serie de velas utilizadas para los cálculos de indicadores. |

## Notas

- El stop-loss y el take-profit se convierten de pips en pasos de instrumentos antes de pasarlos a
`StartProtection`, manteniendo el comportamiento idéntico al experto MQL4 original.
- Todos los indicadores y comentarios comerciales dentro del código fuente de C# están escritos en inglés, según lo exige el repositorio.
directrices.
