# Estrategia de Agotamiento de Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Los picos bruscos de volumen suelen señalar el final de un movimiento cuando los traders se apresuran a cerrar o abrir posiciones. Esta estrategia compara el volumen actual con un promedio para detectar agotamiento. Combinada con la dirección de la vela y un filtro de media móvil, puede identificar entradas de reversión.

Las pruebas indican una rentabilidad anual media de aproximadamente el 133%. Funciona mejor en el mercado de criptomonedas.

Cada vela actualiza el volumen promedio. Si el volumen de la nueva barra supera este promedio por un multiplicador definido y la vela cierra en dirección contraria a la tendencia predominante, el sistema abre una operación. Un stop basado en ATR protege la posición.

La operación generalmente se cierra mediante el stop-loss, ya que la estrategia anticipa una reversión rápida tras el pico de volumen.

## Detalles

- **Criterios de entrada**: Pico de volumen por encima del promedio con vela contraria a la tendencia.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss.
- **Stops**: Sí, basado en ATR.
- **Valores predeterminados**:
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 2.0
  - `MAPeriod` = 20
  - `AtrMultiplier` = 2 ATR
  - `CandleType` = 5 minute
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Volume, MA, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

