# Estrategia de Cierre por Beneficio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia monitorea la ganancia y pérdida realizada de todas las operaciones ejecutadas por la estrategia. Cuando el beneficio acumulado supera un umbral definido por el usuario, cierra inmediatamente cualquier posición abierta y opcionalmente cancela las órdenes activas. El mismo comportamiento puede activarse para la caída estableciendo un límite de pérdida.

La estrategia no analiza indicadores ni movimientos de precio. En cambio, actúa como una capa protectora que sale del mercado una vez que se alcanza un objetivo monetario o un nivel de stop. Una suscripción simple a velas se usa únicamente para proporcionar comprobaciones periódicas del valor PnL actual.

## Parámetros

- **UseProfitToClose** – habilitar o deshabilitar el cierre por objetivo de beneficio. Por defecto: `true`.
- **ProfitToClose** – valor de beneficio en unidades de moneda que activa una salida completa. Por defecto: `20`.
- **UseLossToClose** – habilitar o deshabilitar el cierre por límite de pérdida. Por defecto: `false`.
- **LossToClose** – valor de pérdida en unidades de moneda que activa una salida completa cuando se supera. Por defecto: `100`.
- **ClosePendingOrders** – cancelar todas las órdenes activas al cerrar posiciones. Por defecto: `true`.
- **CandleType** – tipo de velas utilizadas para activar comprobaciones periódicas. Por defecto: marco temporal de `1` minuto.

## Lógica de Trading

1. Suscribirse a las velas del marco temporal seleccionado.
2. En cada vela terminada, calcular el PnL realizado actual.
3. Si el beneficio es mayor o igual a `ProfitToClose`, cerrar toda la posición y opcionalmente cancelar órdenes pendientes.
4. Si el monitoreo de pérdidas está habilitado y el PnL actual es menor o igual a `-LossToClose`, cerrar toda la posición y opcionalmente cancelar órdenes pendientes.

## Notas Adicionales

- La estrategia cierra solo la posición del valor al que está vinculada.
- Las órdenes pendientes se cancelan usando el método integrado `CancelActiveOrders`.
- La lógica puede combinarse con otras estrategias de entrada para implementar toma de ganancias o protección de cartera.

## Filtros

- Categoría: Gestión de riesgo
- Dirección: Ambos
- Indicadores: Ninguno
- Stops: Sí
- Complejidad: Básico
- Marco temporal: Cualquiera
- Estacionalidad: No
- Redes neuronales: No
- Divergencia: No
- Nivel de riesgo: Medio
