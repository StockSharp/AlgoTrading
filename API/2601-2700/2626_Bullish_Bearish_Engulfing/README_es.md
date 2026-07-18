# Estrategia de Alcista y Bajista Engulfing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica la configuración clásica de velas alcistas y bajistas de tipo engulfing que originalmente se implementó en MetaTrader para el asesor experto "Bullish and Bearish Engulfing". El port de StockSharp evalúa velas completadas en un marco temporal configurable, opcionalmente omite un número de barras recientes y reacciona cuando un patrón engulfing cumple un filtro de distancia mínima. La lógica está diseñada para traders discrecionales que desean automatizar un patrón de acción del precio bien establecido manteniendo el control sobre la dirección, el volumen y la gestión de posiciones existentes.

## Definición del patrón
Una señal engulfing se confirma cuando dos velas completadas consecutivas cumplen las siguientes reglas (después de aplicar el desplazamiento configurado):

- **Alcista engulfing**
  - La vela evaluada más reciente cierra por encima de su apertura (cuerpo alcista).
  - La vela anterior cierra por debajo de su apertura (cuerpo bajista).
  - La vela alcista tiene un máximo más alto y un mínimo más bajo que la vela anterior al menos por la distancia del filtro.
  - El cierre alcista termina por encima de la apertura anterior y su apertura está por debajo del cierre previo, respetando también el filtro de distancia.
- **Bajista engulfing**
  - La vela evaluada cierra por debajo de su apertura (cuerpo bajista).
  - La vela anterior cierra por encima de su apertura (cuerpo alcista).
  - La vela bajista aún registra un máximo más alto pero cierra bien por debajo de la apertura anterior, y su apertura supera el cierre previo, cada uno respetando el filtro de distancia.
  - El mínimo de la barra bajista está por debajo del mínimo anterior en el filtro de distancia.

Estas condiciones reproducen la implementación original de MetaTrader, que exigía que la vela engulfing cubriera completamente el cuerpo anterior y se extendiera más allá de ambos extremos. El filtro de distancia se mide en pips y se convierte a precio usando el paso de precio e índice de decimales del instrumento (las cotizaciones forex de 5 y 3 dígitos se escalan automáticamente a pips de 10 puntos).

## Lógica de trading
1. Suscribirse al tipo de vela seleccionado a través de la API de alto nivel y procesar solo las velas terminadas.
2. Mantener un pequeño buffer rodante que almacena los valores OHLC requeridos para el valor de desplazamiento actual.
3. Cuando al menos dos velas históricas están disponibles para evaluación, probar las condiciones de engulfing alcista y bajista descritas anteriormente.
4. Al recibir una señal alcista, enviar una orden de mercado en el lado definido por **BullishSide**. Al recibir una señal bajista, usar el lado configurado mediante **BearishSide**.
5. Si **CloseOppositePositions** está habilitado y existe una exposición opuesta, la estrategia aumenta el volumen de la orden en la posición actual absoluta para que la operación resultante cierre el lado contrario y abra uno nuevo en la dirección deseada. Cuando el indicador está deshabilitado, las señales se ignoran mientras hay una posición opuesta abierta.
6. El dimensionamiento de posiciones está controlado por el parámetro **Volume** de la estrategia (predeterminado 1 contrato/lote). No se adjunta ningún stop-loss ni take-profit automático por defecto; la gestión del riesgo queda a cargo del usuario final o de módulos de protección (puede combinarse con las protecciones integradas de StockSharp).

## Parámetros
| Parámetro | Descripción | Predeterminado | Notas |
|-----------|-------------|----------------|-------|
| `CandleType` | Marco temporal (StockSharp `DataType`) usado para la detección de señales. | Marco temporal de 1 hora | Ajustable a cualquier tipo de vela soportado. |
| `Shift` | Número de velas completadas a omitir antes de evaluar el patrón. | 1 | Configurar 1 analiza la última barra cerrada; valores más altos miran más atrás. |
| `DistanceInPips` | Distancia mínima en pips que la vela engulfing debe superar respecto a la anterior. | 0 | Convertido a precio usando el paso de precio del instrumento; útil para filtrar velas con cuerpos pequeños. |
| `CloseOppositePositions` | Si se debe cerrar una posición opuesta existente cuando se dispara una nueva señal. | `true` | Deshabilitarlo omite la operación si la exposición actual entra en conflicto con la señal. |
| `BullishSide` | Lado de la orden ejecutado en una señal alcista engulfing. | `Buy` | Puede invertirse a `Sell` para comportamiento contrario. |
| `BearishSide` | Lado de la orden ejecutado en una señal bajista engulfing. | `Sell` | Puede invertirse a `Buy` para operar configuraciones contra tendencia. |
| `Volume` | Tamaño base de la orden. | 1 | El volumen de la orden se incrementa en `abs(Position)` al cerrar el lado opuesto. |

## Gestión de posiciones y riesgo
- Dado que las órdenes se envían a mercado sin stops de protección, combine la estrategia con módulos adicionales (p. ej., `StartProtection`) o configure controles de riesgo externos.
- El código original de MetaTrader dimensionaba las operaciones mediante un gestor de dinero basado en riesgo. En este port el dimensionamiento se simplifica a un parámetro de volumen directo para que el comportamiento sea determinístico dentro de StockSharp; integre un bloque personalizado de gestión del capital si se requiere dimensionamiento dinámico.
- Cuando `CloseOppositePositions` es `true`, las reversiones son inmediatas: el volumen de la operación es igual al volumen base más la posición abierta absoluta, garantizando una transición de plano a nueva dirección.

## Archivos
- `CS/BullishBearishEngulfingStrategy.cs` – implementación principal en C# construida sobre la API de estrategia de alto nivel de StockSharp.

> **Nota:** No se proporciona implementación en Python para este ID; solo se incluye la versión en C# según lo solicitado.
