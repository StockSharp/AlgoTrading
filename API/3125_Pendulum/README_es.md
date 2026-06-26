# Estrategia de Pendulum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema martingala basado en grid que oscila entre dos umbrales de precio. La estrategia abre una posición larga cuando el precio alcanza el límite superior del grid y gira a una posición corta con mayor volumen cuando el precio se mueve al límite inferior. Continúa alternando direcciones (hasta un número configurable de capas) mientras expande los objetivos y reduce las distancias de protección según el asesor experto original Pendulum. Después de tomar beneficio, el motor reinicia el grid y programa una nueva entrada en el mismo nivel para mantener el movimiento pendular.

## Detalles

- **Lógica de entrada**
  - Alinea el grid con el precio de cierre de la vela usando el `StepSize` configurado.
  - **Activación del límite superior** → abre una posición larga con el volumen base.
  - **Activación del límite inferior** → abre una posición corta con el volumen base.
  - Cuando la posición activa se mueve al activador opuesto, la estrategia revierte la dirección, multiplica el volumen absoluto por `Multiplier` y actualiza las distancias de take-profit / stop-loss como la versión MQL.
  - Las reentradas se programan después de salidas rentables para que la siguiente vela pueda volver a abrir en el mismo nivel de grid una vez procesadas las órdenes de cierre.
- **Lógica de salida**
  - Cada capa define un take-profit dedicado: un paso para la primera capa, `Multiplier` pasos para cada capa subsiguiente.
  - Los stops de protección reflejan la lógica MQL: la primera capa usa un stop amplio (`StepSize * Multiplier`), las capas subsiguientes usan un stop de un paso contra la nueva dirección.
  - Cuando se alcanza el número máximo de capas, la estrategia espera el take-profit o stop-loss antes de reiniciar.
- **Gestión de posición**
  - Usa netting: el port de StockSharp cierra y revierte la posición agregada en lugar de mantener largos y cortos cubiertos. Esto preserva la exposición del asesor original mientras permanece compatible con los portfolios de StockSharp.
  - El volumen se redondea al paso de volumen del instrumento cuando está disponible.
- **Datos**
  - Funciona con cualquier símbolo y marco temporal. La suscripción por defecto usa velas de 1 minuto y depende de los precios de cierre de las velas para las verificaciones del grid.
- **Protección integrada**
  - `StartProtection()` está habilitado para proteger posiciones inesperadas dejadas después de desconexiones o intervención manual.

## Parámetros

| Parámetro | Por defecto | Descripción |
|-----------|-------------|-------------|
| `StepSize` | `0.001` | Distancia entre niveles de grid. El grid siempre se ajusta a múltiplos de este valor. |
| `Multiplier` | `2` | Multiplica tanto el volumen del trade como los objetivos extendidos cuando la dirección gira a una nueva capa. Debe ser mayor que 1. |
| `MaxLayers` | `3` | Número máximo de capas martingala antes de que la estrategia deje de agregar nuevas reversiones. |
| `BaseVolume` | `1` | Tamaño base del trade usado para la primera capa. Las capas posteriores escalan por `Multiplier`. |
| `CandleType` | `1 Minute TimeFrame` | Tipo de vela usado para la suscripción. Puede cambiarse a cualquier otro marco temporal soportado por la fuente de datos. |

## Notas

- La estrategia recrea el comportamiento de `Pendulum.mq5` sin depender de posiciones cubiertas. Como StockSharp consolida la exposición, la posición neta se revierte para emular los grids MQL.
- Las completaciones de take-profit activan una orden diferida para que la siguiente vela pueda volver a abrir inmediatamente en el mismo nivel de precio una vez procesada la operación de cierre.
- Mantener el tamaño de paso configurado alineado con el paso de precio del instrumento para evitar redondeos excesivos de los niveles del grid.
