# RSI Hook Reversal Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia RSI Hook Reversal intenta capturar puntos de giro a corto plazo cuando el RSI sale de un extremo. Tras un empuje de sobrecompra o sobreventa, el indicador a menudo "engancha" de vuelta hacia la línea media antes de que el precio reaccione.

Las pruebas indican un rendimiento anual promedio de aproximadamente 163%. Funciona mejor en el mercado de acciones.

La estrategia espera ese gancho mientras el precio continúa presionando en la dirección anterior. Una entrada larga se activa una vez que el RSI se curva al alza desde la sobreventa mientras el precio marca un nuevo mínimo, mientras que un corto se abre cuando el RSI gira a la baja desde la sobrecompra durante un nuevo máximo.

Las operaciones utilizan un stop porcentual simple para controlar el riesgo y típicamente se cierran cuando el RSI engancha en la dirección opuesta.

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
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
