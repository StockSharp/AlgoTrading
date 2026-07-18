# Estrategia MACD CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia MACD + CCI. Comprar cuando el MACD está por encima de la línea de señal y el CCI está por debajo de -100 (sobrevendido). Vender cuando el MACD está por debajo de la línea de señal y el CCI está por encima de 100 (sobrecomprado).

Las pruebas indican un retorno anual promedio de aproximadamente el 70%. Funciona mejor en el mercado de acciones.

Las oscilaciones del MACD resaltan los cambios de momentum; el CCI ayuda a cronometrar las entradas en retrocesos en esa dirección. Son posibles tanto operaciones largas como cortas.

Los traders que combinan momentum con osciladores pueden encontrar útil esta técnica. El control de riesgo utiliza un stop basado en ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: `MACD > Signal && CCI < CciOversold`
  - Corto: `MACD < Signal && CCI > CciOverbought`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Cruce del MACD en dirección opuesta
- **Stops**: Basados en porcentaje usando `StopLoss`
- **Valores predeterminados**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: MACD, CCI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
