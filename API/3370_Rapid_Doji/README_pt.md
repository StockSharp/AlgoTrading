# Estratégia Doji Rápida
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Rapid Doji replica a lógica do consultor especialista "Rapid Doji EA" original. Ele verifica velas concluídas de um período configurável (diariamente por padrão) e coloca ordens de entrada de parada acima e abaixo de cada vela doji. Os stops de proteção são posicionados usando um multiplicador Average True Range (ATR), enquanto um trailing stop auxiliar mantém a distância de risco fixa em pontos brutos depois que uma posição se torna lucrativa.

## Lógica de negociação

1. **Assinatura de dados** – a estratégia escuta velas finalizadas do período selecionado e mantém um indicador ATR com período configurável.
2. **Detecção de Doji** – uma vela é tratada como doji quando o tamanho absoluto do corpo é no máximo 3% da faixa completa da vela. Apenas velas concluídas são avaliadas.
3. **Colocação de pedido** – quando um doji válido é encontrado:
   - Uma ordem de compra stop é colocada na máxima doji.
   - Uma ordem de stop de venda é colocada na mínima doji.
   - Cada entrada lembra um preço de parada de proteção igual ao extremo oposto menos/mais ATR × multiplicador.
4. **Gerenciamento de risco** – assim que uma posição é aberta, a ordem pendente restante é cancelada, o stop memorizado é registrado como um stop de proteção e a lógica móvel assume o controle.
5. **Trailing stop** – em cada nova vela o nível de stop é movido para manter uma distância fixa (em pontos convertidos através da etapa de preço do instrumento) do último preço de fechamento, mas somente quando a posição já for lucrativa.

A estratégia nunca utiliza metas de lucro; as saídas acontecem através da parada de proteção ou intervenção manual.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Tipo de dados Candle usado para detecção de padrões (período diário por padrão). |
| `AtrPeriod` | Comprimento de lookback do indicador ATR. |
| `AtrMultiplier` | Multiplicador aplicado ao valor ATR para cálculo de stop loss. |
| `TrailingDistancePoints` | Distância fixa em pontos brutos usados ao rastrear a parada. |

Todos os parâmetros suportam otimização dentro do ambiente StockSharp.

## Notas de implementação

- O código depende da assinatura de vela de alto nível API (`SubscribeCandles`) combinada com a vinculação do indicador (`Bind`) para evitar o tratamento manual do histórico.
- Os pedidos são normalizados por meio de `Security.ShrinkPrice` para respeitar o tamanho do tick da exchange.
- As paradas de proteção são gerenciadas explicitamente para imitar o comportamento do consultor especialista MetaTrader original.
- O projeto omite intencionalmente uma implementação Python de acordo com os requisitos da tarefa.
