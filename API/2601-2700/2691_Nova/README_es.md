# Estrategia Nova
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
- Conversión del asesor experto "Nova" de MetaTrader 5 que monitorea el momentum del precio en un número fijo de segundos.
- Funciona con cualquier tipo de vela elegida a través del parámetro `CandleType` y evalúa la lógica solo en velas completadas.
- Rastrea los mejores precios de compra y venta usando datos Level1 y almacena sus valores de `SecondsAgo` segundos antes.
- Entra en una posición **larga** cuando la vela anterior es alcista y el ask actual es mayor que el ask almacenado por al menos `StepPips`.
- Entra en una posición **corta** cuando la vela anterior es bajista y el bid actual es menor que el ask almacenado por al menos `StepPips`.
- Aplica niveles automáticos de stop-loss y take-profit usando protección de StockSharp si los parámetros correspondientes son mayores que cero.
- Después de una pérdida (activación del stop-loss), el volumen del siguiente trade se multiplica por `LossCoefficient`; después de una salida rentable el volumen se resetea a `BaseVolume`.

## Parámetros
- `SecondsAgo` – número de segundos entre la instantánea del precio de referencia y el momento de evaluación actual.
- `StepPips` – filtro de ruptura en pips; convertido a unidades de precio usando el paso de precio de la seguridad (los instrumentos de 3/5 decimales se ajustan por ×10).
- `BaseVolume` – tamaño inicial del trade; normalizado al paso de volumen del intercambio y límites mín/máx.
- `StopLossPips` – distancia en pips para el stop-loss de protección (0 lo deshabilita).
- `TakeProfitPips` – distancia en pips para el take-profit de protección (0 lo deshabilita).
- `LossCoefficient` – multiplicador aplicado al último volumen ejecutado después de un trade perdedor.
- `CandleType` – fuente de velas usada para señales (marco temporal, tick, rango, etc.).

## Notas Adicionales
- La estrategia requiere datos Level1 (mejor bid/ask) para replicar el comportamiento original de MT5; las velas proporcionan un respaldo usando su precio de cierre cuando Level1 no está disponible.
- El recálculo de volumen respeta `Security.VolumeStep`, `Security.MinVolume` y `Security.MaxVolume` para evitar órdenes inválidas.
- Las conversiones de precio dependen de `Security.PriceStep` y `Security.Decimals` para que la estrategia se adapte tanto a símbolos forex de 4/5 dígitos como a otros instrumentos.
- No se proporciona versión en Python para esta estrategia.
