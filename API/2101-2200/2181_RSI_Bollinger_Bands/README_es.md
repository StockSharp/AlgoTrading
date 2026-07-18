# Estrategia RSI Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina el Índice de Fuerza Relativa (RSI) con las Bollinger Bands. Se abre una posición larga cuando el RSI está por debajo del umbral de sobreventa y el precio de cierre está por debajo de la banda inferior de Bollinger. Se abre una posición corta cuando el RSI está por encima del umbral de sobrecompra y el precio de cierre está por encima de la banda superior de Bollinger. Las posiciones se revierten ante señales opuestas.

## Detalles

- **Criterios de entrada**: RSI por debajo de `RsiOversold` y precio de cierre por debajo de la banda inferior para comprar; RSI por encima de `RsiOverbought` y precio de cierre por encima de la banda superior para vender.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal inversa.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `RsiPeriod` = 20
  - `BollingerPeriod` = 20
  - `BollingerWidth` = 2
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RSI, Bollinger Bands
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: 15 minutos
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
