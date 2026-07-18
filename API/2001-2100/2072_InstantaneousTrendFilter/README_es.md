# Estrategia InstantaneousTrendFilter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia usa la Línea de Tendencia Instantánea de John Ehlers y una línea de disparo para generar señales en cualquier marco temporal. El disparo se calcula como `2 * ITrend - ITrend[2]`, formando una línea rápida que cruza la línea de tendencia más lenta. Un cruce descendente cierra posiciones cortas y abre una larga, mientras que un cruce ascendente cierra las largas y abre una corta. El factor de suavizado `Alpha` controla la capacidad de respuesta: valores más bajos producen líneas más suaves, valores más altos reaccionan más rápido.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El disparo estaba por encima de la línea de tendencia en la barra anterior y cruza por debajo en la barra actual.
  - **Corto**: El disparo estaba por debajo de la línea de tendencia en la barra anterior y cruza por encima en la barra actual.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Las posiciones largas se cierran cuando aparece una señal corta.
  - Las posiciones cortas se cierran cuando aparece una señal larga.
- **Stops**: Ninguno por defecto.
- **Valores predeterminados**:
  - `Alpha` = 0.07.
  - `Candle Type` = Marco temporal de 4 horas.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Único
  - Stops: No
  - Complejidad: Simple
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
