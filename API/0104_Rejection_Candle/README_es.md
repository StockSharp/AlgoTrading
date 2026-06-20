# Estrategia de Vela de Rechazo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Una Vela de Rechazo se forma cuando el precio prueba un nivel pero no logra mantenerse más allá de él, dejando una mecha larga y un cuerpo pequeño.
Estas velas indican que un intento de moverse en una dirección fue firmemente rechazado por el mercado.

Las pruebas indican un rendimiento anual promedio de aproximadamente 49%. Funciona mejor en el mercado de criptomonedas.

La estrategia entra en la dirección opuesta a la mecha una vez que la vela cierra, esperando que el precio revierta de vuelta a través del rango.

Los stops se establecen fuera del máximo o mínimo rechazado para limitar el riesgo, y las operaciones salen si el impulso no llega a materializarse.

## Detalles

- **Criterios de entrada**: coincidencia de patrón
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basados en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

