# Estrategia Parabolic SAR Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia Parabolic SAR + Stochastic. Comprar cuando el precio está por encima del SAR y el Stochastic %K está por debajo de 20 (sobrevendido). Vender cuando el precio está por debajo del SAR y el Stochastic %K está por encima de 80 (sobrecomprado).

Las pruebas indican un retorno anual promedio de aproximadamente el 61%. Funciona mejor en el mercado de criptomonedas.

El Parabolic SAR proporciona la tendencia y el Stochastic refina la entrada en los retrocesos. Las señales cambian cuando el SAR cambia de lado.

Una estrategia de tendencia directa con stops SAR integrados. La configuración del ATR gestiona el control de riesgo adicional.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > SAR && StochK < StochOversold`
  - Corto: `Close < SAR && StochK > StochOverbought`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Inversión del Parabolic SAR en dirección opuesta
- **Stops**: Dinámicos basados en SAR
- **Valores predeterminados**:
  - `AccelerationFactor` = 0.02m
  - `MaxAccelerationFactor` = 0.2m
  - `StochK` = 3
  - `StochD` = 3
  - `StochPeriod` = 14
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Parabolic SAR, Parabolic SAR, Stochastic Oscillator
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
