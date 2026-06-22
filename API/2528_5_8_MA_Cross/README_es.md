# Estrategia de Cruce de MA 5/8
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de Cruce de MA 5/8 es un port de StockSharp del asesor experto de MetaTrader "5_8 MACross". Compara una media móvil exponencial (EMA) rápida calculada sobre precios de cierre con una EMA más lenta calculada sobre precios de apertura. El sistema actúa sobre el cruce entre las dos medias y se puede aplicar a cualquier símbolo y marco temporal que proporcione velas estándar basadas en tiempo.

## Indicadores
- **EMA rápida** – longitud configurable (por defecto 5) calculada desde el precio de cierre de la vela.
- **EMA lenta** – longitud configurable (por defecto 8) calculada desde el precio de apertura de la vela.

## Lógica de trading
1. La estrategia procesa solo velas terminadas para evitar datos parciales.
2. Una entrada larga se genera cuando la EMA rápida estaba por debajo o igual a la EMA lenta en la vela anterior y la cruza por encima en la vela actual.
3. Una entrada corta se genera cuando la EMA rápida estaba por encima o igual a la EMA lenta en la vela anterior y la cruza por debajo en la vela actual.
4. Cuando aparece una señal, la estrategia revierte su exposición: cierra cualquier posición abierta y envía una orden de mercado dimensionada para terminar con contratos `Volume` en la nueva dirección.

## Gestión de riesgo
- **Take profit** – objetivo opcional expresado en puntos de precio. El tamaño del punto se deriva del paso de precio del instrumento; para cotizaciones de tres y cinco dígitos el valor se multiplica automáticamente por 10 para emular el manejo de pips de MetaTrader.
- **Stop loss** – stop protector opcional, también expresado en puntos de precio desde el precio de entrada.
- **Trailing stop** – distancia opcional en puntos de precio. Después de que se abre una posición, la estrategia rastrea el máximo más alto (para largos) o el mínimo más bajo (para cortos) y mueve el stop solo en la dirección rentable. Si no se especifica un stop loss inicial, el trailing stop igualmente iniciará protección inmediatamente después de la entrada.
- Si el take profit o el (trailing) stop se alcanza en un precio de cierre, la posición se cierra a mercado.

## Parámetros
| Nombre | Descripción | Valor predeterminado |
| --- | --- | --- |
| `FastLength` | Período de la EMA rápida (basada en cierre). | 5 |
| `SlowLength` | Período de la EMA lenta (basada en apertura). | 8 |
| `TakeProfitPoints` | Distancia del take profit en puntos de precio. | 40 |
| `StopLossPoints` | Distancia del stop loss en puntos de precio (0 deshabilita el stop). | 0 |
| `TrailingStopPoints` | Distancia del trailing stop en puntos de precio (0 deshabilita el trailing). | 0 |
| `CandleType` | Tipo/marco temporal de vela usado para los cálculos. | Marco temporal de 1 minuto |
| `Volume` | Volumen de orden heredado de la clase base `Strategy`. | 0.1 |

## Diferencias comparadas con la versión MQL
- Las comprobaciones de cobertura específicas de MetaTrader y las llamadas de información de cuenta se omiten porque StockSharp gestiona la contabilidad de posiciones de manera diferente.
- Las señales se evalúan en velas cerradas en lugar del primer tick de una nueva barra; esto mejora la estabilidad en entornos orientados a eventos.
- La lógica de trailing usa el máximo/mínimo de la vela para avanzar el stop en lugar del tick actual de bid/ask, proporcionando un comportamiento determinista para el procesamiento histórico.

## Notas de uso
- Configurar `Volume` en las propiedades de la estrategia para que coincida con el tamaño de lote deseado.
- Combinar la estrategia con los módulos de protección de StockSharp o filtros adicionales si se requiere gestión de riesgo a nivel de cartera.
- La estrategia no coloca órdenes pendientes; todas las entradas y salidas se ejecutan con órdenes de mercado generadas por la lógica de cruce y riesgo anterior.
