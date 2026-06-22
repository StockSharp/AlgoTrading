# Estrategia Universal de Stop Móvil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica la idea central del script MQL4 original `cm_universal_trailing_stop.mq4`. No genera señales de entrada; en cambio, gestiona una posición existente moviendo el stop-loss en la dirección de la ganancia.

El algoritmo mantiene un desplazamiento desde el precio actual y desplaza el stop cada vez que el mercado se mueve un paso configurable. Una vez que se alcanza el umbral mínimo de ganancia, el stop móvil se activa y sigue el precio automáticamente tanto para posiciones largas como cortas.

## Detalles

- **Criterios de entrada**: ninguno. La posición debe abrirse manualmente o por otra estrategia.
- **Largo/Corto**: ambos.
- **Criterios de salida**: orden de stop activada cuando el precio retrocede el desplazamiento configurado.
- **Stops**: stop móvil basado en puntos.
- **Parámetros**:
  - `Delta` – distancia desde el precio al stop en puntos.
  - `Step` – movimiento mínimo del precio en puntos para desplazar el stop.
  - `StartProfit` – ganancia en puntos requerida para activar el trailing.
  - `CandleType` – marco temporal utilizado para los cálculos de trailing.
- **Filtros**:
  - Categoría: Gestión de riesgos
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Trailing
  - Complejidad: Simple
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
