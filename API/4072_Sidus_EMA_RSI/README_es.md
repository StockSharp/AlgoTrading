# Estrategia de Sidus EMA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una versión StockSharp del asesor experto MetaTrader 4 **Exp_Sidus.mq4**. Reproduce la lógica original de que
combina un cruce EMA rápido/lento con un filtro RSI de 50 niveles. Las señales se evalúan únicamente en velas completas y cada vela puede
genera como máximo un orden, coincidiendo con la disciplina de sincronización del robot fuente.

## Lógica de trading

- **Pila de indicadores**
  - Media móvil exponencial rápida (período predeterminado 5)
  - Media móvil exponencial lenta (período predeterminado 12)
  - Índice de fuerza relativa (período predeterminado 21)
- **Configuración alcista**
  1. El EMA rápido estaba por debajo o igual al EMA lento en la vela de señal anterior.
  2. El EMA rápido está por encima del EMA lento en la vela de señal actual.
  3. RSI en la misma vela es estrictamente mayor que 50.
- **Configuración bajista**
  1. El EMA rápido estaba por encima o igual al EMA lento en la vela de señal anterior.
  2. El EMA rápido está por debajo del EMA lento en la vela de señal actual.
  3. RSI en la misma vela es estrictamente menor que 50.
- **Cambio de señal**: el parámetro `SignalShift` (predeterminado `1`) define qué vela cerrada se considera la barra de señal "actual".
Un valor de `1` usa la última vela cerrada, `0` usa la vela recién cerrada, `2` mira dos barras hacia atrás, y así sucesivamente. el anterior
La vela para la detección de cruce se calcula automáticamente como `SignalShift + 1`.
- **Protección duplicada** — la estrategia almacena el tiempo de apertura de la vela de señal y nunca abre otra posición vinculada al
misma barra, imitando fielmente el cheque `LastTime` en el EA original.

## Gestión de Puestos

- Sólo existe una posición en cualquier momento.
- Cuando aparece una señal opuesta mientras una posición está abierta, la estrategia primero cierra la posición existente y luego espera a que
siguiente paso de procesamiento para abrir una operación en la nueva dirección, exactamente como lo hace la versión MQL.
- `StartProtection` adjunta tramos opcionales de obtención de beneficios y límite de pérdidas expresados en puntos de precio (escalones de precio). Las distancias son
derivado de las entradas del EA original: puntos de toma de ganancias predeterminados `80` y puntos de stop-loss `20`.

## Parámetros

| Nombre | Descripción | Predeterminado | Notas |
| ---- | ----------- | ------- | ----- |
| `TakeProfitPoints` | Distancia de obtención de beneficios en pasos de precio. | `80` | Configure `0` para desactivar el objetivo. |
| `StopLossPoints` | Distancia de stop-loss en pasos de precio. | `20` | Configure `0` para desactivar la protección. |
| `TradeVolume` | Volumen de pedidos (lotes/contratos). | `0.1` | Asignado a la propiedad base `Volume` al inicio. |
| `FastPeriod` | Longitud rápida EMA. | `5` | Optimizable. |
| `SlowPeriod` | Longitud lenta de EMA. | `12` | Optimizable. |
| `RsiPeriod` | RSI longitud. | `21` | Optimizable. |
| `SignalShift` | Número de velas cerradas utilizadas para los cálculos de señales. | `1` | Refleja la entrada `shif` del MT4 EA. |
| `CandleType` | Fuente de velas para la suscripción. | `1h` período de tiempo | Se puede configurar en cualquier `DataType` admitido por el entorno. |

## Notas de implementación

- Los datos de la vela se suscriben a través de `SubscribeCandles(CandleType)` y se procesan dentro de `ProcessCandle` solo después de que la vela alcanza
el estado `Finished`.
- Los valores del indicador se almacenan en caché en una cola corta para que la estrategia pueda acceder a las barras actuales y anteriores especificadas por
`SignalShift` sin llamar a métodos de indicador como `GetValue`, cumpliendo con las pautas del repositorio.
- La ejecución comercial utiliza `BuyMarket`/`SellMarket` una vez que la estrategia es plana; cuando existe una posición en la dirección opuesta,
`ClosePosition` se emite primero, manteniendo el flujo de pedidos idéntico al del robot original.
- Todos los registros de ejecución están escritos en inglés para mantener un registro de auditoría claro.

## Notas de conversión

- Las distancias de take-profit y stop-loss multiplican el instrumento `PriceStep`, replicando el comportamiento de MetaTrader `Point`.
- El volumen predeterminado es `0.1`, igual que la entrada `Lots` en la fuente MQL.
- Los umbrales RSI están codificados en 50 para reflejar la implementación original.
