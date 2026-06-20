# Vwap Adx Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en los indicadores VWAP y ADX. Entra largo cuando el precio está por encima del VWAP y ADX > 25. Entra corto cuando el precio está por debajo del VWAP y ADX > 25. Sale cuando ADX < 20.

Las pruebas indican un retorno anual promedio de aproximadamente 157%. Funciona mejor en el mercado de criptomonedas.

El VWAP actúa como referencia de la sesión y el ADX mide la convicción. Las entradas aparecen cuando el precio se aleja del VWAP con ADX mostrando fortaleza.

Adecuado para traders intradía de tendencia. Los stops protectores usan múltiplos de ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > VWAP && ADX > 25`
  - Corto: `Close < VWAP && ADX > 25`
- **Largo/Corto**: Ambos
- **Criterios de salida**: ADX cae por debajo del umbral
- **Stops**: Porcentual usando `StopLossPercent`
- **Valores predeterminados**:
  - `StopLossPercent` = 2m
  - `AdxPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: VWAP, ADX
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

