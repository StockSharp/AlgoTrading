# Three White Soldiers Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El patrón Tres Soldados Blancos es una reversión alcista clásica compuesta por tres velas alcistas fuertes consecutivas. Después de una tendencia bajista, esta secuencia a menudo marca el inicio de un movimiento sostenido al alza, ya que la presión compradora supera a los vendedores.

Las pruebas indican un rendimiento anual promedio de aproximadamente 175%. Funciona mejor en el mercado de acciones.

La estrategia entra largo una vez que se forma el tercer soldado, esperando el seguimiento del aumento en el impulso. Las operaciones cortas no se toman porque el setup es puramente alcista, pero el sistema permite cerrar posiciones cortas iniciadas por otros métodos.

Los stops se colocan a poca distancia por debajo del patrón para protegerse contra señales falsas, y las posiciones se cierran si el precio cierra de nuevo por debajo de ese nivel.

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
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
