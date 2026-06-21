# Estrategia Source
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Source entra en largo cuando la vela cierra por encima de su apertura y en corto cuando cierra por debajo. Porcentajes opcionales de stop loss, take profit y trailing stop gestionan la posición abierta.

## Detalles

- **Criterios de entrada**: largo cuando cierre > apertura, corto cuando cierre < apertura
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta o activación de gestión de stops
- **Stops**: Stop loss, take profit y trailing stop opcionales
- **Valores predeterminados**:
  - `SL %` = 1
  - `TP %` = 3
  - `Trail Points %` = 3
  - `Trail Offset %` = 1
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
