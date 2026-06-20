# Estrategia de Desvanecimiento en el Descanso del Almuerzo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia de Desvanecimiento en el Descanso del Almuerzo apunta a reversiones que se desarrollan durante el lento período de mediodía.
Tras la sesión matutina, las tendencias suelen pausarse o retroceder cuando el volumen cae alrededor de la hora del almuerzo.

Las pruebas indican un retorno anual promedio de aproximadamente el 127%. Funciona mejor en el mercado de acciones.

La estrategia va en contra del movimiento matutino alrededor del mediodía, entrando en dirección contraria a la tendencia predominante y cubriendo antes de que el volumen regrese.

Un stop porcentual gestiona el riesgo si la tendencia se reanuda en lugar de desvanecerse.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Intradía
  - Dirección: Ambos
  - Indicadores: Price Action
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

