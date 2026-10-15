# Estratégia RSI Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o consultor especialista do MetaTrader *"RSI trader v0.15"* na API de alto nível do StockSharp. Ela alinha a direção da tendência entre a ação do preço e um Índice de Força Relativa (RSI) suavizado. O trading é realizado em um único instrumento usando velas de uma hora por padrão, mas o período é configurável através do parâmetro `CandleType`.

## Lógica de trading
1. Calcular um RSI padrão com um período configurável.
2. Suavizar o RSI com duas médias móveis simples (SMA): uma média de sinal rápida e uma de confirmação mais lenta.
3. Rastrear duas médias móveis do preço de fechamento: uma média móvel simples curta e uma média móvel ponderada longa para aproximar o par SMA/LWMA do MQL original.
4. Gerar estados de tendência em cada vela terminada:
   - **Alinhamento de alta**: SMA de preço curta acima da longa **e** SMA RSI rápida acima da lenta.
   - **Alinhamento de baixa**: SMA de preço curta abaixo da longa **e** SMA RSI rápida abaixo da lenta.
   - **Lateral / discordância**: médias móveis apontando em direções opostas, sinalizando que não há tendência clara.
5. Agir sobre o estado detectado:
   - Abrir uma posição comprada quando o alinhamento de alta aparecer e não houver posição atualmente aberta.
   - Abrir uma posição vendida quando o alinhamento de baixa aparecer e não houver posição atualmente aberta.
   - Fechar imediatamente qualquer posição aberta quando o estado lateral for detectado, espelhando a saída protetora da versão MQL.
6. O modo de reversão opcional inverte todas as direções de entrada, permitindo ao usuário operar contra a tendência em relação aos sinais detectados.

A estratégia respeita o tratamento de proteção incorporado do StockSharp e requer velas concluídas antes de tomar qualquer ação.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `RsiPeriod` | Período de retrospecto usado para o cálculo do RSI. | 14 |
| `ShortRsiMaPeriod` | Comprimento da SMA rápida aplicada aos valores do RSI. | 9 |
| `LongRsiMaPeriod` | Comprimento da SMA lenta aplicada aos valores do RSI. | 45 |
| `ShortPriceMaPeriod` | Comprimento da SMA curta aplicada aos preços de fechamento. | 9 |
| `LongPriceMaPeriod` | Comprimento da média móvel ponderada longa aplicada aos preços. | 45 |
| `Reverse` | Quando `true`, as ordens de compra e venda são trocadas (espelha a entrada "Reverse" original). | `false` |
| `CandleType` | Tipo de dados para velas de preço. Padrão é período de uma hora. | `1h` |

Todos os parâmetros inteiros expõem intervalos de otimização que refletem a flexibilidade das configurações de entrada do especialista do MetaTrader.

## Gestão de risco
- As posições são fechadas assim que as tendências de preço e RSI discordam (estado lateral), reproduzindo o comportamento de saída imediata do EA.
- `StartProtection()` é habilitado no início para cooperar com a infraestrutura protetora do StockSharp.

## Notas
- A estratégia depende da propriedade base `Volume` de `Strategy` para definir o tamanho da operação.
- Apenas velas concluídas são processadas; atualizações parciais são ignoradas para evitar sinais prematuros.
- A média móvel ponderada é usada para corresponder à LWMA longa original aplicada aos fechamentos de preço.
