# La estrategia de la hechicera
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia Enchantress replica el comportamiento de autoaprendizaje del asesor experto MQL4 con el mismo nombre. El EA original
clasifica cada vela terminada en diez grupos, mantiene un historial continuo de los últimos siete grupos y lanza compras “virtuales”
y vender órdenes para cada nuevo patrón de siete velas. Cada vez que el precio toca posteriormente los niveles virtuales de toma de ganancias o límite de pérdidas, el
El patrón recibe una puntuación positiva o negativa. Las operaciones en vivo se activan sólo cuando el patrón actual de siete velas pertenece al
patrones virtuales de alto rendimiento. Este puerto StockSharp preserva ese ciclo de retroalimentación y expone todas las opciones de configuración críticas
como parámetros de estrategia.

## Clasificación de velas

1. Cada vela terminada se evalúa una vez, utilizando sus precios de apertura, cierre, máximo y mínimo.
2. La dirección del cuerpo divide las velas en bajistas (dígitos `0–4`) y alcistas (dígitos `5–9`).
3. La relación alta/baja `100 - Low * 100 / High` determina el dígito exacto dentro de cada grupo:
   - `0/5` para rangos muy pequeños (≤ 0,04)
   - `1/6` para rangos pequeños (0,04 – 0,15)
   - `2/7` para rangos medios (0,15 – 0,25)
   - `3/8` para rangos amplios (0,25 – 0,40)
   - `4/9` para rangos extremadamente amplios (>0,40)
4. El último dígito se adjunta a la ventana móvil de siete caracteres que representa el patrón actual del mercado.

Esta clasificación coincide con los depósitos numéricos producidos por la rutina `ManagePatterns` del EA original.

## Motor de pedidos virtuales

- Una vez que hay siete dígitos disponibles, la estrategia crea un conjunto emparejado de órdenes virtuales (largas y cortas) para el patrón activo.
- El precio de entrada virtual es igual al cierre de la vela. Las paradas y objetivos virtuales se derivan de `VirtualStopLoss` y
`VirtualTakeProfit` usando el paso de precio del instrumento.
- En las velas siguientes, la estrategia comprueba si el máximo/mínimo de la vela toca los objetivos virtuales o se detiene:
  - Un objetivo alcanzado contribuye `+1` a la puntuación alcista o bajista respectiva.
  - Un golpe de parada aporta `-3` a la puntuación respectiva, reproduciendo la penalización utilizada por el EA.
- Las órdenes virtuales cerradas se descartan para mantener limitado el uso de la memoria, mientras que las puntuaciones acumuladas permanecen adjuntas a sus
clave de patrón de siete dígitos.

## Generación de señal

Antes de procesar la siguiente vela, la estrategia inspecciona el patrón actual de siete dígitos (construido únicamente a partir de velas anteriores). El comercio es
permitido de lunes a jueves; Los viernes se omiten exactamente igual que en la versión MQL. Se aplican las siguientes reglas:

1. Construya los diez mejores patrones alcistas y bajistas por puntuación (solo se consideran puntuaciones ≥ 1).
2. Si el patrón actual pertenece al conjunto líder alcista, realice una compra en el mercado. Si pertenece al conjunto líder bajista, coloque un
venta en el mercado. La misma vela no puede activar dos entradas porque la estrategia registra la marca de tiempo de la vela después del primer llenado.
3. Después de cada decisión, la vela recién completada se agrega a la ventana del patrón y a las órdenes virtuales para el nuevo patrón.
se lanzan.

## Órdenes de protección y dimensionamiento.

- Las operaciones reales utilizan distancias `StopLoss` y `TakeProfit` expresadas en pips. Ambos parámetros se traducen en diferencias de precios mediante
el paso del precio del valor y se aplica a través de `SetStopLoss`/`SetTakeProfit` justo después de que se complete la orden de mercado.
- El dimensionamiento de posiciones puede operar en dos modos:
  - **Lote fijo**: `LotSize` se utiliza textualmente (ajustado a las restricciones de volumen de intercambio/mínimo/máximo).
  - **Gestión de dinero de riesgo**: el volumen equivale a `PortfolioValue / 100000 * RiskPercent`. Esto refleja el `AccountFreeMargin` original
fórmula y vuelve al lote fijo si no hay ningún valor de cartera disponible.

## Parámetros

| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `LotSize` | Tamaño de orden fijo cuando la administración de dinero está deshabilitada. | `0.01` |
| `UseRiskMoneyManagement` | Alterna el bloque de tamaño dinámico. | `true` |
| `RiskPercent` | Porcentaje del valor de la cartera utilizado en modo riesgo. | `15` |
| `StopLoss` | Distancia real de stop-loss en pips. | `60` |
| `VirtualStopLoss` | Distancia de parada utilizada para la puntuación virtual. | `55` |
| `TakeProfit` | Distancia real de obtención de beneficios en pips. | `19` |
| `VirtualTakeProfit` | Distancia de toma de ganancias para puntuación virtual. | `25` |
| `CandleType` | Plazo de las velas procesadas. | `5m` |

## Notas de uso

- Asegúrese de que los metadatos de seguridad (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`) estén completos; de lo contrario tamaño y pip
las conversiones recurren a valores predeterminados genéricos.
- La valoración de la cartera (`Portfolio.CurrentValue` o `Portfolio.BeginValue`) debe estar disponible para que funcione el dimensionamiento basado en el riesgo.
- La estrategia opera únicamente con velas terminadas y no realiza verificaciones de órdenes virtuales dentro de la barra. La comparación alto/bajo es la
aproximación más cercana de la lógica basada en ticks de MT4.
- Para calentar la base de datos de patrones más rápido, ejecute la estrategia con datos históricos en modo de prueba retrospectiva; la lógica de puntuación es idéntica en
tanto simulación como comercio en vivo.
