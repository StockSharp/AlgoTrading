# Estrategia Xbug Free
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de media móvil contraria que compra cuando el precio cruza por debajo de su media móvil y vende cuando el precio cruza por encima. Utiliza distancias simétricas de take-profit y stop-loss.

## Detalles

- **Criterios de entrada**: precio cruzando por debajo/encima de la media móvil simple
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta o stop de protección
- **Stops**: Sí
- **Valores predeterminados**:
  - `MaPeriod` = 19
  - `MaShift` = 15
  - `StopPoints` = 270
  - `Volume` = 0.1
  - `CandleType` = 4-hour
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: MA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Swing
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
