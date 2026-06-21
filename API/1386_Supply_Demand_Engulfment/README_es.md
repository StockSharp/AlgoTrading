# Estrategia de Engullimiento de Oferta y Demanda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera patrones de engullimiento alcista y bajista cerca de las zonas de soporte y resistencia de Donchian.

## Detalles

- **Criterios de entrada**: Patrón de engullimiento en los límites de la zona.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `ZonePeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Donchian
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí (engulfing)
  - Nivel de riesgo: Medio
