# Estrategia de Movimiento de Stop Loss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de utilidad monitorea una posición abierta y mueve su stop-loss al precio de entrada cuando el mercado alcanza un nivel predefinido. Se suscribe a datos de velas y verifica cada vela completada. Para posiciones largas, una vez que el máximo de la vela supera el `MoveSlPrice` configurado, se coloca una orden stop en el precio de entrada. Para posiciones cortas, el stop se mueve cuando el mínimo de la vela cae por debajo del nivel.

La estrategia no genera nuevas señales de trading. Abre una única posición larga al inicio con fines demostrativos y luego la protege moviendo el stop al punto de equilibrio una vez cumplidas las condiciones. Esto permite a los traders asegurar ganancias mientras dejan correr la operación.

## Detalles

- **Criterios de entrada**: Se abre una posición larga al inicio. No se utilizan señales adicionales.
- **Largo/Corto**: Soporta ambos, pero el ejemplo abre una posición larga.
- **Criterios de salida**: La posición sale cuando se activa la orden stop en el precio de entrada.
- **Stops**: El stop-loss se mueve al precio de entrada cuando se alcanza `MoveSlPrice`.
- **Valores predeterminados**:
  - `MoveSlPrice` = 0 (debe ajustarse antes de ejecutar).
  - `CandleType` = marco temporal de 1 minuto.
- **Filtros**:
  - Categoría: Gestión de riesgos
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Simple
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
