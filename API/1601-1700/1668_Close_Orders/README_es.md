# Estrategia de Cierre de Órdenes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utilitaria cierra inmediatamente posiciones existentes y cancela órdenes pendientes según filtros definidos por el usuario. Puede operar solo sobre el instrumento adjunto o sobre todos los instrumentos de la cartera. Las restricciones opcionales de ventana de tiempo y rango de precio permiten un control preciso sobre qué órdenes se ven afectadas.

## Detalles

- **Propósito**: gestión de riesgos y liquidación manual.
- **Operación**:
  - Al inicio, la estrategia verifica la ventana de tiempo opcional.
  - Si está permitido, cierra posiciones y cancela órdenes que coincidan con los filtros.
  - Tras el procesamiento, la estrategia se detiene automáticamente.
- **Filtros**:
  - `CloseAllSecurities` – incluir todos los instrumentos de la cartera en lugar de solo el instrumento adjunto.
  - `CloseOpenLongOrders` / `CloseOpenShortOrders` – cerrar posiciones largas o cortas existentes.
  - `ClosePendingLongOrders` / `ClosePendingShortOrders` – cancelar órdenes de compra o venta pendientes.
  - `SpecificOrderId` – solo afectar a órdenes con el id de transacción indicado cuando sea distinto de cero.
  - `CloseOrdersWithinRange`, `CloseRangeHigh`, `CloseRangeLow` – limitar por rango de precio de entrada.
  - `EnableTimeControl`, `StartCloseTime`, `StopCloseTime` – aplicar solo durante una ventana de tiempo específica.
- **Valores predeterminados**:
  - Todas las opciones de cierre habilitadas.
  - `SpecificOrderId` = 0.
  - `CloseOrdersWithinRange` = false.
  - `CloseRangeHigh` = 0.
  - `CloseRangeLow` = 0.
  - `EnableTimeControl` = false.
  - `StartCloseTime` = 02:00.
  - `StopCloseTime` = 02:30.
- **Notas**:
  - La estrategia no abre nuevas posiciones.
  - Los filtros de rango de precio se ignoran cuando los límites son cero o negativos.
  - Cuando `CloseAllSecurities` está habilitado, se procesan las posiciones de toda la cartera.
