# Estrategia de Exceeded Candle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este enfoque basado en patrones busca velas alcistas envolventes que superen la barra anterior mientras el precio aún está por debajo de la banda media de Bollinger. La idea es que una fuerte reversión dentro de un retroceso puede impulsar el precio de vuelta hacia la banda superior. La estrategia solo opera en largo y cancela entradas cuando aparecen tres velas bajistas consecutivas.

Cuando el precio toca la banda superior de Bollinger, la posición se cierra, capturando el rebote rápido. El método se adapta a marcos temporales cortos donde las bandas de volatilidad capturan oscilaciones de reversión a la media.

## Detalles

- **Criterios de entrada**:
  - **Largo**: vela anterior roja, vela actual verde y cierra por encima de la apertura anterior, `Close < MiddleBand`, sin tres velas rojas consecutivas
- **Largo/Corto**: Solo largos
- **Criterios de salida**:
  - **Largo**: `Close > UpperBand`
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Solo largos
  - Indicadores: Bollinger Bands, price action
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
