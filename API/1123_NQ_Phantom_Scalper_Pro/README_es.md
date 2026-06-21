# Estrategia de Scalper Fantasma NQ Pro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de Ruptura de bandas VWAP con filtros opcionales de volumen y tendencia.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el precio cierra por encima de la banda VWAP superior con volumen confirmatorio.
  - **Corto**: el precio cierra por debajo de la banda VWAP inferior con volumen confirmatorio.
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - El precio vuelve a cruzar el VWAP o se activa el stop por ATR.
- **Stops**: Basado en ATR
- **Valores predeterminados**:
  - `Band #1 Mult` = 1.0
  - `Band #2 Mult` = 2.0
  - `ATR Length` = 14
  - `ATR Stop Mult` = 1.0
  - `Volume SMA Period` = 20
  - `Volume Spike Mult` = 1.5
  - `Trend EMA Length` = 50
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: VWAP, ATR, EMA, SMA
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
