# Estrategia Crypto SR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Crypto SR porta el asesor experto de MetaTrader 4 "Crypto S&R" a la API de alto nivel de StockSharp. La implementación conserva la lógica de confirmación por capas del sistema original: un filtro de tendencia basado en medias móviles ponderadas lineales (LWMA), una comprobación de impulso en un marco temporal superior, un filtro de tendencia MACD de largo plazo y niveles de soporte/resistencia derivados de fractales. Las órdenes se envían con ejecución de mercado y la posición se gestiona mediante stop-loss/take-profit fijos, ajustes a break-even y un trailing stop medido en pips.

## Lógica de negociación

1. **Análisis del marco principal:** la estrategia se suscribe a la serie de velas configurada y alimenta dos LWMA con el precio típico de la vela `(high + low + close) / 3`. La LWMA rápida debe permanecer por encima (por debajo) de la lenta para habilitar largos (cortos).
2. **Impulso en marco superior:** un indicador `Momentum` se evalúa en una segunda serie de velas. La distancia absoluta de las tres últimas lecturas frente al valor neutral (100) debe superar los umbrales de compra/venta.
3. **Filtro MACD de largo plazo:** la estrategia escucha otra serie de velas donde se calcula un MACD (12, 26, 9). Las posiciones largas requieren que la línea MACD permanezca por encima de su señal; las cortas, por debajo. El marco predeterminado de largo plazo es diario para aproximar la serie mensual usada por el EA; puede ajustarse si hay velas mensuales reales disponibles.
4. **Soporte/resistencia fractal:** las velas terminadas se guardan en un búfer móvil. Cuando aparece el patrón fractal clásico de Bill Williams (dos vecinos a cada lado), el máximo/mínimo correspondiente se convierte en la resistencia o soporte activo. Se aplica un búfer configurable en pips alrededor del nivel para emular las líneas horizontales del experto original.
5. **Reglas de entrada**:
   - *Compra*: sin posición larga abierta, LWMA rápida sobre la lenta, desviación de momentum >= umbral de compra, MACD alcista, la vela actual prueba el soporte con búfer y cierra por encima del cierre anterior.
   - *Venta*: condiciones espejo con la resistencia, el umbral de venta de momentum y confirmación MACD bajista.
6. **Gestión de riesgo:** cada nueva posición recibe un stop-loss y take-profit iniciales en pips. La lógica de break-even puede desplazar el stop al alcanzar la distancia de activación, mientras que un trailing stop opcional sigue al precio usando máximos/mínimos de velas. La exposición larga/corta se cierra si el filtro MACD gira contra la operación.

## Notas de implementación

- El filtro MACD mensual de la versión MetaTrader se aproxima por defecto con una serie diaria porque StockSharp no proporciona velas de mes calendario de forma inmediata. Los usuarios pueden cambiar a un agregador mensual personalizado si su fuente de datos lo admite.
- Las órdenes se cierran con solicitudes de mercado cuando se vulneran los niveles de protección. Esto replica las llamadas `OrderClose` de MQL y evita depender de órdenes stop del lado de la bolsa.
- Todos los enlaces de indicadores se realizan mediante la API de suscripción de alto nivel y no se requieren llamadas directas a `GetValue`.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `FastMaPeriod` | Longitud de la LWMA rápida en el marco principal. | `6` |
| `SlowMaPeriod` | Longitud de la LWMA lenta en el marco principal. | `85` |
| `MomentumPeriod` | Periodo de momentum en el marco superior. | `14` |
| `MomentumBuyThreshold` | Desviación absoluta mínima de momentum frente a 100 para habilitar entradas largas. | `0.3` |
| `MomentumSellThreshold` | Desviación absoluta mínima de momentum frente a 100 para habilitar entradas cortas. | `0.3` |
| `MacdFastPeriod` | Longitud de la EMA rápida para el filtro MACD de largo plazo. | `12` |
| `MacdSlowPeriod` | Longitud de la EMA lenta para el filtro MACD de largo plazo. | `26` |
| `MacdSignalPeriod` | Longitud de la EMA de señal para el filtro MACD de largo plazo. | `9` |
| `StopLossPips` | Distancia de stop-loss duro expresada en pips. | `20` |
| `TakeProfitPips` | Distancia fija de take-profit expresada en pips. | `50` |
| `TrailingStopPips` | Distancia del trailing stop en pips (0 desactiva el trailing). | `40` |
| `UseBreakEven` | Indica si se mueve el stop a break-even tras una activación de beneficio. | `true` |
| `BreakEvenTriggerPips` | Beneficio en pips necesario antes de aplicar ajustes de break-even. | `30` |
| `BreakEvenOffsetPips` | Desplazamiento añadido al mover el stop a break-even. | `30` |
| `FractalWindowLength` | Número de velas terminadas retenidas para confirmar máximos y mínimos fractales. | `7` |
| `FractalBufferPips` | Búfer adicional alrededor de niveles fractales en pips. | `10` |
| `TradeVolume` | Volumen enviado con cada orden de mercado. | `1` |
| `CandleType` | Serie principal de velas para LWMA y lógica fractal. | Marco temporal `15m` |
| `HigherCandleType` | Marco superior para el filtro de momentum. | Marco temporal `1h` |
| `LongTermCandleType` | Marco temporal para el filtro de tendencia MACD. | Marco temporal `1d` |
