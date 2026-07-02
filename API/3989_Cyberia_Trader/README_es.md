# Estrategia adaptativa de Cyberia Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia adaptativa de Cyberia Trader** es una adaptación de C# del asesor experto clásico MetaTrader "CyberiaTrader". el
La estrategia reconstruye el núcleo original impulsado por la probabilidad en StockSharp y lo aumenta con filtros técnicos opcionales.
Analiza continuamente las oscilaciones de precios para medir las probabilidades de reversiones y luego, opcionalmente, confirma la señal con EMA,
MACD, CCI, ADX o filtros fractales antes de enviar pedidos.

## motor de probabilidad
El corazón de la estrategia es la calculadora de probabilidades inspirada en la versión MQL. Utiliza un período de muestreo adaptativo.
(`ValuePeriod`) e inspecciona barras históricas en pasos fijos para clasificar cada barra como:

* **Probabilidad de venta** – barra alcista después de una barra bajista (potencial oportunidad de desvanecimiento).
* **Probabilidad de compra** – barra bajista después de una barra alcista.
* **Probabilidad indefinida**: todas las demás configuraciones de barras.

Para cada clase, la estrategia acumula estadísticas de amplitud promedio, tasa de aciertos y tasa de éxito durante `ValuePeriod × HistoryMultiplier`
muestras. La búsqueda adaptativa escanea períodos desde `1` hasta `MaxPeriod` (predeterminado 23) y mantiene el período que produce el mayor
tasa de éxito. Estas estadísticas se exponen internamente como:

* `BuyPossibility`, `SellPossibility`, `UndefinedPossibility`: valores de clasificación de barras actuales.
* `BuyPossibilityMid`, `SellPossibilityMid`, ...: promedios móviles utilizados por el árbol de decisión original.
* `PossibilityQuality`, `PossibilitySuccessQuality`: índices de calidad utilizados para el diagnóstico y la selección automática de períodos.

Cuando no hay suficiente historial disponible, la estrategia simplemente espera hasta que el motor de probabilidad informe un conjunto de muestras válido.

## Filtros de indicador
El EA original permitía habilitar o deshabilitar módulos adicionales basados en indicadores. El puerto mantiene la misma idea:

* Filtro **EMA**: compara la pendiente de un EMA (`MaPeriod`) entre las dos últimas velas terminadas.
* Filtro **MACD**: comprueba la relación entre MACD y su línea de señal (`MacdFast`, `MacdSlow`, `MacdSignal`).
* Filtro **CCI**: señala regímenes de sobrecompra/sobreventa utilizando umbrales `CciPeriod` y ±100.
* Filtro **ADX**: inspecciona los componentes +DI y −DI (`AdxPeriod`) para preferir la dirección dominante.
* **Filtro fractal**: detecta la oscilación más reciente mediante una ventana `FractalDepth` configurable y bloquea las órdenes en su contra.
* **Detector de inversión**: alterna las banderas de dirección cuando un pico de probabilidad excede `ReversalIndex` veces su promedio.

Cada módulo se puede alternar mediante parámetros y refleja el comportamiento de las entradas externas booleanas originales.

## Lógica comercial
1. Suscríbete a la serie de velas configuradas (`CandleType`).
2. Reconstruya las estadísticas de probabilidad y, opcionalmente, vuelva a seleccionar el período de muestreo óptimo en cada vela terminada.
3. Aplique los filtros de indicadores opcionales y el árbol de decisión de Cyberia para habilitar o deshabilitar las direcciones de compra/venta.
4. Ejecutar operaciones cuando esté activa una decisión de compra o venta, respetando los interruptores globales `BlockBuy` y `BlockSell`.
5. Opcionalmente, aplique protección absoluta de stop-loss o take-profit si se especifican `StopLossPoints` o `TakeProfitPoints`.
6. Cierre posiciones temprano cuando la decisión sea `Unknown` y la calidad de la probabilidad se deteriore.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Serie de velas utilizadas para los cálculos. |
| `AutoSelectPeriod` | Habilita la búsqueda adaptativa en `MaxPeriod` para encontrar la mejor ventana de muestreo. |
| `InitialPeriod` | Período de probabilidad de retroceso cuando la selección automática está deshabilitada. |
| `MaxPeriod` | Período máximo considerado durante la búsqueda adaptativa (por defecto 23 como el EA). |
| `HistoryMultiplier` | Número de muestras por período utilizadas en las estadísticas (por defecto 5). |
| `SpreadFilter` | Movimiento mínimo (en unidades de precio) requerido para tratar una probabilidad como "exitosa". |
| `EnableCyberiaLogic` | Alterna el árbol de decisión original que compara los promedios de probabilidad. |
| `EnableMa`, `EnableMacd`, `EnableCci`, `EnableAdx`, `EnableFractals`, `EnableReversalDetector` | Habilite filtros individuales. |
| `MaPeriod` | EMA longitud para el filtro de media móvil. |
| `MacdFast`, `MacdSlow`, `MacdSignal` | Configuración MACD. |
| `CciPeriod` | Longitud del índice del canal de productos básicos. |
| `AdxPeriod` | Longitud promedio del índice direccional. |
| `FractalDepth` | Número impar de velas analizadas para detectar la oscilación fractal más reciente. |
| `ReversalIndex` | Multiplicador utilizado por el detector de inversión. |
| `BlockBuy`, `BlockSell` | Interruptores duros que detienen la apertura de operaciones en la dirección indicada. |
| `TakeProfitPoints`, `StopLossPoints` | Distancias opcionales de toma de ganancias absoluta y stop-loss. |

## Notas
* La búsqueda del período adaptativo requiere suficiente historial: `ValuePeriod × HistoryMultiplier + ValuePeriod` barras.
* Todos los comentarios se reescribieron en inglés y la lógica se mantiene en el nivel alto StockSharp API con enlaces de indicadores.
* Las métricas de probabilidad son campos internos pero se exponen a través de registros o ampliando la estrategia si se necesitan más diagnósticos.
