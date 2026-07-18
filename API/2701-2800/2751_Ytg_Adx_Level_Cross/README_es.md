# Estrategia Ytg ADX Cruce de Nivel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto `_ADX.mq5` de Yuriy Tokman a la API de alto nivel de StockSharp. Monitorea el Average Directional Index y reacciona cuando los componentes +DI o -DI superan umbrales configurables. Las órdenes se abren solo de una en una, reflejando la lógica MQL original, y los niveles de stop-loss y take-profit de protección expresados en puntos de precio se aplican automáticamente.

## Descripción general

- **Régimen de mercado**: Funciona en movimientos tendenciales o fuertemente direccionales donde los picos de DI acompañan rupturas.
- **Dirección**: Abre posiciones largas o cortas, pero nunca ambas simultáneamente.
- **Marco temporal**: Controlado por el parámetro `CandleType` (por defecto velas de 1 hora).
- **Datos**: Usa velas finalizadas para calcular los valores ADX/DI del indicador `AverageDirectionalIndex`.

## Lógica de trading

1. Suscribirse a la serie de velas seleccionada y construir el indicador ADX con el `AdxPeriod` configurado.
2. Para cada vela finalizada, recopilar los valores +DI y -DI y mantener solo la cantidad de historial requerida por el parámetro `Shift`. Un `Shift` de 1, idéntico al predeterminado de MQL, evalúa la vela cerrada anterior.
3. **Entrada larga**: Activada cuando el valor +DI desplazado sube por encima de `LevelPlus` mientras su valor anterior estaba por debajo del mismo umbral. La estrategia verifica que no haya posición abierta actualmente antes de comprar a mercado.
4. **Entrada corta**: Activada cuando el valor -DI desplazado sube por encima de `LevelMinus` mientras su valor anterior estaba por debajo de ese nivel. Se envía una venta a mercado solo si no hay posición activa.
5. Las salidas son manejadas exclusivamente por órdenes de protección iniciadas a través de `StartProtection`: un take-profit y stop-loss fijos medidos en puntos de precio, equivalentes a `TP` y `SL` del código original.

Esta implementación evita intencionalmente el promediado en posiciones, reentradas mientras las operaciones están activas, o filtros adicionales, coincidiendo con el comportamiento ligero del EA fuente.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `CandleType` | Marco temporal de 1 hora | Marco temporal de la suscripción de velas usada para el cálculo de ADX. |
| `AdxPeriod` | 28 | Longitud del Average Directional Index y sus cálculos de DI. |
| `LevelPlus` | 5 | Umbral que la serie +DI debe superar para abrir una posición larga. |
| `LevelMinus` | 5 | Umbral que la serie -DI debe superar para abrir una posición corta. |
| `Shift` | 1 | Número de velas cerradas a mirar atrás al evaluar el cruce de DI (1 = vela anterior). |
| `TakeProfitPoints` | 500 | Distancia en puntos de precio para la orden de take-profit. Multiplicada internamente por el tamaño de tick del instrumento. |
| `StopLossPoints` | 500 | Distancia en puntos de precio para la orden de stop-loss de protección. |
| `TradeVolume` | 0.1 | Volumen base para nuevas órdenes de mercado, coincidiendo con el ajuste `Lots` en el experto MQL. |

## Gestión de riesgos

- `StartProtection` convierte los valores de take-profit y stop-loss basados en puntos en distancias de precio absolutas usando el `PriceStep` del instrumento.
- No se aplica trailing stop ni lógica de punto de equilibrio; las salidas ocurren únicamente a través de las órdenes de protección configuradas.

## Notas y consejos

- Los umbrales de DI extremadamente bajos pueden llevar a operaciones de vaivén frecuentes, mientras que niveles más altos esperan impulsos direccionales más fuertes.
- El parámetro `Shift` puede aumentarse cuando se necesita confirmación de velas anteriores, por ejemplo en marcos temporales más altos para filtrar el ruido intrábarra.
- Debido a que la estrategia opera solo una posición a la vez, se deben evitar las interferencias manuales o las operaciones externas en la misma cuenta para prevenir conflictos con el seguimiento interno de posición.
