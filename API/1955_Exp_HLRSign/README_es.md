# Estrategia Exp HLRSign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa la lógica del indicador HLRSign en StockSharp.
Abre y cierra posiciones cuando la Relación Alto-Bajo (HLR) cruza niveles predefinidos.

## Cómo Funciona

- Calcula los valores del Canal Donchian sobre un rango configurable.
- Calcula el valor HLR como la posición porcentual del precio medio dentro del canal.
- Genera señales de compra o venta cuando el HLR cruza los umbrales superior o inferior dependiendo del modo seleccionado:
  - **ModeIn** – comprar al cruzar por encima del nivel superior y vender al cruzar por debajo del nivel inferior.
  - **ModeOut** – vender al cruzar por debajo del nivel superior y comprar al cruzar por encima del nivel inferior.
- Permite habilitar o deshabilitar la apertura y el cierre de posiciones largas y cortas por separado.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `Mode` | Modo de operación del indicador (ModeIn o ModeOut). |
| `Range` | Período de retroceso para los precios máximos y mínimos. |
| `UpLevel` | Umbral superior en porcentaje para el HLR. |
| `DnLevel` | Umbral inferior en porcentaje para el HLR. |
| `CandleType` | Marco temporal de las velas usadas para los cálculos. |
| `BuyOpen` | Permitir abrir posiciones largas. |
| `SellOpen` | Permitir abrir posiciones cortas. |
| `BuyClose` | Permitir cerrar posiciones largas. |
| `SellClose` | Permitir cerrar posiciones cortas. |

## Notas

- La estrategia usa la API de alto nivel con el indicador `DonchianChannels`.
- Procesa solo velas cerradas y verifica los permisos de posición antes de operar.
- No se definen niveles de stop-loss ni take-profit; la protección de posiciones puede añadirse manualmente.
