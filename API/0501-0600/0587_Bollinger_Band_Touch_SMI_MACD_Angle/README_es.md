# Estrategia de Toque de Bollinger Band con Ángulos SMI y MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia compra cuando el precio toca la banda inferior de Bollinger y tanto los ángulos SMI como MACD apuntan hacia arriba. La posición se cierra una vez que el precio alcanza la banda superior de Bollinger.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio de cierre toca o cae por debajo de la banda inferior de Bollinger y los ángulos SMI/MACD son positivos pero por debajo de sus umbrales.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - **Largo**: El precio de cierre toca o supera la banda superior de Bollinger.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2.0
  - `SmiLength` = 14
  - `SmiSignalLength` = 3
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `SmiAngleThreshold` = 60
  - `MacdAngleThreshold` = 50
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Solo largos
  - Indicadores: Bollinger Bands, Stochastic (SMI), MACD
  - Stops: Ninguno
  - Complejidad: Bajo
  - Marco temporal: 1H
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
