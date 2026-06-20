# Parabolic Sar Rsi Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia que combina el Parabolic SAR para la dirección de la tendencia y el RSI para la confirmación de entrada con condiciones de sobreventa/sobrecompra.

Las pruebas indican un retorno anual promedio de aproximadamente 166%. Funciona mejor en el mercado de acciones.

Aquí el Parabolic SAR define la tendencia predominante y el RSI mide el agotamiento. Las operaciones se abren una vez que ambos indicadores señalan la misma dirección.

La combinación es atractiva para quienes prefieren stops móviles, ya que el SAR también proporciona una salida dinámica. La colocación del stop sigue la curva del SAR.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > SAR && RSI < RsiOversold`
  - Corto: `Close < SAR && RSI > RsiOverbought`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: `Close < SAR`
  - Corto: `Close > SAR`
- **Stops**: Utiliza el Parabolic SAR como stop móvil
- **Valores predeterminados**:
  - `SarAf` = 0.02m
  - `SarMaxAf` = 0.2m
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Parabolic SAR, Parabolic SAR, RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

