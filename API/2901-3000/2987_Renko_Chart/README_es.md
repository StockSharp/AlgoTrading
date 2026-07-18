# Estrategia Renko Chart
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
RenkoChartStrategy es una conversión directa del asesor experto original **RenkoChart.mq5**. En lugar de colocar órdenes, la estrategia se enfoca en recrear el flujo de trabajo del símbolo Renko personalizado dentro de StockSharp. Se suscribe a datos de tick, produce un flujo de velas Renko con un tamaño de ladrillo configurable y lo expone a través de la plataforma para que pueda visualizarse o reenviarse a otros componentes. Cada ladrillo completado se registra con el último tick que lo activó, permitiendo al operador validar la serie generada contra la implementación MQL.

## Mapeo desde el asesor experto MQL
- **StartDateTime** → `StartTime`: la marca de tiempo inicial utilizada al sembrar el historial Renko.
- **BaseSymbol** → `Strategy.Security`: StockSharp ya asigna el instrumento base, por lo que el parámetro fue reemplazado confiando en el instrumento seleccionado. La estrategia sigue prefijando el nombre del flujo generado con `RenkoPrefix` para imitar la convención de nomenclatura "Renko-\<symbol\>".
- **Mode (Bid/Last)** → `UseBidTicks`: alterna si las actualizaciones de oferta o los ticks de operación impulsan el feed de monitoreo en vivo.
- **Range** → `BrickSizeSteps`: número de pasos de precio que forman un ladrillo Renko. La estrategia multiplica el valor por el `PriceStep` del instrumento para obtener el tamaño absoluto de la caja.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `StartTime` | `DateTimeOffset` | 2018‑08‑01 09:00:00 UTC | Los ladrillos con un tiempo de apertura antes de este momento se ignoran, coincidiendo con el comportamiento de precalentamiento original. |
| `BrickSizeSteps` | `int` | 5 | Tamaño del ladrillo Renko expresado en pasos de precio. Se convierte a precio absoluto cuando se crea la serie Renko. |
| `UseBidTicks` | `bool` | `false` | Cuando es `false` la estrategia escucha ticks de operación, cuando es `true` escucha actualizaciones de oferta para emular el modo MQL `Bid`. |
| `RenkoPrefix` | `string` | `"Renko-"` | Prefijo añadido a los mensajes de registro para que el nombre del flujo coincida con la convención de nomenclatura de símbolos personalizados. |

> **Nota:** la propiedad calculada `BrickSize` expone el tamaño absoluto de la caja y puede ser útil cuando se conecta la estrategia con otros componentes que esperan un delta de precio en lugar de conteos de pasos.

## Flujo de datos
1. `GetWorkingSecurities` configura una suscripción de velas Renko usando `RenkoBuildFrom.Points` y el tamaño de caja calculado.
2. `OnStarted` lanza la suscripción Renko, se suscribe a ticks de operación o de oferta (dependiendo de `UseBidTicks`), y dibuja el flujo Renko en el gráfico si hay uno disponible.
3. `ProcessTrade` / `ProcessLevel1` almacenan el precio y la marca de tiempo del tick más reciente para fines de registro.
4. `ProcessCandle` ignora ladrillos no terminados, filtra datos previos a `StartTime` y registra cada ladrillo completado con los niveles de cierre anterior y nuevo junto con la información del último tick.

## Consejos de uso
- Adjunta la estrategia a cualquier instrumento que proporcione operaciones o actualizaciones de nivel 1. El flujo Renko aparecerá en el área de gráfico estándar con el prefijo configurado.
- Debido a que la implementación no envía órdenes, se puede ejecutar en paralelo con otras estrategias de trading para proporcionar una vista Renko sincronizada del mercado.
- Las entradas de registro contienen tanto la dirección del ladrillo como el tick activador. Esto es útil al comparar la salida con datos históricos exportados desde MetaTrader.

## Diferencias respecto a la versión MQL
- StockSharp ya gestiona símbolos, por lo que la creación explícita de símbolos personalizados fue reemplazada por salida de registro y gráfico.
- Todos los cálculos usan aritmética decimal en lugar de arreglos, confiando en el constructor de velas Renko integrado.
- La estrategia adopta el modelo de suscripción y el auxiliar de protección de StockSharp, preparándola para ser extendida con lógica de trading si es necesario.
