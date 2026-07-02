# Alerta MACD Estratégia lenta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Alert MACD Slow** reproduz o MetaTrader 4 especialista `Alert_MACD_Slow.mq4`. Ele observa a linha principal MACD e duas médias móveis exponenciais e gera alertas textuais quando a pilha do indicador sinaliza um possível rompimento. Nenhum pedido é enviado — a conversão permanece fiel ao consultor original, que exibia apenas mensagens pop-up.

## Ideia Central

1. Assine a série de velas selecionada e alimente um MACD(3, 20, 9) junto com EMAs rápidos e lentos (20 e 65 períodos).
2. Armazene em cache os valores MACD das quatro velas concluídas anteriores para avaliar as transições de inclinação usadas pelo código MQL.
3. Armazene os máximos e mínimos das duas últimas velas para emular os filtros de breakout `High[1]/High[2]` e `Low[1]/Low[2]`.
4. Quando o EMA rápido ficar acima (ou abaixo) do EMA lento e o fechamento da vela quebrar as máximas (ou mínimas) memorizadas enquanto MACD virar para cima (ou para baixo) abaixo da linha zero, registre a respectiva mensagem de alerta.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `MacdFastPeriod` | `3` | Comprimento EMA rápido dentro do cálculo MACD. |
| `MacdSlowPeriod` | `20` | Comprimento EMA lento usado pelo MACD. |
| `MacdSignalPeriod` | `9` | Período de suavização de sinal do MACD. |
| `QuickEmaPeriod` | `20` | Período de acompanhamento rápido de tendência EMA (`Ma_Quick`). |
| `SlowEmaPeriod` | `65` | Período do filtro de tendência lenta EMA (`Ma_Slow`). |
| `CandleType` | `TimeFrame(30m)` | Fonte de vela passada para a cadeia do indicador; escolha um período de tempo que corresponda ao seu gráfico. |

## Detalhes da lógica de alerta

- **MACD memória de inclinação**: a estratégia muda os valores MACD anteriores internamente em vez de chamar `GetValue`, satisfazendo as diretrizes de conversão enquanto preserva as comparações originais (`Macd_1 > Macd_2`, etc.).
- **Verificação de breakout**: os preços de fechamento acima das máximas anteriores ou abaixo das mínimas anteriores são tratados como um proxy para as verificações de compra/venda de MetaTrader, que usaram a cotação ao vivo em relação aos extremos históricos das velas.
- **Filtro de tendência**: o alerta é acionado somente quando o EMA rápido está no lado correto do EMA lento, correspondendo aos filtros longos/curtos no especialista MQL.
- **Registro**: os alertas são enviados por meio de `AddInfoLog`. Eles incluem os quatro valores MACD armazenados em cache e os níveis de interrupção para facilitar a depuração e o backtesting.
- **Sem negociação**: como o consultor de origem nunca realizou negociações, a conversão StockSharp mantém a estratégia plana e se concentra apenas na sinalização.

## Uso típico

1. Anexe a estratégia a um símbolo, configure o tipo de vela para o período desejado e mantenha os períodos do indicador padrão ou ajuste-os para experimentação.
2. Inicie a estratégia e espere até que os indicadores MACD e EMA sejam formados (várias velas são necessárias porque MACD requer histórico).
3. Observe o diário: quando uma configuração de alta aparecer, você verá `SET UP LONG`, enquanto as configurações de baixa produzirão `SET UP SHORT_VALUE`. O sufixo reflete o texto do alerta original.
4. Utilize os diagnósticos impressos para decidir se deve agir manualmente ou encadear a estratégia com automação personalizada.

## Classificação

- **Categoria**: Alertas/Confirmação de quebra de tendência
- **Direção de negociação**: Nenhuma (somente sinal)
- **Estilo de execução**: baseado em eventos em velas finalizadas
- **Requisitos de dados**: Série de velas compatível com o `CandleType` escolhido
- **Complexidade**: Moderada (vários filtros de indicadores, mas manipulação direta de estado)
- **Gerenciamento de Riscos**: Não aplicável (nenhuma posição aberta)

Esta porta mantém o comportamento de alerta do especialista MQL enquanto aproveita assinaturas StockSharp, ligações de indicadores e utilitários de registro.
