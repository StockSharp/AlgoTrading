# Estrategia UltraAbsolutelyNoLag LWMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General

La **Estrategia UltraAbsolutelyNoLag LWMA** replica las señales del experto MetaTrader Ultra Absolutely No Lag LWMA usando la API de alto nivel de StockSharp. La pila de indicadores evalúa una escalera de media móvil ponderada doble y mide cuántas etapas de suavizado apuntan hacia arriba o hacia abajo. Los conteos resultantes se suavizan nuevamente para generar un estado codificado por colores que impulsa la lógica de trading. La estrategia opcionalmente coloca órdenes protectoras de stop-loss y take-profit para cada nueva posición.

## Pipeline del Indicador

1. **Filtro LWMA doble** – el precio aplicado (cierre por defecto) es procesado por dos medias móviles ponderadas consecutivas para eliminar el ruido.
2. **Escalera de suavizado** – la serie filtrada pasa a través de un conjunto configurable de medias móviles. Cada paso usa el método de suavizado seleccionado (Jurik por defecto) y una longitud que aumenta con un paso fijo.
3. **Contador alcista/bajista** – cada paso compara el valor actual con el valor anterior. Los pasos en alza contribuyen al contador alcista, los pasos en baja al contador bajista.
4. **Suavizado final** – los contadores alcistas y bajistas se suavizan nuevamente usando el método seleccionado. Estos dos valores forman el estado final del indicador.

La estrategia recrea la lógica de color del indicador original: los estados fuertemente alcistas producen códigos 7–8, los estados moderadamente alcistas 5–6, los estados fuertemente bajistas 1–2 y los estados moderadamente bajistas 3–4. Cero denota un estado indefinido.

## Lógica de Trading

* Cuando la barra más antigua reportó un código alcista (`> 4`) y la barra más reciente cambia a un código bajista (`< 5` y distinto de cero), la estrategia cierra posiciones cortas abiertas y puede abrir una nueva posición larga.
* Cuando la barra más antigua reportó un código bajista (`< 5` y distinto de cero) y la barra más reciente cambia a un código alcista (`> 4`), la estrategia cierra posiciones largas abiertas y puede abrir una nueva posición corta.
* Las órdenes de stop-loss y take-profit pueden registrarse automáticamente después de cada entrada cuando los offsets correspondientes son mayores que cero.

La evaluación usa las dos barras completadas anteriores del marco temporal del indicador, coincidiendo con el comportamiento del experto MetaTrader que trabaja en el cierre de barra.

## Parámetros

| Nombre | Descripción |
| ------ | ----------- |
| `CandleType` | Tipo/marco temporal de vela usado para los cálculos del indicador. |
| `BaseLength` | Longitud del pre-filtro LWMA doble. |
| `AppliedPriceMode` | Precio aplicado (cierre, apertura, típico, DeMark, etc.) usado como entrada del indicador. |
| `TrendMethod` | Método de media móvil para la escalera de suavizado (Jurik, SMA, EMA, etc.). |
| `StartLength` | Longitud inicial de la escalera de suavizado. |
| `StepSize` | Paso agregado a la longitud de suavizado en cada etapa de la escalera. |
| `StepsTotal` | Número de etapas en la escalera de suavizado. |
| `SmoothingMethod` | Método usado para suavizar los contadores alcista/bajista. |
| `SmoothingLength` | Longitud de la etapa de suavizado final. |
| `UpLevelPercent` | Umbral de porcentaje que marca un estado fuertemente alcista. |
| `DownLevelPercent` | Umbral de porcentaje que marca un estado fuertemente bajista. |
| `SignalBar` | Índice de la barra usada para las señales de trading (1 = barra cerrada anterior). |
| `AllowBuyOpen` / `AllowSellOpen` | Habilitar apertura de posiciones largas/cortas. |
| `AllowBuyClose` / `AllowSellClose` | Habilitar cierre de posiciones largas/cortas existentes. |
| `StopLossOffset` | Distancia absoluta entre el precio de entrada y el stop-loss protector (0 deshabilita). |
| `TakeProfitOffset` | Distancia absoluta entre el precio de entrada y el take-profit (0 deshabilita). |

## Notas de Uso

1. Configurar el tipo de vela para que coincida con el marco temporal del indicador deseado (la versión MetaTrader usa H4 por defecto).
2. Ajustar los parámetros de la escalera si se necesitan reacciones más rápidas o lentas. Un `StepsTotal` más grande crea un indicador más suave pero más lento.
3. Dejar `StopLossOffset` y `TakeProfitOffset` en cero para deshabilitar las órdenes protectoras.
4. El mapeo del indicador usa medias móviles de StockSharp. Los métodos que no están disponibles en StockSharp recurren al suavizado Jurik o EMA.
5. La estrategia solo opera en velas terminadas para permanecer consistente con el experto original.
