# Estratégia de Tendência T3MA Alarm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica a ideia do indicador T3MA-ALARM. Aplica uma média móvel exponencial duplamente suavizada para detectar mudanças na direção da tendência.

Quando a média móvel suavizada vira para cima, abre uma posição comprada. Quando vira para baixo, abre uma posição vendida. Opcionalmente, um sinal oposto pode fechar a posição atual. Os níveis de stop loss e take profit são definidos como distâncias absolutas de preço a partir do preço de entrada.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `MaPeriod` | Período da média móvel exponencial. |
| `MaShift` | Número de barras utilizadas para detectar a mudança de direção. |
| `StopLoss` | Distância de preço para o stop loss de proteção. Defina `0` para desabilitar. |
| `TakeProfit` | Distância de preço para o take profit. Defina `0` para desabilitar. |
| `ReverseOnSignal` | Fechar uma posição oposta quando um novo sinal aparecer. |
| `CandleType` | Tipo de vela utilizada para os cálculos. |

## Sinais

* **Compra** – a direção da MA suavizada muda de baixa para alta.
* **Venda** – a direção da MA suavizada muda de alta para baixa.

As posições são fechadas por um sinal oposto (quando habilitado) ou quando os níveis de stop loss / take profit são atingidos.
