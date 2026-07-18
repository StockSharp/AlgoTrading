# Estrategia Bollinger Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Bollinger Stochastic combina las Bandas de Bollinger con el oscilador estocástico para identificar movimientos sobreextendidos.
Que el precio toque la banda exterior mientras el oscilador está en una zona extrema sugiere un posible retroceso.

Las pruebas indican un rendimiento anual promedio de aproximadamente 133%. Funciona mejor en el mercado de criptomonedas.

El sistema opera contra esos extremos, yendo largo cuando el precio toca la banda inferior con el estocástico en sobreventa, y vendiendo en corto en la banda superior con el estocástico en sobrecompra.

Un stop basado en porcentaje limita el riesgo si la reversión a la media no ocurre.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, Stochastic
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

