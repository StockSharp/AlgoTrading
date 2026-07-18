# Estrategia de Acción de Delta de Volumen en Tiempo Real
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia rastrea la diferencia entre el volumen de compra y venta dentro de cada vela. Se abre una operación cuando el delta de volumen supera un umbral.

## Detalles

- **Criterios de entrada**: Delta de volumen por encima/debajo del umbral.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `DeltaThreshold` = 100
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Volume Delta
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
