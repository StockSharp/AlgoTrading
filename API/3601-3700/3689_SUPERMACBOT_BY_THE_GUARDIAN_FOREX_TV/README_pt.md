# SUPERMACBOT da The Guardian Estratégia de TV Forex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O **SUPERMACBOT da The Guardian Forex TV Strategy** replica o conceito do consultor especialista MetaTrader original, combinando o oscilador MACD com um filtro de tendência duplo de média móvel simples e um filtro de saída de média móvel. A implementação StockSharp convertida funciona em velas concluídas e envia ordens de mercado sempre que uma confluência de alta ou baixa se forma. A estratégia evita a negociação tick-by-tick e segue as diretrizes API de alto nível, contando com assinaturas de velas e ligações de indicadores.

O mecanismo de negociação avalia o impulso por meio do histograma MACD e do alinhamento de tendências entre duas médias móveis simples. Uma média móvel móvel atua tanto como uma referência de gerenciamento comercial quanto como um filtro de confirmação atrasada, espelhando o módulo final configurado no especialista MQL. A versão StockSharp concentra-se na clareza e portabilidade entre instrumentos e prazos, expondo cada valor-chave como um parâmetro configurável.

## Lógica de negociação
1. **Fonte de dados** – a estratégia assina um tipo de vela configurável (período de tempo). Cada vela concluída aciona o fluxo de decisão.
2. **Preparação do indicador** – MACD (com períodos ajustáveis de rápido, lento e sinal) e dois SMAs são recalculados em cada vela. Um SMA adicional replica o filtro final do especialista MQL.
3. **Regras de entrada**
   - **Entrada longa**
     - O histograma MACD ultrapassa o limite configurável.
     - O rápido SMA está acima do lento SMA, mostrando uma tendência de alta estabelecida.
     - O preço de fechamento permanece acima do SMA final para garantir a força do preço.
     - A estratégia não possui posição comprada (apenas uma posição líquida é mantida).
   - **Entrada curta**
     - O histograma MACD ultrapassa o limite negativo.
     - O rápido SMA está abaixo do lento SMA, sinalizando um ambiente de baixa.
     - O preço de fechamento permanece abaixo do SMA final.
     - A estratégia não mantém exposição curta.
4. **Regras de saída**
   - As posições longas são fechadas quando qualquer uma das seguintes situações acontece: o histograma fica negativo, o SMA rápido cai abaixo do SMA lento ou o preço fecha abaixo do SMA final.
   - As posições curtas são fechadas quando o histograma se torna positivo, o SMA rápido sobe acima do SMA lento ou o preço fecha acima do SMA final.
5. **Tratamento de risco** – o algoritmo negocia uma única posição líquida e nunca pirâmides. Paradas de proteção podem ser adicionadas externamente usando regras de risco StockSharp, se desejado.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Série de velas processada pela estratégia. | Período de 1 minuto |
| `FastMaPeriod` | Período do filtro de média móvel simples e rápido. | 12 |
| `SlowMaPeriod` | Período do filtro de média móvel simples lenta. | 26 |
| `MacdFastPeriod` | Período EMA rápido para o indicador MACD. | 12 |
| `MacdSlowPeriod` | Período EMA lento para o indicador MACD. | 24 |
| `MacdSignalPeriod` | Período de sinal EMA para o indicador MACD. | 9 |
| `HistogramThreshold` | Valor absoluto mínimo exigido do histograma MACD antes de abrir uma posição. | 0,0 |
| `TrailingPeriod` | Período da média móvel simples móvel usada para confirmações e saídas. | 12 |

Todos os parâmetros são expostos por meio de `StrategyParam<T>` e podem ser otimizados dentro do StockSharp Designer.

## Notas de uso
- Anexe a estratégia a qualquer segurança e prazo adequados ao seu ambiente de teste.
- Certifique-se de que haja um buffer de histórico suficiente disponível para que todos os indicadores fiquem totalmente formados antes do início da negociação.
- Como a estratégia funciona com velas finalizadas e posições líquidas, é seguro operar em carteiras multiinstrumentos sem ordens conflitantes.
- Gestão de dinheiro adicional (dimensionamento de lote, stop loss, saídas parciais) pode ser adicionada compondo a estratégia com outros módulos StockSharp.

## Diferenças do especialista original
- A conversão StockSharp concentra-se na lógica de fechamento de velas em vez do mecanismo orientado a eventos do MetaTrader Expert Advisor. Isso mantém o comportamento determinístico em backtests e negociações ao vivo.
- O dimensionamento de lote e as ordens de trailing stop do Expert Advisor original são substituídas por uma saída simplificada baseada em posição, condicionada pela média móvel.
- Os limites do sinal são tratados por meio do parâmetro de limite do histograma MACD, permitindo que os usuários imitem o sistema de pontuação do MQL Expert ajustando o valor.

## Isenção de responsabilidade
Os algoritmos de negociação envolvem risco financeiro. Faça um backtest e um teste futuro completos da estratégia antes de implementá-la com capital real.
