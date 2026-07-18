# Estrategia Bollinger Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en los indicadores Bollinger Bands y Williams %R. Entra largo cuando el precio está en la banda inferior y el Williams %R está sobrevendido (< -80). Entra corto cuando el precio está en la banda superior y el Williams %R está sobrecomprado (> -20).

Las pruebas indican un rendimiento anual promedio de aproximadamente 103%. Funciona mejor en el mercado de acciones.

Las bandas de Bollinger exponen las rupturas de volatilidad y el Williams %R asegura que el impulso sea extremo. Las posiciones se abren cuando el precio cierra fuera de una banda con una lectura de Williams %R correspondiente.

La mejor opción para traders de expansión de volatilidad. Los stops de ATR manejan los giros adversos.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close < LowerBand && WilliamsR < -80`
  - Corto: `Close > UpperBand && WilliamsR > -20`
- **Largo/Corto**: Ambos
- **Criterios de salida**: El precio regresa a la banda media
- **Stops**: Basados en ATR usando `AtrMultiplier`
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `WilliamsRPeriod` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, Williams %R, R
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

