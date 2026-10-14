# Estrategia de Acumulación Wyckoff
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Acumulación Wyckoff describe una fase de base donde los grandes intereses construyen posiciones silenciosamente después de una caída.
El volumen y la acción del precio forman una serie de pruebas del soporte seguidas de mínimos más altos, sugiriendo una demanda creciente.

Las pruebas indican un rendimiento anual promedio de aproximadamente 61%. Funciona mejor en el mercado de criptomonedas.

Esta estrategia entra largo cuando el precio rompe el rango de acumulación, esperando una nueva tendencia alcista impulsada por esas compras anteriores.

Un stop de protección se coloca justo por debajo de la base para limitar pérdidas si el rompimiento falla.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basados en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `RangePeriod` = 20
  - `StopLossPercent` = 2
    El rango de acumulación es el máximo más alto y el mínimo más bajo de las RangePeriod velas anteriores. La estrategia solo compra, con un cierre por encima del rango; el stop se coloca StopLossPercent por debajo de la base y la posición se cierra cuando una vela cierra por debajo.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Volume, Price
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

