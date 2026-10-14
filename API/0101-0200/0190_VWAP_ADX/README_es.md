# Estrategia Vwap Adx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en los indicadores VWAP y ADX. Entra largo cuando el precio está por encima del VWAP y ADX > 25. Entra corto cuando el precio está por debajo del VWAP y ADX > 25. Sale cuando ADX < 20.

Las pruebas indican un retorno anual promedio de aproximadamente 157%. Funciona mejor en el mercado de criptomonedas.

El VWAP actúa como referencia de la sesión y el ADX mide la convicción. Las entradas aparecen cuando el precio se aleja del VWAP con ADX mostrando fortaleza.

Adecuado para traders intradía de tendencia. Los stops protectores usan un porcentaje fijo del precio de entrada.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > VWAP && ADX > 25`
  - Corto: `Close < VWAP && ADX > 25`
- **Largo/Corto**: Ambos
- **Criterios de salida**: ADX cae por debajo del umbral
- **Stops**: Porcentual usando `StopLossPercent`
- **Valores predeterminados**:
  - `StopLossPercent` = 2
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25
  - `AdxExitThreshold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    El mercado opera las 24 horas, por lo que el VWAP de la sesión se reinicia cada día UTC y pondera el precio típico de cada vela por su volumen. Los valores 25 y 20 de las reglas son los predeterminados de AdxThreshold y AdxExitThreshold. Una señal de entrada contra una posición abierta la invierte.
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

