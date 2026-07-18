# Estrategia Williams %R Hook Reversal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Williams %R Hook Reversal sigue el indicador Williams %R cuando se retrae rápidamente desde un extremo. Cuando la lectura se mueve por encima de -20 o por debajo de -80 y luego engancha hacia el centro, el impulso previo probablemente está agotado.

Las pruebas indican un rendimiento anual promedio de aproximadamente 172%. Funciona mejor en el mercado de divisas.

La estrategia compra cuando %R revierte al alza desde la sobreventa mientras el precio presiona nuevos mínimos, y vende cuando engancha hacia abajo desde la sobrecompra durante nuevos máximos.

Un stop porcentual ajustado controla el riesgo, y las operaciones se cierran una vez que %R engancha en la dirección opuesta o se activa el stop.

## Detalles

- **Criterios de entrada**: señal del indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minutos
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Williams %R
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
