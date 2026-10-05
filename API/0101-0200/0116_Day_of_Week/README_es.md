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
  - `StopLossPercent` = 2
    Los días son días UTC, ya que el mercado opera las 24 horas; las entradas ocurren al cierre de la primera vela del día. De martes a jueves compra y los lunes y viernes vende en corto; la posición se cierra en la última vela del día y los fines de semana no se opera.
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

