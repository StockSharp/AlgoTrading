# ABE BE Stochastic Estrategia envolvente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia traslada el Expert Advisor de MetaTrader **Expert_ABE_BE_Stoch** al API de alto nivel de StockSharp. Combina el análisis de velas japonesas con la confirmación del impulso para cronometrar las reversiones alrededor de las zonas de sobreventa y sobrecompra. La señal principal busca una vela envolvente alcista respaldada por un oscilador estocástico profundamente sobrevendido, o una vela envolvente bajista confirmada por una lectura de oscilador de sobrecompra. Una vez abierta una posición, la estrategia se basa en cruces de umbrales estocásticos para gestionar las salidas, replicando la mecánica de "voto" del experto original.

La táctica está diseñada para una participación tanto larga como corta. Evalúa sólo velas completas y, por lo tanto, permanece inmune al ruido intrabar. El tamaño de las operaciones permanece bajo el control de la propiedad `Volume` del marco, mientras que las protecciones opcionales de limitación de pérdidas y toma de ganancias convierten la configuración de riesgo original basada en puntos en objetos StockSharp `Unit`.

## como funciona

1. **Suscripción de datos**: la estrategia se suscribe al tipo de vela configurado y crea un `StochasticOscillator` con tres parámetros ajustables (`%K`, `%D` y el factor de desaceleración).
2. **Detección de patrones**: en cada vela terminada, el algoritmo comprueba si la última barra envuelve el cuerpo de la anterior. Dos métodos auxiliares reproducen las definiciones envolventes alcistas y bajistas utilizadas en MetaTrader.
3. **Confirmación de impulso**: la línea `%D` del estocástico sirve como filtro de confirmación. Se requieren valores por debajo del umbral de sobreventa (predeterminado 30) para operaciones envolventes alcistas, mientras que valores por encima del umbral de sobrecompra (predeterminado 70) se requieren para señales bajistas.
4. **Gestión de posición**: el valor `%D` anterior se almacena en caché. Si la nueva lectura cruza hacia arriba a través de 20 u 80, cualquier exposición corta se cierra. Por el contrario, los cruces a la baja a través de 80 o 20 liquidan la exposición larga. Estos umbrales reflejan los votos "cerrados" adicionales producidos por la lógica MQL.
5. **Manejo de riesgos**: cuando se proporcionan distancias positivas de stop-loss o take-profit (expresadas en incrementos de precio), la estrategia las convierte a `UnitTypes.Price` y habilita `StartProtection`. De lo contrario, la protección StockSharp predeterminada se activa con `StartProtection()`.

## Reglas comerciales

- **Entrada larga**: la vela anterior es bajista, la vela actual es alcista y el cuerpo de la vela actual envuelve al cuerpo anterior. El valor estocástico `%D` debe estar por debajo del `EntryOversoldLevel` (predeterminado 30). Cualquier corto existente se cierra y se abre un nuevo largo a través de `BuyMarket`.
- **Entrada breve**: La vela anterior es alcista, la vela actual es bajista y el cuerpo de la vela actual envuelve al cuerpo anterior. El valor estocástico `%D` debe exceder el `EntryOverboughtLevel` (predeterminado 70). Cualquier posición larga existente se cierra y se abre una nueva posición corta a través de `SellMarket`.
- **Salida larga**: Con una apertura larga, si `%D` cruza hacia abajo a través de `ExitUpperLevel` (80 predeterminado) o `ExitLowerLevel` (20 predeterminado), la posición se cierra con `SellMarket`.
- **Salida corta**: Con una apertura corta, si `%D` cruza hacia arriba a través de `ExitLowerLevel` o `ExitUpperLevel`, la posición se cubre usando `BuyMarket`.
- **Paradas/objetivos**: `StopLossPoints` y `TakeProfitPoints` opcionales convierten distancias basadas en puntos en compensaciones de precios absolutos cuando el instrumento expone un valor distinto de cero `PriceStep`.

## Parámetros

| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Fuente de vela utilizada para la detección de patrones. |
| `StochasticPeriodK` | `int` | `47` | Período retrospectivo para el cálculo rápido `%K`. |
| `StochasticPeriodD` | `int` | `9` | Período de suavizado para la línea de señal `%D`. |
| `StochasticPeriodSlow` | `int` | `13` | Se aplicó un suavizado adicional a `%K` antes de que se convierta en `%D`. |
| `EntryOversoldLevel` | `decimal` | `30` | Límite superior de `%D` que permite operaciones envolventes alcistas. |
| `EntryOverboughtLevel` | `decimal` | `70` | Límite inferior de `%D` que permite operaciones envolventes bajistas. |
| `ExitLowerLevel` | `decimal` | `20` | Nivel que, al cruzarse hacia arriba, obliga a salidas cortas; cuando se cruza hacia abajo, cierra largos. |
| `ExitUpperLevel` | `decimal` | `80` | El límite superior se utiliza de la misma manera que el nivel inferior pero para territorio de sobrecompra. |
| `TakeProfitPoints` | `decimal` | `0` | Distancia en pasos de precio para la orden de toma de ganancias (0 la desactiva). |
| `StopLossPoints` | `decimal` | `0` | Distancia en pasos de precio para la orden stop-loss (0 la desactiva). |

## Notas

- Funciona con cualquier instrumento que suministre OHLC velas; los valores predeterminados asumen barras horarias.
- Todos los cálculos se basan en velas cerradas para mantenerse alineados con la lógica de marco temporal del experto MQL.
- El tamaño de la posición debe configurarse a través de la estrategia base `Volume` propiedad o gestión de cartera de nivel superior.
