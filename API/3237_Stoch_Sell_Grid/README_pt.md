# Estratégia de Stoch Sell
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o comportamento do expert MetaTrader original **stochSell**. Ela ouve um único fluxo de velas e aguarda uma confirmação estocástica tripla combinada com um filtro de volatilidade antes de enviar uma ordem de venda a mercado inicial. Imediatamente após a entrada vendida, implanta uma escada de sell stops pendentes para escalar no movimento se o preço continuar caindo.

## Lógica de negociação
- **Filtro de volatilidade** – um Average True Range (ATR) com comprimento configurável deve permanecer abaixo do limiar especificado.
- **Confirmação estocástica lenta** – o oscilador estocástico de período mais longo deve permanecer abaixo do nível de sobrevendido de longo prazo antes de qualquer negociação ser permitida.
- **Confirmação de cruzamento** – tanto o oscilador estocástico médio quanto o rápido devem cruzar para baixo através do gatilho de sobrevendido durante a mesma vela finalizada.
- **Verificação de posição** – novas entradas são colocadas apenas quando a estratégia não tem ordens ativas e a posição está zerada.

Uma vez que todas as condições são atendidas, a estratégia envia uma ordem de venda a mercado usando o volume configurado e imediatamente programa um conjunto de ordens de sell stop de acordo com as configurações de grade. Ordens pendentes são opcionais e podem ser desativadas definindo a contagem de ordens de grade como zero.

## Regras de saída
- **Alvo de lucro** – quando a cesta vendida acumula o lucro desejado em pips (calculado a partir do preço de entrada ponderado por volume), a estratégia recompra toda a posição e remove cada ordem pendente restante.
- **Stop manual** – ordens de grade respeitam um tempo de vida configurável. Quando uma ordem de stop expira sem ser executada, é cancelada automaticamente.
- **Fechamento completo** – qualquer operação de compra que retorne a posição a zero limpa as estatísticas de entrada internas e cancela a grade pendente.

## Gestão de grade
- Ordens pendentes são colocadas abaixo do preço de referência usando o offset inicial e o passo expressos em pips.
- Cada ordem pendente usa o multiplicador de volume da grade, permitindo que o tamanho da cesta difira da entrada a mercado inicial.
- O vencimento (em minutos) é aplicado a cada ordem pendente; zero desativa o tempo limite.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Período principal para cada indicador e decisão de negociação. |
| `AtrPeriod` / `AtrThreshold` | Filtro de volatilidade que controla quando a estratégia pode negociar. |
| `FastKPeriod`, `FastDPeriod`, `FastSlowing` | Configuração do oscilador estocástico rápido. |
| `MediumKPeriod`, `MediumDPeriod`, `MediumSlowing` | Configuração do oscilador estocástico médio. |
| `SlowKPeriod`, `SlowDPeriod`, `SlowSlowing` | Configuração do oscilador estocástico lento. |
| `OversoldLevel` | Nível que os valores estocásticos rápido e médio devem cruzar para baixo. |
| `LongTermOversoldLevel` | Limite superior para o estocástico lento durante a entrada. |
| `ProfitTargetPips` | Lucro líquido em pips necessário para fechar a cesta vendida. |
| `GridOrdersCount` | Número de sell stops pendentes criados após a entrada. |
| `GridStartOffsetPips` | Offset em pips entre o preço de entrada e a primeira ordem pendente. |
| `GridStepPips` | Distância em pips entre ordens pendentes consecutivas. |
| `GridVolume` | Volume aplicado a cada ordem pendente. |
| `GridExpirationMinutes` | Tempo de vida das ordens pendentes em minutos. |
| `MarketVolume` | Volume usado para a venda a mercado inicial. |

## Notas
- Os valores dos indicadores são processados através da API `BindEx` de alto nível e apenas velas finalizadas acionam decisões de negociação.
- A lógica de rastreamento de posição mantém um preço de entrada ponderado por volume para traduzir o alvo de lucro bruto em pips.
- Para desativar o escalonamento, basta definir a contagem de ordens de grade como zero; a estratégia ainda dependerá da confirmação estocástica e do filtro ATR para operações de disparo único.
