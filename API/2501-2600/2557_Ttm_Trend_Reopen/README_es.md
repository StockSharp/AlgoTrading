# Estrategia TTM Trend de Reapertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia recrea la lógica del asesor experto de MetaTrader *Exp_ttm-trend_ReOpen*. Traslada el indicador TTM Trend al framework de StockSharp, usa el suavizado Heikin-Ashi para colorear velas y opera cuando el color cambia de bajista a alcista o viceversa. Cada cambio de color representa un cambio de régimen en la compresión/expansión de volatilidad, por lo que el bot cierra inmediatamente cualquier exposición opuesta y abre una posición en la nueva dirección.

## Lógica del indicador
El indicador original colorea cada barra según tanto el cuerpo Heikin-Ashi como la vela OHLC clásica:

- **Verde brillante (4)** – El cierre Heikin-Ashi está por encima de su apertura y la vela estándar cierra más alto de lo que abre.
- **Azul verdoso (3)** – Heikin-Ashi es alcista pero la vela bruta cierra más bajo.
- **Rosa intenso (0)** – Heikin-Ashi es bajista y la vela bruta cierra más bajo.
- **Púrpura (1)** – Heikin-Ashi es bajista mientras la vela bruta cierra más alto.
- **Gris (2)** – Fallback neutro si la tendencia no puede determinarse.

Para imitar el suavizado del buffer de MetaTrader, el indicador mantiene una ventana rodante (`CompBars`) de valores Heikin-Ashi anteriores. Si el último cuerpo permanece dentro del rango alto/bajo de cualquier vela almacenada, se reutiliza el color anterior. Esto previene whipsaws durante pequeños retrocesos, igual que la implementación fuente.

## Reglas de trading
1. Suscribirse al marco temporal configurado por `CandleType` y evaluar solo velas finalizadas (`SignalBar` selecciona cuántas barras cerradas observar desde el último punto histórico).
2. Cuando aparece un **color alcista** (valores 1 o 4) y la señal anterior no era alcista:
   - Cerrar cualquier corto si `EnableShortExits` está activo.
   - Abrir una posición larga (o girar de corto a largo) si `EnableLongEntries` es verdadero.
3. Cuando aparece un **color bajista** (valores 0 o 3) y la señal anterior no era bajista:
   - Cerrar cualquier largo si `EnableLongExits` está activo.
   - Abrir una posición corta (o girar de largo a corto) si `EnableShortEntries` es verdadero.
4. Cada lado puede piramidear volumen adicional cuando el precio se mueve a favor de la operación por al menos `PriceStepPoints` (convertidos a precio usando el `PriceStep` del instrumento). El número acumulado de entradas por dirección está limitado por `MaxPositions`.

## Comportamiento de pirámide
- `PriceStepPoints` refleja el input "PriceStep" de MetaTrader: una vez que el beneficio no realizado excede esta distancia desde el precio medio de entrada, el bot añade el `Volume` base nuevamente.
- `MaxPositions` limita el recuento total de entradas apiladas, incluyendo la operación inicial. Establecer en `1` para deshabilitar las reentradas completamente.

## Gestión de riesgo
`StopLossPoints` y `TakeProfitPoints` se miden en puntos del instrumento, igual que en el EA original. Se transforman en distancias de precio absolutas via `Security.PriceStep` y se aplican a través de `StartProtection`. Establecer cualquiera de los parámetros en cero para deshabilitar la protección respectiva.

## Parámetros
- `CandleType` – marco temporal usado para el cálculo de TTM Trend (por defecto: velas de 4 horas).
- `CompBars` – número de velas Heikin-Ashi históricas mantenidas para el suavizado de color (por defecto: 6).
- `SignalBar` – número de barras atrás desde la última vela finalizada a evaluar (por defecto: 1 → última barra cerrada).
- `PriceStepPoints` – movimiento favorable mínimo, en puntos, requerido antes de piramidear (por defecto: 300).
- `MaxPositions` – número máximo de entradas acumuladas por dirección (por defecto: 10).
- `EnableLongEntries` / `EnableShortEntries` – activar/desactivar aperturas largas/cortas en cambios de color.
- `EnableLongExits` / `EnableShortExits` – activar/desactivar salidas forzadas cuando aparece el color opuesto.
- `StopLossPoints` – distancia del stop protector en puntos (por defecto: 1000).
- `TakeProfitPoints` – distancia del objetivo de ganancia en puntos (por defecto: 2000).

## Notas de uso
- La lógica de color TTM Trend es sensible al marco temporal elegido; el sistema original usaba el gráfico H4, pero puede suministrarse cualquier `CandleType`.
- Dado que el indicador trabaja con cuerpos Heikin-Ashi, los gaps repentinos pueden no activar un cambio de color inmediatamente—esperar a la siguiente vela finalizada para confirmar.
- Establecer `PriceStepPoints` en cero si se desea un sistema de entrada única que nunca piramidee.
