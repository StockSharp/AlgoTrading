# Estrategia Vwap Macd
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en VWAP y MACD. Entra en largo cuando el precio está por encima del VWAP y MACD > Señal. Entra en corto cuando el precio está por debajo del VWAP y MACD < Señal. Sale cuando el MACD cruza su línea de señal en dirección opuesta.

Las pruebas indican un rendimiento anual promedio de aproximadamente 181%. Funciona mejor en el mercado cripto.

VWAP orienta el valor intradía, y los cruces de MACD revelan cambios de momentum. Las operaciones se inician cuando el MACD gira cerca del nivel VWAP.

Adecuado para operadores de momentum a corto plazo. Las reglas de stop ATR previenen el riesgo excesivo.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > VWAP && MACD > Signal`
  - Corto: `Close < VWAP && MACD < Signal`
- **Largo/Corto**: Ambos
- **Criterios de salida**: cruce de MACD en sentido opuesto
- **Stops**: Basados en porcentaje usando `StopLossPercent`
- **Valores predeterminados**:
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: VWAP, MACD
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

