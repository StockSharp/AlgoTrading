# Kloss MQL/8186 Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Kloss MQL/8186** es una conversión directa del MetaTrader4 asesor experto `Kloss.mq4`. Combina un índice de canal de productos básicos (CCI), un oscilador Stochastic y un filtro de precios típico desplazado para cronometrar las reversiones de una sola posición. La versión StockSharp mantiene los umbrales de entrada originales, las distancias de stop-loss y take-profit y la lógica de volumen (tamaño de lote fijo o tamaño basado en porcentaje) mientras utiliza la suscripción de vela de alto nivel API.

## Lógica de trading

- **Datos**: Velas completadas del período de tiempo configurado (predeterminado 5 minutos). Los indicadores se calculan sobre la misma serie.
- **Indicadores**:
  - CCI con período 10. El valor absoluto se compara con `±CciThreshold` (predeterminado 120).
  - Stochastic oscilador con `%K=5`, `%D=3`, suavizando `=3`. La línea principal `%K` se compara con las bandas de sobreventa/sobrecompra.
  - Precio típico ((Máximo + Mínimo + Cierre) / 3) retrasado por cinco velas completas para replicar la LWMA desplazada del asesor experto.
- **Entrada Larga**:
  - CCI <= `-CciThreshold`.
  - Stochastic %K < `StochasticOversold` (predeterminado 30).
  - Apertura de vela anterior > precio típico de hace cinco velas.
  - No existe ninguna posición larga (`Position <= 0`). Cualquier apertura corta se cierra y se revierte a una posición larga en una única orden de mercado.
- **Entrada corta**:
  - CCI >= `CciThreshold`.
  - Stochastic %K > `StochasticOverbought` (predeterminado 70).
  - Cierre de vela anterior <precio típico de hace cinco velas.
  - No existe ninguna posición corta existente (`Position >= 0`). Cualquier apertura larga se cierra y se revierte a una posición corta con una orden de mercado.
- **Gestión de posición**: StockSharp's `StartProtection` emite órdenes de stop-loss y take-profit automáticamente utilizando las distancias de puntos especificadas. Por lo demás, la estrategia mantiene una única posición en todo momento (plana, larga o corta).

## Dimensionamiento de posiciones

- **Volumen fijo**: si es `FixedVolume > 0`, la estrategia siempre negocia ese volumen exacto (después de alinearse con los `VolumeStep` y `MinVolume` del instrumento).
- **Porcentaje de riesgo**: cuando `FixedVolume = 0`, la estrategia asigna `RiskPercent` (predeterminado 0,2) del valor de la cuenta dividido por el último cierre para estimar el tamaño del pedido. El volumen se fija mediante `MaxVolume` (predeterminado 5) y se redondea al paso del instrumento.
- **Salvaguardias**: el método recurre al volumen mínimo negociable si falta información de la cuenta o el valor calculado no es positivo.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `CciPeriod` | Número de velas utilizadas para calcular el índice del canal de productos básicos. | 10 |
| `CciThreshold` | Nivel absoluto CCI que activa las entradas. | 120 |
| `StochasticKPeriod` | Periodo %K del oscilador Stochastic. | 5 |
| `StochasticDPeriod` | %D período de suavizado. | 3 |
| `StochasticSmooth` | Suavizado adicional aplicado a %K antes de la señal. | 3 |
| `StochasticOversold` | Umbral %K para confirmar entradas largas. | 30 |
| `StochasticOverbought` | Umbral %K para confirmar entradas cortas. | 70 |
| `StopLossPoints` | Distancia en puntos de precio para la parada de protección. | 48 |
| `TakeProfitPoints` | Distancia en puntos de precio para el objetivo de ganancias. | 152 |
| `FixedVolume` | El valor positivo obliga a un volumen comercial fijo. | 0 |
| `RiskPercent` | Fracción de cartera convertida a volumen cuando `FixedVolume` es cero. | 0,2 |
| `MaxVolume` | Volumen comercial máximo permitido. | 5 |
| `CandleType` | Tipo de vela/plazo de tiempo para los cálculos del indicador. | marco de tiempo de 5 minutos |

## Notas de ejecución

- **Posición única**: Solo se mantiene abierta una posición. Las reversiones cierran la posición existente y abren la nueva con una orden de mercado única.
- **Sincronización del indicador**: El cambio de precio utiliza las últimas cinco velas completadas; Se deben procesar al menos seis velas antes de que pueda aparecer la primera operación.
- **Paradas/Objetivos**: `StartProtection` convierte distancias basadas en puntos en compensaciones de precios absolutos utilizando el `PriceStep` del instrumento. Si se desconoce `PriceStep`, se aplica el valor de puntos sin procesar.
- **Requisitos de datos**: Funciona con cualquier instrumento que proporcione OHLC velas; la alineación del volumen respeta `MinVolume` y `VolumeStep` cuando estén disponibles.
- **Diferencias frente a MT4**: los cálculos de margen de MetaTrader se aproximan a través del capital de la cuenta (`Portfolio.CurrentValue`). Cuando los datos sobre acciones no están disponibles, la estrategia vuelve al volumen mínimo negociable.

## Consejos de uso

1. Ajuste `CandleType` a la sesión de mercado utilizada en MetaTrader (M5 en la plantilla original).
2. Revisar las distancias de parada en relación con el tamaño de las marcas; La conversión de punto a precio se produce automáticamente, pero es posible que sea necesario ajustar los valores para instrumentos que no sean Forex.
3. Para tamaños de contrato fijos, establezca `FixedVolume` en el lote deseado y `RiskPercent` en cero.
4. Habilite la optimización de los umbrales de los indicadores al calibrar la estrategia en nuevos símbolos.
