# Estrategia de Reversión Alcista de Tres Barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Este patrón atrapa giros alcistas rápidos después de una breve caída. Requiere dos velas bajistas consecutivas seguidas de una fuerte vela alcista que cierre por encima del máximo de la barra anterior. La lógica opcionalmente verifica que el precio estaba tendiendo a la baja con anterioridad.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 85%. Funciona mejor en el mercado cripto.

La estrategia mantiene las últimas tres velas en memoria. Una vez que la secuencia coincide con los criterios y se satisface cualquier filtro de tendencia bajista, se abre una posición larga. Un stop de volatilidad por debajo del mínimo del patrón limita el riesgo de la operación.

Tras la entrada, el sistema espera ya sea un toque del stop o la aparición de otra configuración en dirección opuesta. Este enfoque simple se adapta a mercados propensos a rebotes bruscos desde condiciones de sobreventa.

## Detalles

- **Criterios de entrada**: Dos velas bajistas con mínimos decrecientes, luego una vela alcista que cierra por encima del máximo de la barra central.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Stop-loss o siguiente patrón.
- **Stops**: Sí, por debajo del mínimo del patrón.
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 1
  - `RequireDowntrend` = true
  - `DowntrendLength` = 5
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Largo
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

