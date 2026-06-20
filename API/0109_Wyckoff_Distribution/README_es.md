# Estrategia de Distribución Wyckoff
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Distribución Wyckoff es una fase de techo caracterizada por ventas pesadas en los rallies y pruebas de resistencia.
El volumen a menudo se expande en los movimientos bajistas y se contrae en los rebotes, sugiriendo que los grandes intereses están liquidando posiciones.

Las pruebas indican un rendimiento anual promedio de aproximadamente 64%. Funciona mejor en el mercado de divisas.

Esta estrategia vende en corto cuando el precio rompe hacia abajo desde el rango de distribución, anticipando una caída sostenida.

Un stop justo por encima del rango protege contra falsos rompimientos, y las posiciones se cierran si el precio regresa a la parte superior de la estructura.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basados en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
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

