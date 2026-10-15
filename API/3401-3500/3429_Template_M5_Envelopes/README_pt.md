# Estratégia de envelopes modelo M5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertido do consultor especialista MetaTrader 4 "Template_M5_Envelopes.mq4". A estratégia rastreia um envelope de média móvel ponderada linear (LWMA) em velas de cinco minutos e ordens de stop de rompimento de armas sempre que o preço se afasta o suficiente do canal. As ordens pendentes são reavaliadas dinamicamente para acompanhar o mercado, e as posições preenchidas são protegidas por lógica configurável de stop-loss, take-profit e trailing-stop.

## Lógica de negociação

1. Um LWMA baseado no preço médio da vela é calculado com o `EnvelopePeriod` configurado. As faixas superior e inferior do envelope são derivadas aplicando-se a porcentagem `EnvelopeDeviation`.
2. Cada vela finalizada de cinco minutos armazena seus valores de envelope junto com a máxima e a mínima. Os sinais só são avaliados quando um conjunto completo de valores "anteriores" estiver disponível, correspondendo à implementação MetaTrader que referenciou `iEnvelopes(..., shift = 1)` e a barra anterior.
3. Uma configuração de **compra** aparece quando:
   * A mínima da vela anterior fica pelo menos `DistancePoints` abaixo do envelope inferior anterior, e
   * O preço de oferta atual permanece pelo menos `DistancePoints` abaixo do mesmo valor do envelope.
4. Uma configuração de **venda** reflete a lógica com a máxima anterior e o envelope superior.
5. Quando uma configuração está ativa, apenas uma ordem stop é permitida (o EA original também se restringiu a um único mercado ou ordem pendente). O pedido é feito na oferta/oferta atual mais a distância de `EntryOffsetPoints`.
6. Enquanto a ordem pendente permanece ativa, a estratégia monitora o mercado. Se a diferença entre o preço da ordem e o preço de compra/venda atual exceder `EntryOffsetPoints + SlippagePoints`, a ordem é cancelada e imediatamente registrada novamente no novo preço de referência, mantendo o stop-loss e o take-profit anexados alinhados com as compensações desejadas.
7. Se o spread atual exceder `MaxSpreadPoints`, todas as entradas pendentes serão canceladas para evitar negociações em condições de liquidez desfavoráveis.

## Gerenciamento de pedidos

* Após a ativação da ordem de entrada, a estratégia registra o preço de execução e registra ordens de proteção stop e take-profit em compensações de `StopLossPoints` e `TakeProfitPoints`, respectivamente. Se um dos valores for zero, a proteção correspondente será ignorada.
* O módulo de trailing stop (habilitado com `UseTrailingStop`) rastreia o melhor lance/venda. Sempre que o preço se move a favor da posição aberta em mais de `TrailingStopPoints`, a ordem stop é reavaliada mais perto do mercado usando `ReRegisterOrder`. As paradas longas apenas seguem para cima, enquanto as paradas curtas apenas seguem para baixo.
* Quando a posição estiver totalmente fechada, todas as ordens de proteção serão canceladas e o estado interno será redefinido. Nenhuma nova ordem de entrada será considerada até que a posição retorne à estabilidade.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `MaxSpreadPoints` | Spread máximo permitido antes do cancelamento de ordens pendentes. |
| `TakeProfitPoints` | Distância de take-profit aplicada às posições preenchidas. |
| `StopLossPoints` | Distância de stop-loss aplicada a posições pendentes e preenchidas. |
| `EntryOffsetPoints` | Compensação (em pontos) do bid/ask onde as entradas stop são colocadas. |
| `UseTrailingStop` | Permite o gerenciamento de trailing stop para posições abertas. |
| `TrailingStopPoints` | Distância (em pontos) mantida pelo trailing stop. |
| `FixedVolume` | Volume de negociação enviado com cada ordem de entrada. |
| `EnvelopePeriod` | Comprimento do LWMA usado como base do envelope. |
| `EnvelopeDeviation` | Largura do envelope em porcentagem. |
| `DistancePoints` | Diferença mínima entre preço e envelope necessária para um sinal. |
| `SlippagePoints` | Tolerância extra (em pontos) adicionada ao limite de reprecificação. |
| `CandleType` | Prazo usado para calcular o envelope LWMA (padrão M5). |

## Notas

* A estratégia assina velas e cotações de nível 1. Se os dados de compra/venda não estiverem disponíveis, as condições de entrada não serão acionadas porque os cálculos de spread e trailing stop dependem disso.
* As ordens protetoras de stop e take-profit são recriadas com o volume mais recente sempre que a lógica móvel ajusta o preço do stop-loss.
* Todos os comentários dentro do código são escritos em inglês e tabulações são usadas para recuo para corresponder às convenções do projeto.
