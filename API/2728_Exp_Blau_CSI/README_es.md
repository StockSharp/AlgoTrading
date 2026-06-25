# Estrategia Exp Blau CSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión en C# del asesor experto MetaTrader 5 `Exp_BlauCSI`. Opera en el Blau Candle Stochastic Index (CSI) calculado sobre una serie de velas seleccionada. La estrategia puede reaccionar ya sea a rupturas de la línea cero o a cambios de dirección en el indicador y soporta niveles configurables de stop-loss y take-profit medidos en pasos de precio.

## Lógica de trading

El Blau CSI compara un componente de momentum con el rango máximo-mínimo de velas recientes. Ambas partes se suavizan tres veces usando un tipo de media móvil seleccionado.

* **Modo Ruptura** – abre una posición larga cuando el indicador cruza por debajo de cero y cierra cualquier corto mientras el valor anterior era positivo. Abre una posición corta en un cruce por encima de cero y cierra cualquier largo mientras el valor anterior era negativo.
* **Modo Giro** – abre una posición larga cuando el indicador gira hacia arriba (el valor sube comparado con la barra anterior después de declinar). Abre una posición corta cuando el indicador gira hacia abajo. La dirección de la barra anterior siempre se usa para cerrar posiciones existentes del lado opuesto.

Las señales se evalúan en una barra histórica configurable (`Signal Bar`) para garantizar la confirmación en velas completamente cerradas.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Entry Mode` | Selecciona la lógica `Breakdown` o `Twist`. |
| `Smoothing Method` | Tipo de media móvil usado dentro del Blau CSI (Simple, Exponencial, Suavizada, LinearWeighted o Jurik). |
| `Momentum Length` | Número de barras usadas para calcular los componentes de momentum y rango. |
| `First/Second/Third Smoothing` | Profundidad de las tres etapas de suavizado aplicadas al momentum y rango. |
| `Smoothing Phase` | Parámetro de fase para suavizado Jurik (ignorado por otros métodos). |
| `Momentum Price` / `Reference Price` | Constantes de precio aplicadas para los valores de momentum líderes y rezagados (cierre, apertura, máximo, mínimo, mediana, típico, ponderado, simple, cuarto, seguimiento de tendencia o Demark). |
| `Signal Bar` | Desplazamiento (en barras) usado al evaluar el buffer Blau CSI. Por defecto `1` significa la barra cerrada anterior. |
| `Stop Loss (pts)` | Distancia de stop-loss en pasos de precio (`0` deshabilita). |
| `Take Profit (pts)` | Distancia de take-profit en pasos de precio (`0` deshabilita). |
| `Allow Long/Short Entries` | Habilitar o deshabilitar la apertura de posiciones para cada dirección. |
| `Allow Long/Short Exits` | Habilitar o deshabilitar señales de salida para posiciones existentes. |
| `Candle Type` | Tipo de datos para la suscripción (por defecto marco temporal de 4 horas). |
| `Start Date` / `End Date` | Filtros de fecha para la actividad de trading. |
| `Order Volume` | Volumen de orden de mercado. |

## Gestión de riesgo

Cuando se abre una nueva posición, la estrategia calcula los niveles de stop-loss y take-profit usando el `PriceStep` del instrumento. Si el instrumento no proporciona un paso, los stops se deshabilitan automáticamente. El trailing no se realiza; cada posición mantiene los niveles de protección iniciales hasta que se cierra por una señal o al alcanzar un objetivo.

## Notas de uso

1. Adjuntar la estrategia a un instrumento que proporcione datos de velas para el `Candle Type` seleccionado.
2. Elegir el modo del indicador y los parámetros de suavizado según su plan de trading.
3. Asegurarse de que el instrumento tenga un `PriceStep` válido al usar distancias de stop-loss o take-profit.
4. Opcionalmente restringir el trading a un rango de tiempo usando `Start Date` y `End Date`.

## Diferencias comparadas con la versión original MT5

* La implementación usa indicadores StockSharp y APIs de estrategia en C# en lugar de funciones de trading de MetaTrader.
* La gestión del tamaño de lote está simplificada: el volumen de la orden se toma directamente del parámetro `Order Volume`.
* Solo se soportan los métodos de suavizado provistos por StockSharp (Simple, Exponencial, Suavizada, LinearWeighted, Jurik). Los modos MT5 no soportados recurren al suavizado Exponencial.
* Los toggles de dirección de operación y la gestión de stop permanecen compatibles con el comportamiento original.

La estrategia está lista para backtesting dentro de StockSharp Designer, Shell, Runner o cualquier aplicación host de StockSharp personalizada.
