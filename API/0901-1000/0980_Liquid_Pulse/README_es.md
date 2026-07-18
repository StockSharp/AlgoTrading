# Estrategia de Pulso Líquido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Detecta picos de volumen elevado confirmados por MACD y ADX. El ATR define el stop y el take profit con límite diario de operaciones.

## Detalles

- **Criterios de entrada**:
  - Largo: pico de volumen, MACD cruza por encima de la señal, +DI > -DI, ADX >= umbral
  - Corto: pico de volumen, MACD cruza por debajo de la señal, -DI > +DI, ADX >= umbral
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop o take profit basado en ATR
- **Stops**: Múltiplos de ATR
- **Valores predeterminados**:
  - `VolumeSensitivity` = Medium
  - `MacdSpeed` = Medium
  - `DailyTradeLimit` = 20
  - `AtrPeriod` = 9
  - `AdxTrendThreshold` = 41
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MACD, ADX, ATR, Volumen
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
