# Estrategia de Cuadrícula Aeron Robot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un sistema de cobertura basado en cuadrícula inspirado en el asesor experto AeronRobot. Coloca órdenes de compra y venta en intervalos de precio predefinidos y aumenta el volumen de la posición después de cada nueva orden. El enfoque busca capturar pequeñas oscilaciones de precio mientras controla el riesgo mediante take-profit, stop-loss y límites de operaciones configurables.

La estrategia trabaja con posiciones largas y cortas. Cuando el precio se mueve en pasos definidos por el parámetro *Gap*, se abre una nueva orden con volumen multiplicado por *LotsFactor*. Los beneficios se aseguran cuando el precio regresa *TakeProfit* puntos, y las pérdidas se cortan si el movimiento alcanza *StopLoss* puntos. La bandera *Hedging* permite mantener posiciones en ambos lados simultáneamente.

## Detalles

- **Criterios de entrada**:
  - **Largo**: el precio cae `Gap` puntos desde el último precio de compra.
  - **Corto**: el precio sube `Gap` puntos desde el último precio de venta.
- **Gestión de volumen**: el volumen de cada nueva orden se multiplica por `LotsFactor`.
- **Criterios de salida**:
  - las posiciones de un lado se cierran cuando el beneficio supera los puntos `TakeProfit`.
  - las posiciones de un lado se cierran cuando la pérdida supera los puntos `StopLoss`.
- **Parámetros**:
  - `FirstLot` – volumen inicial de la orden.
  - `LotsFactor` – multiplicador para las órdenes siguientes.
  - `Gap` – distancia base entre niveles de cuadrícula en puntos.
  - `GapFactor` – multiplicador que expande el intervalo después de cada operación.
  - `MaxTrades` – número máximo de operaciones por lado.
  - `Hedging` – permitir posiciones largas y cortas simultáneas.
  - `TakeProfit` – objetivo en puntos.
  - `StopLoss` – límite protector en puntos.
  - `CandleType` – marco temporal de velas usado para el procesamiento.
- **Largo/Corto**: ambos.
- **Filtros**:
  - Categoría: Cuadrícula / Reversión a la media
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto

