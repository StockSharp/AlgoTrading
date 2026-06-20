# Adx Bollinger Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en los indicadores ADX y Bandas de Bollinger. Entra largo cuando ADX > 25 y el precio rompe por encima de la banda superior de Bollinger. Entra corto cuando ADX > 25 y el precio rompe por debajo de la banda inferior de Bollinger.

Las pruebas indican un retorno anual promedio de aproximadamente 115%. Funciona mejor en el mercado de acciones.

Las rupturas de las bandas de Bollinger filtradas con ADX garantizan que el precio esté rompiendo con fuerza. El sistema opera en la dirección de la ruptura.

Adecuado para entornos de alta volatilidad. Un stop basado en ATR reduce el riesgo a la baja.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close < LowerBand && ADX > 25`
  - Corto: `Close > UpperBand && ADX > 25`
- **Largo/Corto**: Ambos
- **Criterios de salida**: El precio regresa a la banda media
- **Stops**: Basado en ATR usando `AtrMultiplier`
- **Valores predeterminados**:
  - `AdxPeriod` = 14
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: ADX, Bollinger Bands
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

