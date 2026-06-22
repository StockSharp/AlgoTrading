# Estrategia Experto RSI Stochastic MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Experto RSI Stochastic MA** es una conversión del asesor experto de MetaTrader 5 `Expert_RSI_Stochastic_MA.mq5`. La implementación en C# aprovecha la API de estrategias de alto nivel de StockSharp mientras reproduce la lógica original: un filtro de tendencia basado en una media móvil configurable, confirmación de momentum del RSI, y un oscilador Stochastic de doble línea para un timing preciso. El comportamiento protector replica el algoritmo fuente con un umbral de pérdida fijo opcional y una salida trailing impulsada por Stochastic.

## Indicadores y Parámetros
La estrategia expone las mismas entradas que la versión MetaTrader y mantiene sus valores por defecto. Todos los parámetros están disponibles para optimización a través de la UI de StockSharp.

| Categoría | Parámetro | Por defecto | Descripción |
| --- | --- | --- | --- |
| General | `CandleType` | Marco temporal de 15 minutos | Agregación de velas usada para los cálculos de indicadores. |
| Trading | `TradeVolume` | `0.01` | Tamaño base de la orden en lotes/contratos. |
| RSI | `RsiPeriod` | `3` | Número de barras usadas para calcular el RSI. |
| RSI | `RsiPriceType` | Cierre | Precio aplicado para RSI (cierre, apertura, alto, bajo, mediano, típico, ponderado). |
| RSI | `RsiUpperLevel` | `80` | Umbral de sobrecompra que activa las condiciones cortas. |
| RSI | `RsiLowerLevel` | `20` | Umbral de sobreventa que activa las condiciones largas. |
| Stochastic | `StochKPeriod` | `6` | Período de la línea %K. |
| Stochastic | `StochDPeriod` | `3` | Período de la línea de suavizado %D. |
| Stochastic | `StochSlowing` | `3` | Factor de desaceleración adicional aplicado a %K. |
| Stochastic | `StochUpperLevel` | `70` | Nivel de sobrecompra compartido por ambas líneas Stochastic. |
| Stochastic | `StochLowerLevel` | `30` | Nivel de sobreventa compartido por ambas líneas Stochastic. |
| Media Móvil | `MaMethod` | Simple | Tipo de media móvil (simple, exponencial, suavizada, ponderada). |
| Media Móvil | `MaPriceType` | Cierre | Precio aplicado para la media móvil. |
| Media Móvil | `MaPeriod` | `150` | Longitud de la media móvil. |
| Media Móvil | `MaShift` | `0` | Número de barras completadas usadas para desplazar el valor de la media móvil hacia atrás. |
| Riesgo | `AllowLossPoints` | `30` | Máxima excursión adversa en puntos antes de salir de una operación perdedora (0 deshabilita). |
| Riesgo | `TrailingStopPoints` | `30` | Distancia en puntos para el stop trailing basado en Stochastic (0 cierra en Stochastic sin trailing). |

> **Cálculo de puntos** – La implementación convierte los parámetros `AllowLoss` y `TrailingStop` en precios absolutos usando `Security.PriceStep`. Cuando el instrumento tiene 3 o 5 decimales, el valor se multiplica por 10 para emular el manejo de pips de MetaTrader.

## Lógica de Trading
### Configuración Largo
1. **Filtro de tendencia** – El cierre de la vela debe mantenerse por encima de la media móvil desplazada.
2. **Confirmación de momentum** – RSI debe estar por debajo de `RsiLowerLevel`.
3. **Timing** – Ambas líneas Stochastic (%K y %D) deben estar por debajo de `StochLowerLevel`.
4. **Filtro de posición** – Las órdenes largas solo se colocan cuando no existe exposición larga (`Position <= 0`). El tamaño de la orden es `TradeVolume` más cualquier cantidad requerida para cerrar una posición corta existente.

### Configuración Corto
1. **Filtro de tendencia** – El cierre de la vela debe estar por debajo de la media móvil desplazada.
2. **Confirmación de momentum** – RSI debe superar `RsiUpperLevel`.
3. **Timing** – Ambas líneas Stochastic deben estar por encima de `StochUpperLevel`.
4. **Filtro de posición** – Las nuevas posiciones cortas requieren `Position >= 0`. La estrategia compensa los largos existentes automáticamente si es necesario.

### Gestión de Salidas
- **Operaciones perdedoras**
  - Cuando `AllowLossPoints` es cero, la estrategia espera a que la línea principal del Stochastic se mueva hacia el extremo opuesto (`StochUpperLevel` para largos, `StochLowerLevel` para cortos) antes de cerrar operaciones negativas.
  - Cuando `AllowLossPoints` es positivo, la estrategia convierte el valor en un desplazamiento de precio y cierra la operación tan pronto como la pérdida supere este umbral *y* el Stochastic vuelva dentro de la zona neutral (`stochMain > StochLowerLevel` para largos, `< StochUpperLevel` para cortos).
- **Salida trailing**
  - Con `TrailingStopPoints > 0`, una vez que una operación es rentable y el Stochastic alcanza su zona extrema, se establece un stop trailing en cada vela finalizada. Para operaciones largas el stop sigue por debajo del precio; para cortas, sigue por encima.
  - Con `TrailingStopPoints = 0`, las operaciones rentables se cierran inmediatamente cuando el Stochastic alcanza el nivel extremo (igualando el comportamiento del EA original).
- **Disparador de trailing** – Las actualizaciones de trailing solo ocurren en velas completadas, reflejando la implementación MQL que restringía las actualizaciones a una por barra.

## Notas de Implementación
- El desplazamiento de la media móvil se maneja almacenando valores recientes y leyendo el valor `MaShift` barras atrás, reproduciendo el parámetro `shift` de MetaTrader.
- Las entradas de RSI y media móvil soportan múltiples precios aplicados para coincidir con las opciones de MetaTrader. Los cálculos del Stochastic dependen del oscilador incorporado de StockSharp (modo Low/High) y respetan las longitudes de suavizado configuradas.
- Los umbrales de trailing y pérdida se miden en *puntos*. El ayudante escala automáticamente el valor para tamaños de tick típicos de FX (3 o 5 decimales) y por defecto usa un `PriceStep` en caso contrario.
- La salida del gráfico incluye velas, la media móvil, RSI e indicadores Stochastic, permitiendo validación visual similar a la plantilla original.
- No hay versión Python adjunta por solicitud; solo se proporciona la implementación en C#.

## Consejos de Uso
- Al desplegar en valores con tamaños de tick no convencionales, verifique que `Security.PriceStep` esté completado; de lo contrario se usará la conversión predeterminada (1 punto = 1 unidad de precio).
- Combine el `StartProtection` incorporado o módulos de riesgo adicionales si se requiere mayor gestión de stop-loss o take-profit.
- Optimice las longitudes de los indicadores y los umbrales de riesgo juntos — la estrategia expone intencionalmente todos los controles primarios del experto MetaTrader.
