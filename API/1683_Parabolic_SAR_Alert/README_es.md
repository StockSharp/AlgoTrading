# Estrategia de Alerta Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia monitorea el indicador Parabolic SAR (Stop and Reverse) para detectar posibles reversiones de tendencia. Cuando el valor del SAR pasa de estar por encima del precio a estar por debajo, el algoritmo lo interpreta como una señal alcista y abre una posición larga. Cuando el SAR se mueve de debajo del precio hacia arriba, se abre una posición corta.

El factor de aceleración predeterminado (0.02) y la aceleración máxima (0.2) siguen la configuración clásica del Parabolic SAR. Estos parámetros controlan la rapidez con que el indicador se aproxima al precio: los valores más altos hacen que el SAR reaccione más rápido pero pueden provocar señales falsas. La estrategia procesa solo velas completadas y almacena los valores anteriores de SAR y precio para identificar cruces sin consultar datos históricos.

La gestión del riesgo no está definida explícitamente; el ejemplo se basa en señales opuestas para salir. Se puede activar protección adicional a través de los mecanismos integrados del framework.

## Detalles

- **Criterios de entrada**: Parabolic SAR cruza el precio de cierre.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No definidos.
- **Valores predeterminados**:
  - `InitialAcceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `CandleType` = 5 minute
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Opcional
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
