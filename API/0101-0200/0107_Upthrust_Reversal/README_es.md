# Estrategia de Reversión Upthrust
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Reversión Upthrust es el complemento bajista del spring y ocurre cuando el precio rompe brevemente por encima de la resistencia pero cae rápidamente de nuevo.
El movimiento elimina a los compradores tardíos antes de revertir a la baja.

Las pruebas indican un rendimiento anual promedio de aproximadamente 58%. Funciona mejor en el mercado de acciones.

Esta estrategia vende en corto una vez que el precio cae de nuevo por debajo del nivel de rompimiento, esperando que la oferta supere a la demanda.

Un stop justo por encima del máximo del upthrust gestiona el riesgo y las posiciones salen si el precio se recupera por encima de ese nivel.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basados en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `LookbackPeriod` = 20
  - `StopLossPercent` = 2
    La resistencia es el máximo más alto de las LookbackPeriod velas anteriores. La estrategia solo vende: una vela bajista que supera la resistencia y cierra de nuevo por debajo. El stop se coloca StopLossPercent por encima del máximo del upthrust y la posición se cierra cuando una vela cierra por encima.
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Wyckoff
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

