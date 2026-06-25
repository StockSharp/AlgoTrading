# Estrategia Maximus vX Lite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Conversión del asesor experto de MetaTrader 5 "maximus_vX lite" a la API de alto nivel de StockSharp.
La estrategia busca zonas de consolidación por encima y por debajo del precio actual y espera a que el precio se mueva un número
configurable de puntos alejándose de esas zonas antes de entrar. El tamaño de la posición se determina a partir de un presupuesto
de porcentaje de riesgo opcional, y el beneficio flotante puede desencadenar una liquidación forzada de toda la exposición abierta.

## Lógica de la estrategia

1. **Escaneo histórico** – en cada vela finalizada la estrategia mantiene hasta `HistoryDepth` velas y usa una ventana deslizante
   `RangeLookback` para detectar máximos y mínimos compactos que forman áreas de consolidación.
2. **Canal superior** – cuando se detecta un bloque superior válido, el canal se ancla alrededor del cierre actual con un ancho
   de `RangePoints`. Si ningún bloque histórico cumple los requisitos, el canal recurre al mismo ancho ajustado al precio actual.
3. **Canal inferior** – el bloque inferior se toma directamente de los máximos/mínimos históricos que satisfacen las condiciones
   de rango o, si no existen, de un nivel sintético alrededor del cierre actual menos `RangePoints`.
4. **Entradas largas** – se permiten dos configuraciones largas:
   - Ruptura por encima de la consolidación inferior: el precio debe superar `_lowerMax` por `DistancePoints` y el canal superior
     debe estar disponible. El take profit usa dos tercios de la distancia entre `_lowerMax` y `_upperMin`, con un mínimo igual a `RangePoints`.
   - Ruptura por encima del canal superior: el precio debe superar `_upperMax` por `DistancePoints`. El take profit se establece en `2 * RangePoints`.
5. **Entradas cortas** – la lógica simétrica se dispara cuando el precio cae por debajo de `_upperMin` o `_lowerMin` por `DistancePoints`.
   La configuración corta primaria también usa el objetivo dinámico de dos tercios, mientras que la secundaria usa `2 * RangePoints`.
6. **Stops y salidas** – `StopLossPoints` define un stop protector fijo cuando es mayor que cero. `MinProfitPercent` monitorea el capital
   flotante frente al último balance plano y cierra todas las posiciones una vez superado el umbral. Las comprobaciones manuales de stop/objetivo
   emulan el comportamiento del asesor experto original dentro de la estrategia.
7. **Dimensionamiento de posición** – cuando `RiskPercent` es mayor que cero y hay un stop definido, el volumen de la orden se calcula a partir
   del valor del portafolio y la distancia del stop. De lo contrario, la estrategia reutiliza la propiedad `Volume`.

## Parámetros

- `DelayOpen` (predeterminado `2`) – número de barras del período de tiempo durante las cuales se permite añadir al mismo lado.
- `DistancePoints` (predeterminado `850`) – distancia mínima desde el borde de consolidación antes de entrar.
- `RangePoints` (predeterminado `500`) – ancho de las cajas de consolidación.
- `HistoryDepth` (predeterminado `1000`) – número de velas guardadas en memoria para escaneos históricos.
- `RangeLookback` (predeterminado `40`) – longitud de ventana usada para calcular máximos y mínimos locales.
- `CandleType` (predeterminado `TimeSpan.FromMinutes(15).TimeFrame()`) – período de tiempo usado para los cálculos.
- `RiskPercent` (predeterminado `5m`) – porcentaje del valor del portafolio arriesgado por operación. Poner en cero para usar volumen fijo.
- `StopLossPoints` (predeterminado `1000`) – distancia del stop protector; cero deshabilita el stop.
- `MinProfitPercent` (predeterminado `1m`) – porcentaje de beneficio flotante que fuerza el cierre de todas las posiciones.

## Detalles

- **Largo/Corto**: Ambas direcciones
- **Criterios de salida**: Stop fijo o take profit, bloqueo de capital mediante `MinProfitPercent`
- **Stops**: Stop fijo opcional desde `StopLossPoints`
- **Indicadores**: Ninguno (precio puro con análisis de ventana deslizante)
- **Marco temporal**: Configurable mediante `CandleType` (predeterminado 15 minutos)
- **Complejidad**: Intermedio (combina escaneo de historial, objetivos dinámicos y dimensionamiento de riesgo)
- **Nivel de riesgo**: Alto cuando se usa porcentaje de riesgo debido a la naturaleza de ruptura
