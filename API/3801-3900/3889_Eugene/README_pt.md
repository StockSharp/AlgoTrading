# Estratégia de Eugênio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo

A Estratégia Eugene transporta o consultor especialista MetaTrader 4 original "Eugene" para o StockSharp alto nível API. O algoritmo monitora velas horárias por padrão e procura rompimentos de velas internas que são confirmados por uma retração para um terço da vela anterior. Uma vez confirmado um rompimento, a estratégia entra na direção do rompimento e pode reverter as posições existentes quando uma configuração oposta aparecer.

## Lógica de negociação

1. **Detecção de vela interna** – a vela anterior deve estar completamente dentro do alcance da vela anterior. Sua direção de fechamento determina se ele é classificado como um insider preto (baixa) ou branco (alta).
2. **Filtros de pássaros** – uma vela interna confirmada por outra vela da mesma cor atrás dela é marcada como um "pássaro". Os pássaros pretos bloqueiam negociações longas, os pássaros brancos bloqueiam negociações curtas. Isso reflete o filtro de proteção da versão MQL.
3. **Níveis de confirmação em zigue-zague** – dois preços de confirmação são calculados em um terço do corpo da vela ou pavio anterior:
   - O nível de confirmação longo está um terço abaixo do fechamento anterior (corpo para velas de alta, pavio para velas de baixa).
   - O nível de confirmação de venda está um terço acima do fechamento anterior (corpo para velas de baixa, pavio para velas de alta).
4. **Filtro de sessão** – se a vela atual abrir às 08:00 ou mais tarde, as confirmações são consideradas satisfeitas mesmo sem retração.
5. **Condição de rompimento** – um sinal de compra exige que a vela atual atinja uma máxima mais alta do que a vela anterior, mantendo uma mínima mais alta e sobrepondo o intervalo da vela duas barras atrás. Um sinal de venda usa condições simétricas com mínimos e máximos mais baixos.
6. **Gerenciamento de posição** – antes de abrir uma nova negociação a estratégia fecha qualquer exposição oposta. Apenas uma entrada longa e uma entrada curta podem ser emitidas por vela, replicando as restrições `Counter_buy` e `Counter_sell` do consultor especialista original.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Trade Volume` | Tamanho do pedido para ordens de mercado. | `0.1` |
| `Candle Type` | Período da série de velas processadas. | `1 hour` |

## Gráficos

Quando uma área do gráfico está disponível, a estratégia traça as velas processadas juntamente com as negociações executadas, ajudando a visualizar o comportamento do rompimento.

## Notas

- A versão StockSharp mantém o filtro de sessão por hora do especialista MQL. Ajuste o tipo de vela ao negociar em outros mercados ou fusos horários.
- O gerenciamento de stop-loss e take-profit não está incluído no arquivo de origem MQL. A porta, portanto, deixa o gerenciamento de riscos para o ambiente de hospedagem.
