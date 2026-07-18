# Estrategia Hancock RSI Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula un Índice de Fuerza Relativa (RSI) ponderado por volumen, inspirado en el script Hancock de TradingView. El RSI usa el volumen alcista y bajista para medir la fuerza de la tendencia. Se abre una posición larga cuando la tendencia del RSI gira al alza, y una posición corta cuando gira a la baja.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La tendencia del RSI cambia a alcista.
  - **Corto**: La tendencia del RSI cambia a bajista.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Señal de tendencia opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `RSI Length` = 14.
  - `Threshold` = 0.1.
  - `Use Wicks` = true.
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: RSI, Volumen
  - Stops: No
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
