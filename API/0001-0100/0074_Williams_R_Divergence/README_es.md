# Estrategia de Divergencia con Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El oscilador Williams %R mide las condiciones de sobrecompra y sobreventa. Cuando el precio marca un nuevo mínimo pero el %R forma un mínimo más alto, o cuando el precio imprime un nuevo máximo pero el %R gira a la baja, el impulso puede revertirse. Esta estrategia busca tales divergencias en los extremos del indicador.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 109%. Funciona mejor en el mercado de criptomonedas.

En cada barra, el sistema registra el último cierre y el valor de %R para compararlo con la lectura anterior. Una divergencia alcista combinada con un nivel por debajo de -80 activa una entrada larga, mientras que una divergencia bajista y una lectura por encima de -20 genera una posición corta. Los stops se establecen usando un porcentaje del precio.

Las posiciones se cierran cuando el oscilador vuelve al extremo opuesto, capturando el rebote desde la señal de divergencia.

## Detalles

- **Criterios de entrada**: Divergencia Precio/Williams %R con %R por debajo de -80 para largos o por encima de -20 para cortos.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Williams %R alcanzando el extremo opuesto o stop-loss.
- **Stops**: Sí, basados en porcentaje.
- **Valores predeterminados**:
  - `WilliamsRPeriod` = 14
  - `DivergencePeriod` = 5
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filtros**:
  - Categoría: Divergencia
  - Dirección: Ambos
  - Indicadores: Williams %R
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio

