# Estrategia de Tendencia KWAN RDP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión StockSharp del experto MetaTrader `Exp_KWAN_RDP`. La lógica calcula el oscilador KWAN RDP combinando tres indicadores estándar y suavizando su producto:

1. **DeMarker** — mide la relación entre máximos y mínimos recientes para evaluar el agotamiento del momentum.
2. **Money Flow Index** — evalúa precio y volumen para detectar condiciones de sobrecompra o sobreventa.
3. **Momentum** — captura la velocidad de los cambios de precio usando el período seleccionado.
4. El valor bruto `100 * DeMarker * MFI / Momentum` se suaviza con una media móvil configurable (SMA, EMA, SMMA, WMA o Jurik).

La pendiente del oscilador suavizado produce señales de trading:

- **Giro alcista (pendiente ascendente)**: cerrar posiciones cortas y opcionalmente abrir una posición larga.
- **Giro bajista (pendiente descendente)**: cerrar posiciones largas y opcionalmente abrir una posición corta.
- Las barras neutrales (pendiente plana) no desencadenan acciones.

## Parámetros

- `CandleType` — serie de velas para los cálculos de indicadores (predeterminado: marco temporal H1).
- `DeMarkerPeriod` — período del indicador DeMarker.
- `MfiPeriod` — período del Money Flow Index.
- `MomentumPeriod` — período del indicador Momentum.
- `SmoothingLength` — longitud de la media móvil de suavizado.
- `Smoothing` — método de suavizado (Simple, Exponential, Smoothed, Weighted, Jurik).
- `EnableLongEntries` / `EnableShortEntries` — permite abrir posiciones largas o cortas.
- `CloseLongsOnReverse` / `CloseShortsOnReverse` — cerrar posiciones opuestas cuando aparece una señal de reversión.
- `TakeProfitPercent` / `StopLossPercent` — protección opcional basada en porcentaje aplicada a través de `StartProtection`.

## Reglas de trading

1. Suscribirse a la serie de velas configurada y calcular DeMarker, MFI, Momentum y el valor KWAN suavizado en cada vela terminada.
2. Detectar la dirección de la pendiente del último valor del oscilador frente al anterior.
3. Cuando la pendiente sube, cerrar cortos (si está habilitado) y abrir un largo si el trading largo está permitido y no hay posición larga activa.
4. Cuando la pendiente baja, cerrar largos (si está habilitado) y abrir un corto si el trading corto está permitido y no hay posición corta activa.
5. Usar los porcentajes opcionales de stop-loss y take-profit para proteger las posiciones con protección de la plataforma.

## Notas

- Las señales solo se procesan en velas completadas para evitar el ruido intrabarra.
- El cálculo de DeMarker utiliza suavizado interno para coincidir con la implementación de MetaTrader.
- Todos los comentarios en el código C# están escritos en inglés según las directrices del proyecto.
