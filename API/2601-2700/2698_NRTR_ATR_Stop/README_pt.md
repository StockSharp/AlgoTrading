# Estratégia NRTR ATR Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia NRTR ATR Stop** é uma conversão direta do consultor especialista de MetaTrader `Exp_NRTR_ATR_STOP_Tm`. O sistema combina um stop de Reversão de Tendência Não Repintável (NRTR) com um filtro de Faixa Verdadeira Média (ATR) para determinar a tendência dominante e arrastar os níveis de proteção. As decisões de negociação são geradas no fechamento do período selecionado e podem ser atrasadas por um número configurável de barras totalmente formadas para imitar o deslocamento de sinal original.

A estratégia é implementada sobre a API de alto nível do StockSharp. Toda a lógica de negociação é impulsionada por assinaturas de candles, vínculos de indicadores e auxiliares de ordens gerenciadas, garantindo compatibilidade com os produtos Designer, Shell, Runner e API.

## Lógica de negociação

1. **Cálculo do indicador**
   - O ATR é calculado no período selecionado com o período fornecido.
   - O valor do ATR é multiplicado por um coeficiente para construir os níveis superior e inferior do NRTR.
   - A direção da tendência muda quando o candle anterior rompe o nível NRTR oposto; esses eventos também criam sinais de seta que podem acionar entradas.
2. **Atraso de sinal**
   - O parâmetro `SignalBarDelay` reproduz a entrada `SignalBar` do MetaTrader. Ele atrasa a execução pelo número escolhido de candles completados, permitindo que a estratégia avalie sinais históricos exatamente como o especialista fonte.
3. **Entradas**
   - Uma posição **comprada** é aberta quando ocorre uma reversão altista do NRTR e as entradas compradas estão habilitadas.
   - Uma posição **vendida** é aberta quando ocorre uma reversão baixista do NRTR e as entradas vendidas estão habilitadas.
4. **Saídas**
   - Reversões direcionais fecham qualquer posição oposta se o fechamento for permitido para aquele lado.
   - Um filtro de sessão opcional pode forçar o fechamento de todas as posições fora da janela de negociação permitida.
   - O gerenciamento de risco adicional é tratado por distâncias de stop-loss e take-profit expressas em passos de preço. O nível NRTR também arrasta uma posição ativa ao apertar o stop de proteção na direção da tendência.

## Gestão de risco

- **Volume**: As negociações são abertas com o parâmetro configurável `OrderVolume`. O volume pode ser otimizado assim como na versão do MetaTrader.
- **Stop-loss / take-profit**: As distâncias são especificadas em múltiplos do passo de preço do instrumento, correspondendo às configurações originais baseadas em pontos. Quando tanto um stop manual quanto um nível NRTR estão disponíveis, o preço de proteção é escolhido de forma conservadora (mais próximo do mercado) para evitar ampliar o risco.
- **Controle de sessão**: Quando `UseTradingWindow` está habilitado, a estratégia só abre posições dentro do intervalo `[StartHour:StartMinute, EndHour:EndMinute]` definido e fecha qualquer posição aberta assim que o mercado sai dessa janela.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `OrderVolume` | 1 | Volume usado ao enviar ordens de mercado. |
| `StopLossPoints` | 1000 | Distância de stop em passos de preço. Definir como `0` para desabilitar. |
| `TakeProfitPoints` | 2000 | Distância de take-profit em passos de preço. Definir como `0` para desabilitar. |
| `BuyPosOpen` / `SellPosOpen` | `true` | Permitir abertura de posições compradas ou vendidas em reversões NRTR. |
| `BuyPosClose` / `SellPosClose` | `true` | Permitir fechamento de posições compradas ou vendidas quando aparece um sinal oposto. |
| `UseTradingWindow` | `true` | Habilitar o filtro de tempo que imita o consultor especialista original. |
| `StartHour` / `StartMinute` | 0 / 0 | Início da sessão de negociação permitida. |
| `EndHour` / `EndMinute` | 23 / 59 | Fim da sessão de negociação permitida. Suporta intervalos noturnos. |
| `CandleType` | Período de 1 hora | Tipo de candle usado para os cálculos de ATR e NRTR. |
| `AtrPeriod` | 20 | Número de barras usadas para calcular o ATR. |
| `AtrMultiplier` | 2 | Coeficiente aplicado ao ATR ao construir os níveis NRTR. |
| `SignalBarDelay` | 1 | Número de barras completadas para atrasar a execução do sinal. |

## Notas

- A estratégia usa apenas processamento em nível de candle; a replicação tick a tick do EA original é evitada intencionalmente para permanecer consistente com a arquitetura de alto nível do StockSharp.
- Os comentários dentro do código estão em inglês conforme exigido pelas diretrizes do projeto.
- Uma versão em Python é intencionalmente omitida para atender à solicitação do usuário.
