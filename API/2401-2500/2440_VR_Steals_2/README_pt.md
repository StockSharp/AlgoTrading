# Estratégia VR Steals 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão para StockSharp do expert de MetaTrader 5 "VR---STEALS-2". Abre uma única posição comprada e demonstra gestão simples de posições sem indicadores.

## Como funciona
1. No início, a estratégia compra usando `BuyMarket` e regista o preço de entrada.
2. Dados de velas (1 minuto por padrão) são subscritos via `SubscribeCandles`.
3. Para cada vela concluída:
   - Quando o preço se moveu `Breakeven` passos a favor da operação, o nível de stop é movido acima da entrada em `BreakevenOffset` passos.
   - Se o preço atingir a entrada mais `TakeProfit` passos, a posição é fechada.
   - Se o preço cair para o nível de stop (inicial `StopLoss` abaixo da entrada ou o stop de break-even movido), a posição é fechada.
4. Após a saída, a estratégia não abre novas posições.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| TakeProfit | Distância em passos de preço até ao nível de take-profit. | 50 |
| StopLoss | Distância inicial do stop em passos de preço. | 50 |
| Breakeven | Lucro em passos necessário para ativar o stop de break-even. | 20 |
| BreakevenOffset | Deslocamento acima da entrada quando o stop de break-even é definido. | 9 |
| CandleType | Tipo de vela usado para processamento de preços. | Período temporal de 1 minuto |

`StartProtection()` é usado para ativar a proteção integrada de posições.
