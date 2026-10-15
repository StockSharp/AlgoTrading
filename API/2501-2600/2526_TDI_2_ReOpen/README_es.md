# 2526 Estrategia TDI-2 ReOpen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión en C# del asesor experto de MetaTrader 5 **Exp_TDI-2_ReOpen**. Opera usando el indicador Trend Direction Index (TDI-2) y aplica la lógica original de re-entrada en posiciones. El port en C# usa la API de alto nivel de StockSharp y mantiene el comportamiento central de la versión MQL: reacciona a los cruces entre la línea de momentum TDI y la línea de índice TDI, escala en posiciones rentables después de un avance de precio configurable, y gestiona operaciones con stops protectores opcionales.

## Indicadores
- **Indicador TDI-2** – un indicador personalizado basado en momentum implementado en este repositorio. Construye dos líneas:
  - *Línea direccional*: `Período × MomentumSuavizado`, donde el momentum es igual al precio aplicado menos el precio `Período` barras atrás.
  - *Línea de índice*: `|Direccional| − (2 × Período × Suavizado(|Momentum|, 2×Período) − |Momentum|)`.
- El indicador soporta los siguientes métodos de suavizado: media móvil Simple, Exponencial, Suavizada (RMA) y Linealmente Ponderada.
- Las opciones de precio aplicado soportadas replican la implementación MQL original, incluyendo las fórmulas TrendFollow y Demark.

## Lógica de trading
1. En cada vela terminada, la estrategia evalúa los valores TDI-2 en la barra especificada por **Signal Bar** (por defecto: la vela anterior cerrada) y una barra antes.
2. Cuando la línea direccional estaba por encima de la línea de índice y luego la cruza hacia abajo:
   - Si **Allow Long Entries** está habilitado y no hay posición larga activa, la estrategia prepara una nueva entrada larga.
   - Si existe una posición corta y **Allow Short Exits** está habilitado, cierra la posición corta.
3. Cuando la línea direccional estaba por debajo de la línea de índice y luego la cruza hacia arriba:
   - Si **Allow Short Entries** está habilitado y no hay posición corta activa, la estrategia prepara una nueva entrada corta.
   - Si existe una posición larga y **Allow Long Exits** está habilitado, cierra la posición larga.
4. Lógica de re-entrada (escalado):
   - Mientras se mantiene una posición larga, la estrategia rastrea el precio de ejecución de la última operación larga. Si el mercado se mueve a favor en **Re-entry Step (points)** y el número de operaciones largas ejecutadas sigue siendo inferior a **Max Entries**, abre una orden larga adicional con el volumen base.
   - La misma lógica aplica a posiciones cortas usando el precio de ejecución corta más reciente.
5. Al abrir una posición mientras existe una posición contraria, la estrategia envía una orden de mercado combinada dimensionada tanto para cerrar la exposición contraria como para establecer la nueva posición con el volumen base configurado.
6. Los niveles opcionales de stop-loss y take-profit se activan a través de `StartProtection` usando el multiplicador `PriceStep` del instrumento.

## Parámetros
| Nombre | Descripción | Valor predeterminado |
| --- | --- | --- |
| Money Management | Volumen de orden base. | 0.1 |
| Max Entries | Número máximo de entradas por dirección (operación inicial + re-entradas). | 10 |
| Stop Loss (points) | Distancia del stop-loss en puntos del instrumento. | 1000 |
| Take Profit (points) | Distancia del take-profit en puntos del instrumento. | 2000 |
| Slippage (points) | Conservado por compatibilidad; no se usa en la implementación de StockSharp. | 10 |
| Re-entry Step (points) | Movimiento mínimo favorable antes de escalar en una posición existente. | 300 |
| Allow Long Entries / Allow Short Entries | Habilitar apertura de posiciones largas/cortas. | true |
| Allow Long Exits / Allow Short Exits | Habilitar cierre de posiciones largas/cortas. | true |
| Candle Type | Serie de velas usada para los cálculos. | Velas H4 |
| TDI Smoothing | Método de suavizado para el indicador TDI-2. | MA Simple |
| TDI Period | Período de retroceso del momentum. | 20 |
| TDI Phase | Reservado por compatibilidad con el input MQL (sin efecto en los modos de suavizado soportados). | 15 |
| Applied Price | Fuente de precio usada por TDI-2. | Close |
| Signal Bar | Número de velas cerradas a mirar atrás al evaluar cruces. | 1 |

## Notas adicionales
- Solo se implementan los métodos de suavizado soportados por los indicadores de StockSharp (SMA, EMA, SMMA, LWMA). Otros modos MQL como JJMA o T3 no están disponibles.
- El parámetro **TDI Phase** se mantiene por completitud. No influye en los métodos de suavizado soportados y puede dejarse en su valor predeterminado.
- El parámetro **Slippage (points)** se proporciona por paridad con el asesor experto original pero no es usado por la API de alto nivel.
- Los contadores de re-entrada se reinician automáticamente cuando la posición neta vuelve a cero.
