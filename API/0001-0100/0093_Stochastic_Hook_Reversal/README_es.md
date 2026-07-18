# Estrategia Stochastic Hook Reversal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Stochastic Hook Reversal observa la línea %K en busca de un gancho fuera del territorio de sobrecompra o sobreventa. Después de extenderse a un extremo, el oscilador frecuentemente se curva de regreso, indicando que el impulso está disminuyendo.

Las pruebas indican un rendimiento anual promedio de aproximadamente 166%. Funciona mejor en el mercado de acciones.

El sistema entra largo cuando %K gira al alza desde por debajo de veinte mientras el precio presiona un nuevo mínimo. Vende en corto cuando el oscilador engancha hacia abajo desde por encima de ochenta durante un empuje final hacia arriba.

Las posiciones utilizan un pequeño stop porcentual y se cierran cuando el estocástico engancha en la otra dirección o se alcanza el stop.

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
  - Indicadores: Stochastic
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
