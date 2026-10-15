# Estratégia Macd Secrets
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **estratégia Macd Secrets** é um sistema multitemporal de seguimento de momentum inspirado no expert advisor original "Macd Secrets I" para MetaTrader. A versão StockSharp usa a API de alto nível e foca em alinhar a direção MACD em três períodos, filtrando operações com uma linha de base de média móvel ponderada linear (LWMA) e uma verificação de desvio de momentum. A estratégia mantém apenas uma posição líquida por vez, oferecendo um perfil de risco simplificado e transparente em comparação com o EA de origem, que podia piramidar múltiplas ordens.

## Geração de sinais
### Configuração comprada
1. A LWMA rápida está abaixo da LWMA lenta no período de execução, sinalizando que o preço negocia perto do lado inferior do canal de tendência (o EA original aplica o mesmo filtro).
2. A linha MACD está acima da sua linha de sinal em todos os períodos acompanhados: execução, confirmação de tendência e confirmação mensal. Isso espelha o alinhamento triplo de MACD na versão MQL.
3. Pelo menos uma das três últimas leituras de momentum no período de tendência se desvia de 100 pelo mínimo configurado (padrão 0.3). O cálculo de desvio reproduz a lógica `MathAbs(100 - Momentum)` do EA.
4. Nenhuma posição está aberta.

Quando as condições são atendidas, uma ordem de compra a mercado é colocada com o volume configurado.

### Configuração vendida
1. A linha MACD está abaixo da sua linha de sinal nos períodos de execução, tendência e mensal.
2. Pelo menos uma das três últimas divergências de momentum no período de tendência excede o limite vendido configurado.
3. Nenhuma posição está aberta (a versão evita hedge e escalonamento).

Se todas as regras se mantêm, uma ordem de venda a mercado é enviada.

### Gestão da operação
- A estratégia opcionalmente inicia ordens de proteção usando distâncias baseadas em pontos para stop-loss e take-profit. Essas distâncias são multiplicadas pelo passo de preço do ativo para converter pontos em incrementos de preço.
- Nenhuma lógica de trailing-stop, breakeven ou proteção baseada em equity do EA original está incluída; a proteção do StockSharp é aplicada uma vez na inicialização.
- Sinais são avaliados apenas em candles finalizados para evitar ruído intrabar.

## Dados multitemporais
- **Período primário**: frequência de execução (padrão 15 minutos). MACD e o par de LWMAs são calculados aqui.
- **Período de tendência**: confirmação de período superior (padrão 1 hora). Tanto MACD quanto momentum rodam nessa assinatura. Desvios de momentum são coletados dos três últimos candles fechados.
- **Período mensal**: confirmação MACD de longo prazo (padrão 30 dias para aproximar um mês calendário).

A estratégia sobrescreve `GetWorkingSecurities` para que todas as três assinaturas sejam solicitadas ao conector desde o início.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | --------- | ------ |
| `OrderVolume` | Volume de negociação em lotes. Deve ser positivo. | `0.1` |
| `TakeProfitPoints` | Distância de take-profit medida em pontos. Defina como zero para desabilitar. | `50` |
| `StopLossPoints` | Distância de stop-loss em pontos. Defina como zero para desabilitar. | `20` |
| `FastMaPeriod` | Comprimento da LWMA rápida no período primário. | `6` |
| `SlowMaPeriod` | Comprimento da LWMA lenta no período primário. | `85` |
| `MacdFastPeriod` | Período da EMA rápida usado por cada instância MACD. | `12` |
| `MacdSlowPeriod` | Período da EMA lenta usado por cada instância MACD. | `26` |
| `MacdSignalPeriod` | Período da EMA de sinal para MACD. | `9` |
| `MomentumPeriod` | Retrospectiva de momentum no período de tendência. | `14` |
| `MomentumBuyThreshold` | Desvio absoluto mínimo a partir de 100 exigido para operações compradas. | `0.3` |
| `MomentumSellThreshold` | Desvio absoluto mínimo a partir de 100 exigido para operações vendidas. | `0.3` |
| `PrimaryCandleType` | Tipo de candle para execução. Padrão de período de 15 minutos. | `15m` |
| `TrendCandleType` | Tipo de candle para confirmação. Padrão de período de 1 hora. | `1h` |
| `MonthlyCandleType` | Tipo de candle para confirmação de longo prazo. Padrão de barra de 30 dias. | `30d` |

## Notas de uso
- O filtro LWMA é intencionalmente assimétrico: apenas operações compradas exigem que a LWMA rápida esteja abaixo da LWMA lenta, correspondendo ao comportamento observado no script MQL.
- Como a versão negocia uma única posição líquida, ela ignora o dimensionamento de posição estilo martingale presente no código-fonte (`LotsOptimized`). Se empilhamento for necessário, ele pode ser reintroduzido acompanhando o volume executado e comparando com `OrderVolume`.
- Garanta que a corretora ou fonte de dados conectada possa fornecer todos os três períodos de candles; caso contrário, a estratégia permanecerá inativa aguardando formação dos indicadores.
- Considere ajustar o período mensal para mercados em que candles de 30 dias não estejam disponíveis, fornecendo um parâmetro `DataType` personalizado.
- A estratégia opera inteiramente em candles fechados e não lê buffers históricos de indicadores diretamente, cumprindo as diretrizes de uso de indicadores do StockSharp.

## Diferenças em relação ao EA original
- Trailing-stop, breakeven, saídas baseadas em dinheiro e proteção de equity em nível de conta não são portados. Em vez disso, usa-se proteção StockSharp com distâncias estáticas.
- Piramidagem de ordens e lógica martingale são omitidas por clareza. O dimensionamento de posição permanece constante.
- Notificações (alertas, e-mails, mensagens push) não são implementadas.

## Aviso
Negociação algorítmica envolve risco financeiro significativo. Teste a estratégia em dados históricos e em ambiente simulado antes de usá-la com capital real.
