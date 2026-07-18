# Estrategia Bollinger ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina las Bandas de Bollinger y el indicador ADX. Busca rupturas con fuerte confirmación de tendencia.

Las pruebas indican un retorno anual promedio de aproximadamente el 46%. Funciona mejor en el mercado de acciones.

Los movimientos de precio fuera de las Bandas de Bollinger se filtran mediante ADX para confirmar la fortaleza. Las operaciones se activan cuando una ruptura de banda coincide con un ADX elevado.

Útil para aumentos de volatilidad acompañados de tendencias fuertes. El tamaño del stop se determina mediante el ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close < LowerBand && ADX > AdxThreshold`
  - Corto: `Close > UpperBand && ADX > AdxThreshold`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Reversión a la media de Bollinger
- **Stops**: Basados en ATR usando `AtrMultiplier`
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, ADX
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
