# Estrategia Parabolic SAR Bug5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La Estrategia Parabolic SAR Bug5 opera reversiones de precio detectadas por el indicador Parabolic SAR. Abre una posición larga cuando el precio cruza por encima del SAR y una posición corta cuando el precio cruza por debajo. La estrategia opcionalmente invierte la dirección de trading, cierra posiciones abiertas cuando el SAR cambia de lado, y admite stop trailing, toma de ganancias y reglas de stop-loss.

## Reglas de entrada

- **Comprar** cuando el precio cruza por encima del SAR y no hay posición larga abierta.
- **Vender** cuando el precio cruza por debajo del SAR y no hay posición corta abierta.
- Si `Reverse` está habilitado, las señales se invierten.

## Reglas de salida

- Cerrar posición cuando aparece la señal SAR opuesta si `SarClose` está habilitado.
- Aplicar objetivos fijos de stop-loss y toma de ganancias.
- Si `Trailing` está habilitado, el stop-loss sigue el precio más alto (para largos) o más bajo (para cortos) desde la entrada.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Step` | Factor de aceleración inicial para Parabolic SAR. |
| `Maximum` | Factor de aceleración máximo para Parabolic SAR. |
| `StopLossPoints` | Distancia del stop-loss en puntos. |
| `TakeProfitPoints` | Distancia de la toma de ganancias en puntos. |
| `Trailing` | Habilitar gestión de stop trailing. |
| `TrailPoints` | Distancia del stop trailing en puntos. |
| `Reverse` | Invertir dirección de trading. |
| `SarClose` | Cerrar posición al cambio de SAR. |
| `CandleType` | Marco temporal de las velas a procesar. |
