# Estrategia del Efecto Mes del Año
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
El Efecto Mes del Año captura las diferencias de rendimiento observadas en varios meses.
Por ejemplo, las acciones suelen repuntar en noviembre y diciembre, pero pueden ser débiles durante septiembre.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 88%. Funciona mejor en el mercado de acciones.

El sistema entra en largo o corto al comienzo de cada mes basándose en esos promedios históricos, saliendo a fin de mes.

Se utilizan stops para proteger el capital si el comportamiento estacional habitual no se materializa.

## Detalles

- **Criterios de entrada**: desencadenadores de efecto calendario
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Estacionalidad
  - Dirección: Ambos
  - Indicadores: Estacionalidad
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

