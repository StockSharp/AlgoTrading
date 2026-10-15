# ABE BE CCI Estratégia envolvente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia StockSharp transporta o MetaTrader 5 consultor especialista **Expert_ABE_BE_CCI** (pasta `MQL/306`). O EA original combina padrões de velas Bullish/Bearish Engulfing com um módulo de confirmação do Commodity Channel Index (CCI) e gerenciamento de dinheiro de lote fixo. A implementação C# mantém a mesma lógica de decisão enquanto aproveita a assinatura de alto nível e as ligações de indicadores fornecidas por StockSharp.

O mecanismo observa as velas concluídas no período selecionado, calcula uma média contínua dos corpos das velas, uma média dos preços de fechamento e um CCI com período configurável. Padrões envolventes de alta ou baixa só são aceitos quando os corpos das velas excedem a média recente e o ponto médio da vela engolida está no lado correto da média móvel, imitando as verificações MQL `CCandlePattern`. As negociações longas exigem um engolfo de alta mais CCI abaixo do limite de sobrevenda, enquanto as negociações curtas exigem a condição de espelho com CCI acima do limite de sobrecompra. As saídas de posição refletem a lógica de "votação" EA: CCI cruzamentos de ±ExitLevel neutralizam as posições abertas independentemente da direção.

## Fluxo de trabalho

1. Assine o tipo de vela configurado e calcule:
   - Média do corpo da vela acima de `BodyAveragePeriod` barras.
   - Média móvel dos preços de fechamento na mesma janela.
   - Índice de canal de commodities com comprimento `CciPeriod`.
2. Para cada vela acabada:
   - Verifique se a vela anterior forma uma barra engolfada de cor oposta.
   - Check that the engulfing body is larger than the rolling body average and closes beyond the previous open, replicating the MQL filters.
   - Confirme o contexto da tendência comparando o ponto médio da vela anterior com a média móvel do preço de fechamento.
   - Confirme o impulso com CCI vs. `EntryOversoldLevel` ou `EntryOverboughtLevel`.
3. Gerenciar negociações:
   - Se as condições de alta se alinharem e nenhuma posição longa estiver ativa, feche as posições vendidas e compre o volume configurado.
   - Se as condições de baixa se alinharem e nenhuma venda estiver ativa, feche as posições compradas e venda o volume configurado.
   - Monitore CCI para saídas: qualquer cruzamento abaixo de `+ExitLevel` ou através de `-ExitLevel` fecha posições compradas, enquanto cruzamentos acima de `-ExitLevel` ou abaixo de `+ExitLevel` fecham posições vendidas, correspondendo à lógica de "voto" de 40 pontos do EA.

## Parâmetros padrão

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CciPeriod` | 49 | Comprimento do indicador Commodity Channel Index. |
| `BodyAveragePeriod` | 11 | Janela para cálculo da média do tamanho do corpo da vela e da média do preço de fechamento. |
| `EntryOversoldLevel` | -50 | Limite de CCI confirmando configurações envolventes de alta. |
| `EntryOverboughtLevel` | 50 | Limite de CCI confirmando configurações de baixa. |
| `ExitLevel` | 80 | Nível CCI absoluto que aciona saídas de posição quando cruzado. |
| `CandleType` | 1 hora | Prazo usado para assinatura de velas. |

## Notas

- O tratamento de volume reflete conversões típicas de StockSharp: `Volume` define o tamanho base do pedido; posições opostas são achatadas antes da reversão.
- Os componentes de rastreamento e gerenciamento de dinheiro (`TrailingNone`, `MoneyFixedLot`) do pacote MQL não são recriados; O dimensionamento do pedido de StockSharp já cobre o comportamento do lote fixo.
- Todos os comentários dentro do código estão em inglês, tabulações são usadas para indentação e nenhum valor de indicador é recuperado por meio de `GetValue`, seguindo as diretrizes do repositório.
