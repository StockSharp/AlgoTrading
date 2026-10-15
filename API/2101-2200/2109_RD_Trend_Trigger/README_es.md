# Estrategia RD Trend Trigger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia RD Trend Trigger utiliza el oscilador RD-TrendTrigger para capturar reversiones de tendencia o rupturas de niveles según el modo seleccionado. En el modo twist, las operaciones siguen los cambios de dirección del oscilador; en el modo disposition, las operaciones se producen cuando el oscilador cruza niveles predefinidos.

## Detalles

- **Criterios de entrada**:
  - **Modo twist**: Entrar largo cuando el oscilador gira hacia arriba; entrar corto cuando gira hacia abajo.
  - **Modo disposition**: Entrar largo cuando el oscilador sube por encima de `HighLevel`; entrar corto cuando cae por debajo de `LowLevel`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Señales opuestas o condiciones de salida explícitas en el modo disposition cuando el oscilador sube por encima de `LowLevel`.
- **Stops**: Ninguno por defecto; la protección puede activarse externamente.
- **Valores predeterminados**:
  - `Regress` = 15
  - `T3Length` = 5
  - `T3VolumeFactor` = 0.7
  - `HighLevel` = 50
  - `LowLevel` = -50
  - `Mode` = Twist
  - `CandleType` = 4-hour candles
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo & Corto
  - Indicadores: RD-TrendTrigger personalizado (basado en máximos/mínimos y Tillson T3)
  - Stops: Opcional
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
