# Estratégia de filtro iCHO Trend CCIDualOnMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma porta StockSharp de alto nível do consultor especialista MetaTrader **"iCHO Trend CCIDualOnMA Filter"**. Ele combina um filtro de regime de linha zero do oscilador Chaikin com uma confirmação dupla do índice de canal de commodities (CCI) que é calculada sobre uma série de preços suavizados. O resultado é uma abordagem de acompanhamento de tendências que reage às mudanças de impulso, mas ainda requer uma confirmação de impulso do par CCI antes de entrar em uma negociação.

## Lógica de negociação

1. **Núcleo do oscilador Chaikin** – a linha de acumulação/distribuição é suavizada por duas médias móveis configuráveis. A diferença deles replica o oscilador Chaikin. Cruzamentos acima/abaixo de zero sinalizam uma mudança no fluxo de capital dominante.
2. **Filtro CCI duplo** – ambas as instâncias CCI usam a mesma entrada de preço suavizado por média móvel, mas diferentes períodos de lookback. Uma configuração longa requer que o CCI rápido se recupere do território negativo e cruze acima do CCI lento enquanto o oscilador Chaikin permanece acima de zero. Uma breve configuração reflete essas condições.
3. **Reversão opcional** – o EA original fornece um sinalizador “reverso” que troca sinais longos e curtos. A porta mantém esse comportamento para que as mesmas regras possam ser usadas para testes de contratendência.
4. **Gerenciamento de posição** – sinalizadores opcionais fecham a exposição oposta antes de abrir uma nova posição e limitam a estratégia a uma única posição aberta. Uma regra de uma negociação por barra é aplicada para imitar a implementação MetaTrader.
5. **Filtro de sessão** – a negociação pode ser restrita a uma janela intradiária definida pelo usuário, incluindo sessões wrap-around que passam da meia-noite.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `FastChaikinLength` | Período de média móvel rápida usado dentro do oscilador Chaikin. |
| `SlowChaikinLength` | Período de média móvel lenta usado dentro do oscilador Chaikin. |
| `ChaikinMethod` | Método de média móvel (Simples, Exponencial, Suavizado, LinearWeighted) aplicado à linha de acumulação/distribuição. |
| `FastCciLength` | Retrospectiva do índice rápido de canais de commodities. |
| `SlowCciLength` | Retrospectiva do lento Commodity Channel Index. |
| `MaLength` | Comprimento da média móvel de pré-processamento que alimenta os CCIs. |
| `MaMethod` | Método de média móvel usado para pré-processar o preço antes de atingir os CCIs. |
| `MaPrice` | Tipo de preço (fechamento, abertura, máximo, mínimo, mediano, típico, ponderado) que é suavizado antes dos CCIs. |
| `UseClosedBar` | Processe apenas velas totalmente concluídas (padrão verdadeiro, idêntico a `SignalsBarCurrent=bar_1` em EA). |
| `ReverseSignals` | Troque lógica longa e curta. |
| `CloseOpposite` | Feche uma posição aberta na direção oposta antes de entrar em uma nova. |
| `OnlyOnePosition` | Permitir apenas uma única posição aberta por vez. |
| `TradeMode` | Restrinja a execução a posições compradas, vendidas ou ambas (BuyOnly, SellOnly, BuyAndSell). |
| `UseTimeFilter` | Habilite o filtro da sessão de negociação. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Limites da sessão (inclusive o início, excluindo o final) expressos em tempo de troca. Sessões envolventes são suportadas. |
| `CandleType` | Prazo de assinatura da vela alimentando os indicadores. |

## Notas

- A estratégia usa apenas ligações `SubscribeCandles` de alto nível e indicadores integrados; nenhum buffer personalizado ou solicitação histórica é necessária.
- Todos os cálculos baseados em preços adotam o mesmo pré-processamento de média móvel que o indicador MetaTrader `CCIDualOnMA`, alimentando o CCI com uma série de preços suavizados.
- Os parâmetros padrão reproduzem os padrões originais do EA: Chaikin 3/10 EMA, CCI períodos 14 e 50, pré-processamento de 12 períodos SMA e uma janela de negociação das 10h01 às 15h02.
