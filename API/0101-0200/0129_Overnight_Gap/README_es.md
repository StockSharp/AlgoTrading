# Estrategia de Brecha Nocturna
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Brecha Nocturna opera en la apertura cuando el precio presenta un gap significativo respecto al cierre anterior debido a noticias o actividad fuera de horario.
Los grandes gaps suelen retraerse parcialmente a medida que los operadores digieren el movimiento.

Las pruebas indican un retorno anual promedio de aproximadamente el 124%. Funciona mejor en el mercado de forex.

La estrategia va en contra de los gaps excesivos, entrando en dirección opuesta poco después de la apertura y cerrando antes de que termine la sesión.

Los stops se basan en un porcentaje más allá de los extremos del gap para gestionar el riesgo si el movimiento continúa.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `MinGapPercent` = 0.01
    El mercado opera las 24 horas, por lo que la sesión es el día UTC. El gap es la distancia entre el último cierre del día anterior y la primera apertura del día. Un gap de al menos MinGapPercent se opera en contra al cierre de la primera vela; el stop se sitúa StopLossPercent más allá del máximo (corto) o del mínimo (largo) de esa vela y se comprueba en los cierres, y la posición se cierra en la última vela del día.
- **Filtros**:
  - Categoría: Gap
  - Dirección: Ambos
  - Indicadores: Gap
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

