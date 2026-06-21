# Estrategia DCA de Soporte y Resistencia con RSI y Filtro de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de promedio de costo en dólares que utiliza niveles de soporte/resistencia, RSI y filtro de tendencia EMA. Compra en soporte en una tendencia alcista cuando el RSI está sobrevendido y vende en resistencia en una tendencia bajista cuando el RSI está sobrecomprado.

## Detalles

- **Criterios de entrada**:
  - Largo: precio en soporte, RSI por debajo de sobrevendido, por encima del EMA
  - Corto: precio en resistencia, RSI por encima de sobrecomprado, por debajo del EMA
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: el precio alcanza la resistencia o RSI por encima de sobrecomprado
  - Corto: el precio alcanza el soporte o RSI por debajo de sobrevendido
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `LookbackPeriod` = 50
  - `RsiLength` = 14
  - `Overbought` = 70
  - `Oversold` = 40
  - `EmaPeriod` = 200
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: RSI, EMA, Highest, Lowest
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
