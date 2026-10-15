# Estrategia Wedge Pattern
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general

La **estrategia Wedge Pattern** es una conversión del asesor experto de MetaTrader *Wedge pattern.mq4* a la API de alto nivel de StockSharp. Busca consolidaciones de cuña simétricas derivadas de fractales de Bill Williams y opera las rupturas cuando los filtros de tendencia y momentum se alinean.

La implementación de alto nivel reemplaza la gestión manual de órdenes original con funciones StockSharp, conservando la lógica de decisión:

- **Filtro de tendencia:** compara una media móvil ponderada lineal (LWMA) rápida y una lenta calculadas sobre precios típicos.
- **Filtro de momentum:** evalúa la distancia absoluta del indicador momentum de 14 periodos respecto a su nivel neutral (100). Las tres últimas lecturas de momentum deben superar un umbral configurable.
- **Confirmación MACD:** requiere que la línea principal MACD esté por encima de la línea de señal para largos (o por debajo para cortos).
- **Detección de cuña fractal:** recopila puntos fractales superiores e inferiores para construir líneas de tendencia convergentes. Las señales se producen cuando el precio cierra más allá de esas líneas más un búfer de confirmación configurable.
- **Gestión de riesgo:** imita la implementación MQL con distancias fijas de stop-loss y take-profit, movimiento automático a break-even y ajustes de trailing stop.

## Funcionamiento

1. Suscribirse a un único marco temporal definido por `CandleType`.
2. Actualizar indicadores con cada vela completada y mantener búferes móviles de máximos y mínimos para detectar nuevos fractales.
3. Construir líneas de tendencia de cuña desde los dos fractales altos y bajos más recientes. Solo las cuñas convergentes (máximos descendentes y mínimos ascendentes) se consideran configuraciones válidas.
4. Se abre un largo cuando:
   - LWMA rápida > LWMA lenta.
   - Línea MACD > línea de señal.
   - Cualquiera de las tres últimas lecturas de momentum supera el umbral configurado.
   - La vela actual cierra por encima de la línea de tendencia superior proyectada al menos por el búfer de ruptura.
5. Un corto refleja las condiciones con líneas y umbrales invertidos.
6. Después de la entrada, la estrategia coloca inmediatamente órdenes de stop-loss y take-profit. Luego puede mover el stop a break-even y trailarlo cuando la posición se vuelve rentable.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Marco usado para análisis y órdenes. |
| `FastMaPeriod` | Longitud del filtro LWMA rápido. |
| `SlowMaPeriod` | Longitud del filtro LWMA lento. |
| `MomentumPeriod` | Periodo de retrospección del indicador momentum (14 por defecto). |
| `MomentumThreshold` | Distancia mínima desde 100 requerida para considerar impulsivo al mercado. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Configuración MACD estándar. |
| `FractalDepth` | Número de barras a cada lado necesario para confirmar un máximo o mínimo fractal. |
| `StopLossPips` | Distancia inicial del stop protector en pips. |
| `TakeProfitPips` | Distancia inicial del objetivo de beneficio en pips. |
| `UseBreakeven`, `BreakevenTriggerPips`, `BreakevenOffsetPips` | Activa y configura la automatización de break-even. |
| `UseTrailing`, `TrailingActivationPips`, `TrailingDistancePips`, `TrailingStepPips` | Activa y configura el comportamiento del trailing-stop. |
| `BreakoutBufferPips` | Búfer extra aplicado a la confirmación de ruptura de la cuña. |

Todos los ajustes basados en pips se convierten a distancias de precio usando el tamaño de tick del instrumento. El cálculo de pips predeterminado considera precios fraccionarios (3 o 5 decimales) exactamente como el asesor experto original.

## Guías de uso

1. Adjunte la estrategia al instrumento deseado y seleccione el marco de velas que coincida con la configuración original (por ejemplo, velas de 15 minutos).
2. Configure el tamaño de posición mediante la propiedad base `Strategy.Volume`.
3. Opcionalmente ajuste filtros y parámetros de riesgo para coincidir con la volatilidad del mercado objetivo.
4. Inicie la estrategia; se suscribirá a velas, dibujará datos de gráfico y operará automáticamente cuando ocurran rupturas de cuña.

## Diferencias frente a la versión MQL

- La versión StockSharp usa `SubscribeCandles` de alto nivel y APIs de binding de indicadores, evitando procesamiento manual de ticks.
- La gestión de trailing stop y break-even depende de `SetStopLoss`/`SetTakeProfit`, integrándose con el comportamiento protector integrado.
- Se mantiene solo una posición a la vez; el script MetaTrader admitía piramidación hasta un número máximo de operaciones.
- Se omiten funciones de alerta, correo y notificación; el manejo de eventos debe implementarse externamente si se necesita.

A pesar de estas adaptaciones, la lógica central de entrada y las reglas protectoras siguen de cerca al experto MetaTrader original usando patrones idiomáticos de StockSharp.
