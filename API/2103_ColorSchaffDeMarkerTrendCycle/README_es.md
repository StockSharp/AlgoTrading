# Estrategia de Ciclo de Tendencia Color Schaff DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Ciclo de Tendencia Color Schaff DeMarker** utiliza un oscilador personalizado derivado de los valores rápido y lento de DeMarker. El indicador aplica dos pasos estocásticos para crear un valor de ciclo que oscila entre -100 y +100. Los colores se asignan según el nivel y la pendiente del oscilador, que luego se utilizan para generar señales de trading.

La estrategia entra en posiciones largas cuando el oscilador sale de la zona superior y abandona posiciones cortas. Abre posiciones cortas cuando el oscilador sale de la zona inferior y abandona posiciones largas. La idea es reaccionar a los cambios de momentum en niveles extremos.

## Detalles

- **Criterios de entrada**:
  - **Largo**: color anterior > 5 y color actual < 6.
  - **Corto**: color anterior < 2 y color actual > 1.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - **Largo**: color < 2 cuando hay una posición larga abierta.
  - **Corto**: color > 5 cuando hay una posición corta abierta.
- **Stops**: Sin stop-loss ni take-profit explícitos.
- **Valores predeterminados**:
  - `FastDeMarker` = 23
  - `SlowDeMarker` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: DeMarker, Highest, Lowest
  - Stops: No
  - Complejidad: Medio
  - Marco temporal: 4H
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
