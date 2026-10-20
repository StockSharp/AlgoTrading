# BBSR Extreme

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Reentrada en las bandas con filtro de pendiente EMA y protección ATR. `C,L,U,E` son cierre original, banda inferior, superior y EMA; `p` es la vela formada anterior.

## Reglas de negociación

Largo: `C[p] < L[p] && C > L && E > E[p]`. Corto: `C[p] > U[p] && C < U && E < E[p]`. La igualdad no activa entradas. Solo se entra sin posición, por `Volume`, sin acumulación, inversión ni espera fija.

Se fija `A=ATR` de la vela de señal antes de enviar la entrada. La protección local nativa usa el precio realmente ejecutado: largo, stop/objetivo `fill-2A` / `fill+3A`; corto, `fill+2A` / `fill-3A`. Los multiplicadores son parámetros; el ATR posterior no mueve los niveles. Las suscripciones bid/ask permiten cerrar el volumen ejecutado entre velas con órdenes de mercado.

Las entradas requieren velas terminadas e indicadores preparados. La posición se gestiona hasta su salida; las órdenes de mercado pendientes impiden envíos duplicados.

Bollinger usa la desviación estándar poblacional de la ventana móvil de cierres, incluida la vela terminada actual.

## Parámetros

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `20` |
| `BollingerMultiplier` | `2` |
| `MaLength` | `7 (EMA)` |
| `AtrLength` | `14 (Wilder)` |
| `AtrStopMultiplier` | `2` |
| `AtrProfitMultiplier` | `3` |
| `CandleType` | `5m` |
