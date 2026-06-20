# Estrategia de Reversión en Pivot Point
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Los Pivot Points diarios y sus niveles de soporte y resistencia a menudo actúan como puntos de giro para la acción del precio intradía. Esta estrategia calcula los pivotes clásicos del floor-trader a partir del máximo, mínimo y cierre del día anterior, y luego busca velas que reboten desde S1 o R1.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 127%. Funciona mejor en el mercado de acciones.

Cuando el precio se acerca al nivel de soporte S1 y forma una vela alcista, se toma una entrada larga. Si el precio prueba el nivel de resistencia R1 e imprime una vela bajista, se abre un corto. Las operaciones salen al alcanzar el pivot central o si se activa el stop de protección.

El método se restablece al inicio de cada sesión de negociación con nuevos cálculos de pivote, lo que lo hace muy adecuado para sesiones con rangos intradía claros.

## Detalles

- **Criterios de entrada**: Vela alcista cerca de S1 o vela bajista cerca de R1.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Precio cruzando el pivot central o stop-loss.
- **Stops**: Sí, basados en porcentaje.
- **Valores predeterminados**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Pivot Points
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

