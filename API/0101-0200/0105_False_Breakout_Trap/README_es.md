# Estrategia de Trampa de Falso Rompimiento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La Trampa de Falso Rompimiento busca capitalizar los rompimientos que no logran mantenerse más allá del soporte o resistencia clave.
Los traders a menudo entran en un rompimiento solo para ver que el precio revierte rápidamente, dejándolos atrapados.

Las pruebas indican un rendimiento anual promedio de aproximadamente 52%. Funciona mejor en el mercado de criptomonedas.

Esta estrategia espera ese fracaso, entrando en la dirección opuesta una vez que el precio cierra de nuevo dentro del rango.

La colocación del stop es ajustada, justo más allá del nivel de rompimiento fallido, asegurando que las pérdidas se mantengan pequeñas si la reversión no se materializa.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basados en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Price Action
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

