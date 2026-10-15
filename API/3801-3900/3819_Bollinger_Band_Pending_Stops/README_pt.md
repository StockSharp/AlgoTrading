# Bollinger Estratégia de paradas pendentes de banda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Este exemplo converte o consultor especialista MQL "Bb_0_1" original no StockSharp API de alto nível. A estratégia escuta uma assinatura de vela e usa bandas Bollinger para delimitar o preço atual. Quando o mercado fica entre as bandas superior e inferior, o algoritmo coloca três ordens de stop de compra em camadas acima do preço e três ordens de stop de venda em camadas abaixo do preço. Cada camada é configurada com distâncias de take-profit individuais enquanto compartilha a mesma referência de stop obtida da banda oposta.

## Lógica de negociação
- Assine o prazo configurado e calcule Bollinger Bandas com período e desvio solicitado.
- Dentro da janela de negociação (`StartHour` < hora < `EndHour`) e enquanto o preço permanece entre as faixas, coloque ordens pendentes:
  - Três paradas de compra no nível da banda superior atual com lucros deslocados por `FirstTakeProfit`, `SecondTakeProfit` e `ThirdTakeProfit` etapas de preço acima da entrada.
  - Três paradas de venda no nível atual da banda inferior com lucros espelhados abaixo da entrada.
  - Todas as entradas herdam a banda oposta como parada protetora inicial.
- As ordens pendentes são automaticamente registradas novamente sempre que as bandas se aproximam do preço, para que as ordens sigam os envelopes do indicador.
- Depois que uma ordem stop é executada, a estratégia registra ordens explícitas de stop-loss e take-profit para o volume preenchido.
- A proteção de rastreamento é opcional: `UseBandTrailingStop` seleciona a banda oposta para rastreamento, caso contrário, a banda do meio (EMA) é usada. O stop só termina quando o fechamento ultrapassa o preço de entrada e o valor do indicador fornece um nível melhor.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Período usado para os cálculos da banda Bollinger. |
| `BandPeriod` | Quantidade de velas utilizadas pelas bandas. |
| `BandDeviation` | Multiplicador de desvio padrão para as bandas. |
| `Volume` | Volume de cada camada pendente. |
| `StartHour` / `EndHour` | Janela de negociação por hora (limites exclusivos). |
| `FirstTakeProfit`, `SecondTakeProfit`, `ThirdTakeProfit` | Distâncias de lucro expressas em etapas de preços para cada camada. |
| `UseBandTrailingStop` | Selecione a referência final: banda oposta (`true`) ou Bollinger linha média (`false`). |

## Notas de implementação
- O volume do pedido reflete o consultor especialista original usando um tamanho estático (`Volume`). O dimensionamento de posição baseado em risco do código MQL não é implementado porque o ambiente de amostra StockSharp não fornece histórico da conta.
- Os parâmetros de mudança do indicador do script MQL não são expostos porque o nível superior API já fornece valores alinhados para a vela atual.
- As ordens de proteção são ordens normais de stop e limite que são atualizadas sempre que as condições de trilha baseadas em banda melhoram o nível de stop.
