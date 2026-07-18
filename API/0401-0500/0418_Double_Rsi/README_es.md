# Estrategia Double RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Double RSI utiliza dos cálculos del Índice de Fuerza Relativa: uno en el gráfico de
trading y otro en un marco temporal superior. Las operaciones se realizan solo cuando
ambas lecturas de RSI apoyan la misma dirección, alineando las entradas de corto plazo
con el impulso de más largo plazo.

El marco temporal principal busca que el RSI cruce fuera de zonas de sobrecompra o
sobreventa. Si el RSI del marco temporal superior confirma el movimiento, la estrategia
abre una posición. Una toma de ganancias opcional puede asegurar las ganancias después
de un movimiento predefinido.

## Detalles
- **Datos**: Velas de precio en dos marcos temporales.
- **Criterios de entrada**:
  - **Largo**: RSI de marco temporal inferior sale de sobreventa Y RSI de marco temporal superior es alcista.
  - **Corto**: RSI de marco temporal inferior sale de sobrecompra Y RSI de marco temporal superior es bajista.
- **Criterios de salida**: Señal RSI opuesta o toma de ganancias si `UseTP` es verdadero.
- **Stops**: Ninguno por defecto.
- **Valores predeterminados**:
  - `CandleType` = tf(5)
  - `RSILength` = 14
  - `MTFTimeframe` = tf(15)
  - `UseTP` = False
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Largo/Corto
  - Indicadores: RSI (multi‑timeframe)
  - Complejidad: Moderado
  - Nivel de riesgo: Medio
