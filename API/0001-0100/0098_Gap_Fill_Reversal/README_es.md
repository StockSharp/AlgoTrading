# Estrategia Gap Fill Reversal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Reversión por Relleno de Brecha aprovecha los gaps nocturnos que retroceden rápidamente durante la siguiente sesión. Cuando el precio forma un gap alejándose del cierre anterior pero inmediatamente regresa para llenar ese vacío, a menudo señala el agotamiento del movimiento inicial.

Las pruebas indican un rendimiento anual promedio de aproximadamente 181%. Funciona mejor en el mercado de criptomonedas.

La estrategia entra una vez que el gap está completamente cerrado y busca una reversión en la dirección opuesta a la apertura. Su objetivo es capturar el rebote que ocurre cuando los traders atrapados salen de sus posiciones.

Un stop basado en porcentaje define el riesgo, y las posiciones se cierran cuando el impulso se desvanece o se alcanza el stop.

## Detalles

- **Criterios de entrada**: coincidencia de patrón
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minutos
  - `StopLoss` = 2%
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Gap
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
