# Estrategia GRIM309 CallPut
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia GRIM309 CallPut opera basándose en la alineación de múltiples EMA con un sistema de advertencia. Las posiciones largas entran cuando las EMA de corto plazo confirman una tendencia alcista y la EMA5 sube por encima de la EMA10. Las posiciones cortas entran en condiciones opuestas. Un período de enfriamiento impide la reentrada inmediata tras un cierre. Una advertencia adicional activa salidas anticipadas cuando el diferencial EMA5-EMA10 se contrae rápidamente.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: EMA10 sobre EMA20, precio sobre EMA50, EMA5 sube sobre EMA10, sin posición y enfriamiento cumplido.
  - **Corto**: EMA10 bajo EMA20, precio bajo EMA50, EMA5 cae bajo EMA10, sin posición y enfriamiento cumplido.
- **Criterios de salida**: Precio cruzando EMA15 o señal de advertencia.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Ema5Length` = 5
  - `Ema10Length` = 10
  - `Ema15Length` = 15
  - `Ema20Length` = 20
  - `Ema50Length` = 50
  - `Ema200Length` = 200
  - `CooldownBars` = 2
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: EMA
  - Complejidad: Moderado
  - Nivel de riesgo: Medio
