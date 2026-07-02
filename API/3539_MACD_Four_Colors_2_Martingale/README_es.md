# MACD Cuatro colores 2 Martingale Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

# MACD Cuatro colores 2 Martingale

La estrategia traslada el asesor experto "MACD Four Colors 2 Martingale" de MetaTrader a StockSharp. Mantiene la lógica original construida en torno a la interpretación del "color" MACD y un modelo de tamaño de posición de martingala.

## Descripción general

El indicador subyacente pinta el histograma MACD con cinco colores. En el esquema de color "nuevo" predeterminado, el histograma cambia de color dependiendo de si la línea MACD sube o baja por encima o por debajo de la línea cero. El Asesor Experto abre una posición cada vez que los colores pasan de plateado a amarillo (el MACD negativo vuelve a bajar) o del rojo al azul (el MACD positivo se vuelve a bajar). La versión StockSharp reproduce esta secuencia reconstruyendo los colores a partir de los valores MACD.

Sólo una cesta direccional de operaciones está activa en cualquier momento. Se permite una nueva operación sólo si su precio mejora la entrada promedio de la cesta actual (precio más bajo para los largos, precio más alto para los cortos). Cada nueva entrada multiplica el último volumen llenado por un coeficiente de lote configurable, implementando la martingala promediando el EA original.

## Reglas comerciales

- **Lógica del indicador**: Un indicador `MovingAverageConvergenceDivergenceSignal` con la configuración clásica del 26/12/9 genera valores MACD.
- **Reconstrucción de color**: la estrategia compara los dos últimos valores MACD. El negativo ascendente MACD se asigna al color 1 (plata), el positivo ascendente al color 2 (rojo), el positivo decreciente al color 3 (azul) y el negativo al color 4 (amarillo).
- **Entrada larga**: Se activa cuando los colores reconstruidos pasan del 1 al 4 mientras el MACD de la barra anterior permanece por debajo de cero. La operación se ejecuta solo si no hay exposición corta y el nuevo precio es más bajo que cualquier entrada larga existente.
- **Entrada corta**: Se activa cuando los colores pasan de 2 a 3 mientras el MACD de la barra anterior permanece por encima de cero. El comercio se dispara sólo si no hay una exposición larga y el nuevo precio es más alto que cualquier entrada corta existente.
- **Gestión de volumen**: el primer pedido utiliza `InitialVolume`. Cada orden posterior dentro de la misma cesta multiplica el último volumen ejecutado por `LotCoefficient`. Establecer el coeficiente ≤ 0 desactiva el multiplicador.
- **Control de pérdidas y ganancias**: el PnL flotante se evalúa en cada vela terminada. Al presionar `TargetProfit` se cierran todas las posiciones y se reinicia el ciclo de martingala. Incumplir `MaxDrawdown` (interpretado como un umbral de pérdida) también cierra todo y reinicia el ciclo. Se admiten umbrales negativos al igual que en el código original.
- **Salida de posición**: Aparte de los objetivos monetarios, no hay paradas automáticas. Las posiciones permanecen abiertas hasta que se alcanza un umbral de riesgo o el usuario interviene manualmente.

## Parámetros

- `CandleType` *(Tipo de datos, predeterminado 1h)*: período de tiempo para el cálculo de MACD.
- `InitialVolume` *(decimal, predeterminado 1)* – volumen del primer pedido en una cesta.
- `LotCoefficient` *(decimal, predeterminado 2)* – multiplicador aplicado al volumen anterior cuando la martingala está activa.
- `MaxDrawdown` *(decimal, predeterminado 50)* – umbral de pérdida flotante (en dinero) que obliga a la liquidación. Los valores positivos miran `-MaxDrawdown`, los valores negativos usan el valor exacto.
- `TargetProfit` *(decimal, predeterminado 150)* – objetivo de beneficio flotante (en dinero) que cierra la cesta. Los valores negativos invierten la comparación como en la versión MQL.
- `FastEmaPeriod` *(int, predeterminado 12)* – duración de la EMA rápida para MACD.
- `SlowEmaPeriod` *(int, predeterminado 26)* – duración del EMA lento para MACD.
- `SignalPeriod` *(int, predeterminado 9)* – longitud de la señal EMA para el suavizado MACD.

## Notas de uso

- Funciona en cualquier instrumento que defina `PriceStep` y `StepPrice`, porque el PnL no realizado se calcula a partir de las especificaciones del intercambio.
- El tamaño de la martingala puede hacer crecer la posición rápidamente. Valide los límites de riesgo antes de permitir el comercio en una cuenta real.
- Para un análisis visual, adjunte el área del gráfico creada por la estrategia. Traza velas, el indicador MACD y ejecuta operaciones.

## Filtros de catálogo

- **Categoría**: Promedio de tendencia/impulso
- **Dirección**: Ambas (canastas largas y cortas)
- **Indicadores**: MACD
- **Paradas**: salida únicamente basada en dinero
- **Plazo**: Configurable (predeterminado 1h)
- **Complejidad**: Intermedio
- **Riesgo**: Alto debido a la descamación de martingala
- **Automatización**: Completamente automatizado una vez iniciado
