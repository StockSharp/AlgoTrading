# Estrategia Brandy v1.2 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Brandy v1.2** es una conversión directa del MetaTrader 4 asesor experto "Brandy_v1_2.mq4" al marco de estrategia de alto nivel StockSharp. El sistema evalúa un par de promedios móviles simples (SMA) desplazados calculados sobre el precio de cierre de la serie de velas configuradas. Las nuevas posiciones se abren solo cuando las SMA a largo y corto plazo muestran un impulso sincronizado en la misma dirección, mientras que las operaciones existentes se gestionan mediante inversiones de pendiente, niveles fijos de stop-loss y un módulo de trailing stop opcional.

El script original MQL se ejecutó exactamente una vez por barra completada. Este puerto procesa StockSharp velas terminadas de la misma manera, lo que garantiza que todas las decisiones comerciales se basen en datos cerrados sin depender de barras parcialmente formadas.

## Lógica de trading
1. **Preparación de indicadores**
   - Se calculan dos SMA: una línea de base más larga (`LongPeriod`) y una línea de confirmación más corta (`ShortPeriod`).
   - A cada promedio se accede dos veces: el valor de la barra anterior (shift = 1) y otro valor desplazado por `LongShift`/`ShortShift` barras respectivamente. Esto reproduce las llamadas `iMA(..., shift)` presentes en el EA original.
2. **Reglas de entrada**
   - **Compre** cuando el valor de la barra anterior de ambas SMA sea mayor que sus contrapartes desplazadas (ambas pendientes apuntando hacia arriba) y no haya ninguna posición abierta.
   - **Vender** cuando el valor de la barra anterior de ambas SMA sea menor que sus contrapartes desplazadas (ambas pendientes apuntando hacia abajo) y no haya ninguna posición abierta.
   - Solo puede haber una posición activa en cualquier momento, reflejando la verificación `k == 0` en la fuente MQL.
3. **Reglas de salida**
   - **Inversión de pendiente**: una posición larga abierta se liquida si el SMA largo baja (`longPrev < longShifted`), mientras que una posición corta se cubre cuando el SMA largo sube (`longPrev > longShifted`).
   - **Stop-loss fijo**: al entrar, la estrategia almacena un nivel de stop inicial compensado en `StopLossPoints × PriceStep` del precio de entrada. El stop se compara con el rango alto/bajo de la vela, aproximando la gestión del nivel de tick del asesor original.
   - **Trailing stop**: si `TrailingStopPoints ≥ 100`, la estrategia replica la lógica de seguimiento (parámetro `ts`). Una vez que el beneficio flotante excede la distancia de seguimiento, el stop se lleva a `currentPrice ± trailingDistance`, siempre que el nuevo nivel esté más cerca del precio que el stop existente. Este comportamiento coincide con las llamadas `OrderModify` en el experto MQL.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `LongPeriod` | 70 | Longitud del SMA principal (`p1` en MQL). Debe ser > 0. |
| `LongShift` | 5 | Desplazamiento hacia atrás aplicado a la comparación larga SMA (`s1`). Puede ser cero. |
| `ShortPeriod` | 20 | Duración de la confirmación SMA (`p2`). Debe ser > 0. |
| `ShortShift` | 5 | Desplazamiento hacia atrás para el corto SMA (`s2`). Puede ser cero. |
| `StopLossPoints` | 50 | Distancia de parada fija en pasos de precio (`sl`). Establezca en 0 para desactivar la parada brusca. |
| `TrailingStopPoints` | 150 | Distancia de seguimiento en pasos de precio (`ts`). El seguimiento se activa solo cuando el valor es ≥ 100, reflejando el umbral original. |
| `Volume` | 0.1 | Volumen de pedido utilizado para las entradas (`lots`). |
| `CandleType` | plazo de 15 minutos | Serie de velas procesadas por la estrategia (configurable por el usuario). |

### Dependencia del paso del precio
Ambos parámetros de parada operan en puntos del instrumento. El método auxiliar los convierte en deltas de precios absolutos mediante `Security.PriceStep`. Si la fuente de datos no proporciona `PriceStep`, la estrategia vuelve a recurrir a `0.0001` por lo que la lógica sigue funcionando, aunque con una conversión aproximada. Verifique siempre los metadatos del símbolo en StockSharp antes de su uso en vivo.

## Gestión del riesgo
- **Parada completa**: almacenado internamente y validado con cada vela terminada. Cuando el precio viola el tope, la llamada `SellMarket`/`BuyMarket` correspondiente cierra toda la posición.
- **Trailing stop**: sigue las condiciones exactas del EA original, moviendo el stop solo cuando la ganancia actual excede la distancia de seguimiento *y* el stop existente aún está más lejos que esa distancia.
- **Posición única**: el algoritmo nunca hace pirámides; tiene una única posición larga, una única posición corta o es plano.

## Notas de implementación
- El estado (precio de entrada, nivel de parada, SMA historiales) se restablece automáticamente en `OnReseted()`, lo que garantiza pruebas retrospectivas y reinicios limpios.
- Los historiales de los indicadores se almacenan en buffers cortos para reproducir las compensaciones `iMA(..., shift)` sin llamar a `GetValue()`.
- Todos los comentarios en línea permanecen en inglés según lo exigen las pautas del repositorio.
- No se proporciona ninguna contraparte de Python. Solo la implementación de alto nivel de C# se entrega en `CS/BrandyV12Strategy.cs` según lo solicitado.

## Uso
1. Coloque la estrategia en una solución StockSharp, seleccione el instrumento deseado y asegúrese de que los datos de la vela coincidan con el período de tiempo especificado por `CandleType`.
2. Configure los parámetros en la interfaz de usuario o mediante código. Los valores predeterminados replican los valores originales de MT4.
3. Inicia la estrategia. Se suscribirá a la serie de velas, dibujará ambas SMA en el gráfico y gestionará las operaciones automáticamente.

> **Descargo de responsabilidad:** Este port está destinado a fines educativos y de prueba. Valide siempre el comportamiento en sesiones de negociación históricas y en papel antes de implementarlo en mercados reales.
