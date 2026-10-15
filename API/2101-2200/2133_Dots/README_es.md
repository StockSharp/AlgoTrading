# Estrategia de Puntos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertida desde MQL5 "Exp_Dots". La estrategia opera reversiones cuando el indicador Dots cambia de color.
Va largo cuando el indicador cambia de azul a rojo y corto cuando cambia de rojo a azul.

## Detalles

- **Criterios de entrada**:
  - Largo: El color del indicador cambia de azul a rojo.
  - Corto: El color del indicador cambia de rojo a azul.
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `Length` = 10
  - `Filter` = 0m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoría: Reversión de tendencia
  - Dirección: Ambos
  - Indicadores: Dots (NonLag Moving Average)
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: 4H
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
