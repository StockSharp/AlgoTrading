# Estratégia de Linhas de Reunião AML RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **AML RSI Meeting Lines Strategy** é uma versão StockSharp do MetaTrader 5 consultor especialista `Expert_AML_RSI.mq5`. O sistema original combina o reconhecimento do padrão de velas japonês com o Índice de Força Relativa (RSI) para negociar reversões de "Linhas de Encontro" de alta e baixa. Esta conversão mantém a lógica de negociação principal enquanto a adapta ao StockSharp de alto nível de API com assinaturas de velas e indicadores integrados.

## Lógica de negociação
- Assina um tipo de vela configurável e processa apenas velas finalizadas.
- Calcula uma média móvel simples dos tamanhos do corpo das velas para detectar velas "longas" que formam padrões de linhas de reunião.
- Rastreia valores RSI nas duas velas concluídas mais recentes para confirmação e sinais de saída.
- **Configuração de alta**: a reversão das Linhas de Reunião de duas barras com RSI abaixo do limite de alta aciona entradas longas.
- **Configuração de baixa**: padrão espelhado com RSI acima do limite de baixa aciona entradas curtas.
- **Saídas de posição**: RSI cruzamentos por meio de níveis inferior e superior configuráveis fecham negociações abertas na direção oposta.
- Usa ajudantes `BuyMarket`, `SellMarket` e `ClosePosition` para gerenciar a exposição e muda automaticamente o tamanho da posição quando um sinal contrário aparece.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Prazo usado para avaliar padrões de velas. | Período de 1 hora |
| `RsiPeriod` | RSI comprimento de lookback. | 11 |
| `BodyAveragePeriod` | Número de velas para o tamanho médio do corpo. | 3 |
| `BullishRsiLevel` | Máximo RSI que valida Linhas de Reunião de alta. | 40 |
| `BearishRsiLevel` | Mínimo RSI que valida linhas de reunião de baixa. | 60 |
| `LowerExitLevel` | RSI nível que fecha posições vendidas em cruzamentos ascendentes. | 30 |
| `UpperExitLevel` | RSI nível que fecha posições compradas em cruzamentos descendentes. | 70 |

Todos os parâmetros são expostos como objetos `StrategyParam<T>` para que possam ser otimizados no designer StockSharp.

## Gestão de risco
- `StartProtection()` é invocado em `OnStarted` para ativar o monitoramento de posição integrado da estrutura.
- A estratégia fecha a exposição existente sempre que RSI cruza os limites de saída configurados antes de considerar novos sinais.
- As ordens de mercado revertem automaticamente a posição adicionando o valor absoluto da exposição atual ao volume configurado.

## Notas de conversão
- A média do castiçal usa `SimpleMovingAverage` alimentado com corpos absolutos de velas, espelhando o auxiliar `AvgBody` da fonte MQL5.
- A confirmação de RSI depende dos valores das duas velas anteriores, reproduzindo as verificações `RSI(1)` e `RSI(2)` do especialista original.
- Todos os comentários no código foram reescritos em inglês e a estrutura segue os requisitos do repositório de namespaces com escopo de arquivo com recuo de tabulação.

## Uso
1. Anexe a estratégia a um título em StockSharp e selecione o tipo de vela desejado.
2. Configure RSI e limites de saída para corresponder ao local de negociação ou à volatilidade do instrumento.
3. Execute primeiro a estratégia na negociação em papel para validar o reconhecimento de padrões antes de passar para a negociação ou otimização ao vivo.
4. Use os parâmetros fornecidos durante a otimização para ajustar os níveis de RSI e o comprimento médio do corpo para diferentes mercados.

## Isenção de responsabilidade
Esta estratégia é fornecida apenas para fins educacionais. Teste minuciosamente em dados históricos e em ambientes simulados antes de implantá-los em capital real.
