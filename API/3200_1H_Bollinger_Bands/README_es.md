# Estrategia de 1H Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de 1H Bollinger Bands** adapta el experto de MetaTrader "1H Bolinger Bands" a la API de alto nivel de StockSharp. La idea es operar rebotes desde las Bollinger Bands diarias mientras la tendencia horaria está alineada y el MACD mensual a largo plazo confirma la dirección. La estrategia funciona en el marco temporal H1 (por defecto) y se basa en flujos de datos de marcos temporales superiores adicionales para la confirmación.

## Lógica de trading
- **Filtro de tendencia:** Dos medias móviles ponderadas linealmente (LWMA 250 y 500) en el marco temporal base aseguran que solo se permitan operaciones alineadas con la dirección dominante.
- **Patrón de activación:** En el marco temporal superior (diario por defecto), la estrategia busca una vela cuyo mínimo perfora por debajo de la Banda de Bollinger inferior y la siguiente vela abre de nuevo por encima (inverso para posiciones short con la banda superior). Esto replica la condición de rebote original.
- **Confirmación de momentum:** El Momentum (período 14) se calcula en el marco temporal superior. Al menos una de las tres desviaciones de momentum más recientes desde 100 debe superar el umbral configurado (por defecto 0.3).
- **Filtro MACD:** Un MACD mensual (12/26/9) debe estar de acuerdo con la señal. Para operaciones long, la línea MACD debe estar por encima de la línea de señal; para posiciones short, debe estar por debajo.
- **Entrada:** Cuando todos los filtros se alinean, la estrategia abre una orden de mercado. Si hay una posición opuesta abierta, el volumen solicitado neutraliza la exposición existente e invierte la dirección.

## Gestión de posiciones
La gestión de riesgos se implementa directamente en la estrategia usando distancias basadas en pips convertidas a través de `Security.PriceStep`:
- **Stop Loss:** Cierra la posición una vez que el precio se mueve contra la entrada por el número configurado de pips.
- **Take Profit:** Asegura ganancias cuando el precio alcanza el objetivo de pips configurado.
- **Trailing Stop (opcional):** Cuando está habilitado y el movimiento supera la distancia de trailing, un nivel de trailing interno sigue el precio. Una barra que penetra ese nivel cierra la operación.
- **Break-Even (opcional):** Después de que el precio avanza por la distancia de activación, el nivel de stop se mueve al precio de entrada más el offset configurado (menos para posiciones short). Un retroceso a ese nivel sale de la posición.

La gestión de ganancias basada en dinero del experto original no se recrea; la versión de StockSharp se centra en controles basados en precio para permanecer agnóstica respecto al intercambio.

## Parámetros
| Parámetro | Descripción | Por defecto |
|-----------|-------------|---------|
| `CandleType` | Marco temporal base para la evaluación de señales. | Velas de 1 hora |
| `HigherTimeFrame` | Marco temporal utilizado para Bollinger Bands y momentum. | Velas de 1 día |
| `MacdTimeFrame` | Marco temporal para el MACD de confirmación. | Velas de 30 días |
| `FastMaPeriod` / `SlowMaPeriod` | Longitudes de LWMA rápida/lenta en el marco temporal base. | 6 / 85 |
| `TrendFastPeriod` / `TrendSlowPeriod` | Filtros de tendencia LWMA a largo plazo. | 250 / 500 |
| `MomentumPeriod` | Lookback de momentum en el marco temporal superior. | 14 |
| `MomentumThreshold` | Desviación absoluta mínima desde 100 para momentum. | 0.3 |
| `BollingerPeriod` / `BollingerWidth` | Configuración de la Banda de Bollinger diaria. | 20 / 2.0 |
| `TradeVolume` | Volumen base para cada nueva posición. | 1 |
| `StopLossPips` / `TakeProfitPips` | Stop de protección y objetivo en pips. | 20 / 50 |
| `EnableTrailing` / `TrailingStopPips` | Toggle de trailing stop y distancia. | true / 40 |
| `EnableBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Toggle de break-even, distancia de activación y offset. | true / 30 / 30 |

Todos los parámetros numéricos se exponen a través de `StrategyParam<T>` y se pueden optimizar en Designer/Runner.

## Notas de implementación
- La estrategia se suscribe simultáneamente a tres flujos de velas: marco temporal base, marco temporal superior para Bollinger/Momentum y marco temporal MACD.
- El Momentum usa el indicador estándar de StockSharp `Momentum` y almacena las últimas tres desviaciones para imitar la lógica MQL.
- El volumen de trading y las distancias en pips asumen que `Security.PriceStep` está correctamente rellenado; de lo contrario, la lógica protectora no se activará.
- StockSharp mantiene una única posición neta. El comportamiento de escalado "Max_Trades" del script original se simplifica a una única posición agregada en este port.
- Las salidas basadas en equity y las características de trailing de dinero de la versión MQL se omiten intencionalmente para mantener la implementación neutra respecto al intercambio.

## Uso
1. Adjunte la estrategia a un instrumento que proporcione velas horarias, diarias y mensuales (o ajuste los parámetros según corresponda).
2. Asegúrese de que el instrumento exponga `PriceStep` para que las distancias en pips se traduzcan en offsets de precio.
3. Configure el volumen y los parámetros de riesgo deseados en la UI o en código antes de iniciar la estrategia.
4. Inicie la estrategia; se suscribirá automáticamente a los datos necesarios, evaluará señales en velas cerradas y gestionará la posición con las reglas de protección configuradas.

## Diferencias conocidas con el experto MQL
- El trailing basado en dinero y el stop total de equity no están implementados; solo se retienen los controles basados en precio.
- Las alertas, correos electrónicos y notificaciones push del código MQL se omiten.
- El apilamiento de órdenes se reemplaza por el modelo de posición neta única de StockSharp.

Estos ajustes mantienen la estrategia idiomática para StockSharp mientras preservan la idea de trading central del experto original.
