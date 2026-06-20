# Estrategia del Efecto Día de la Semana
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
El Efecto Día de la Semana aprovecha las tendencias de los mercados a exhibir un comportamiento recurrente en días específicos de la semana.
Algunos índices muestran una fortaleza consistente a mitad de semana, mientras que el lunes o el viernes pueden ser relativamente débiles.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 85%. Funciona mejor en el mercado de criptomonedas.

La estrategia abre operaciones basándose en esas tendencias históricas, comprando o vendiendo al inicio de la sesión y saliendo al cierre.

Un stop moderado protege contra anomalías, cerrando la posición anticipadamente si el patrón falla en un día determinado.

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

