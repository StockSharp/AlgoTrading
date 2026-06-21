# Estrategia de Volumen de Velas Dekidaka-Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina el cuerpo de la vela con el volumen suavizado usando el enfoque Dekidaka-Ashi. Compra con señales alcistas y vende con señales bajistas. Las velas que abarcan ambos rangos cierran las posiciones abiertas.

## Detalles

- **Criterios de entrada**:
  - Señal alcista fuerte o débil: máximo por encima del rango superior y mínimo por encima del rango inferior.
  - Señal bajista fuerte o débil: máximo por debajo del rango superior y mínimo por debajo del rango inferior.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Señal opuesta o vela que abarca ambos rangos (incertidumbre).
- **Stops**: No.
- **Valores predeterminados**:
  - `BodySize` = 1
  - `VolumeSmooth` = 1
  - `CandleType` = marco temporal de 5 minutos
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Largo y Corto
  - Indicadores: EMA, Volumen
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
