# Estrategia de Bollinger Divergence
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bollinger Divergence busca extremos donde el precio perfora una banda mientras la
banda opuesta comienza a contraerse. Esta divergencia entre el impulso del precio
y la volatilidad a menudo precede a un rebote hacia el centro del rango.

Una señal larga aparece cuando una vela cierra por debajo de la banda inferior
mientras la banda superior se estrecha al menos un porcentaje definido. Para los
cortos, el patrón se refleja alrededor de la banda superior. Las posiciones apuntan
a un movimiento rápido de regreso a la línea central de Bollinger con una toma de
ganancias fija opcional.

El setup funciona mejor en mercados en rango o después de que un pico de volatilidad
comienza a desvanecerse. El parámetro `CandlePercent` controla cuánto debe
contraerse la banda opuesta antes de permitir una operación, ayudando a evitar
señales falsas durante tendencias fuertes.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: Cierre por debajo de la banda inferior Y la banda superior se contrae en `CandlePercent`.
  - **Corto**: Cierre por encima de la banda superior Y la banda inferior se contrae en `CandlePercent`.
- **Criterios de salida**:
  - Retorno a la banda central O porcentaje de toma de ganancias.
- **Stops**: Sin stop duro; depende de la toma de ganancias o salida manual.
- **Valores predeterminados**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `CandlePercent` = 30
  - `TakeProfit` = 5
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo/Corto
  - Indicadores: Bollinger Bands
  - Complejidad: Simple
  - Nivel de riesgo: Medio
