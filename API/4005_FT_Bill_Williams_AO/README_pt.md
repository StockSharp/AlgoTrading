# FT Bill Williams Estratégia AO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **FT Bill Williams AO Strategy** é uma versão de alto nível StockSharp do MetaTrader 4 especialista `FT_BillWillams_AO`. O original
robot foi publicado em FORTRADER.RU e combina fractais Bill Williams, o indicador Alligator e o Awesome Oscillator para
identificar oportunidades de ruptura antecipadas. A versão StockSharp mantém a lógica original, mas funciona com uma única posição líquida em vez de
vários pedidos simultâneos.

O algoritmo opera em velas concluídas a partir de um período configurável. Cada barra:

1. Detecta fractais de alta e baixa construídos a partir de um número ímpar de velas.
2. Filtra fractais verificando se o preço do fractal está fora da linha dos dentes Alligator.
3. Aguarda que o Awesome Oscillator (AO) forme o clássico padrão de aceleração de três barras.
4. Coloca um gatilho de rompimento acima/abaixo da máxima ou mínima recente, deslocado por um número definido pelo usuário de MetaTrader pontos.
5. Aplica a rotina de rastreamento Gragus de Bill Williams e regras de saída opcionais baseadas na mandíbula.

## Lógica de entrada
### Entradas longas
- Um fractal de alta aparece e seu preço alto fica acima dos dentes Alligator.
- Os valores AO obtidos `SignalShift + 2`, `SignalShift + 1` e `SignalShift` velas atrás satisfazem `A > B`, `B < C`, e todos os três são
positivo.
- Um nível de rompimento pendente é calculado como `High[SignalShift] + IndentPoints * price step`.
- Quando uma vela completa cruza esse nível e AO ainda aumenta (`C > B`), a estratégia abre ou reverte para uma posição longa.

### Entradas curtas
- Um fractal de baixa aparece e seu mínimo está abaixo dos dentes Alligator.
- Os valores AO satisfazem `A < B`, `B > C` e todos os três são negativos.
- Um gatilho de breakout é colocado em `Low[SignalShift] - IndentPoints * price step`.
- Uma posição curta (ou reversão de longa) é aberta quando a vela cai abaixo desse gatilho enquanto AO continua caindo (`C < B`).

## Gestão de saída e risco
- Stop-loss e take-profit iniciais são expressos em MetaTrader pontos e se traduzem na distância real do preço por meio do instrumento
etapa de preço.
- O modo **CloseDropTeeth** pode fechar posições quando o fechamento atual ou o fechamento anterior cruza a mandíbula Alligator.
- **CloseReverseSignal** determina se um fractal oposto ou a ativação do sinal de fuga oposto deve forçar um
saída.
- A opção **UseTrailing** ativa a rotina de trailing stop original do Gragus: quando os lábios Alligator avançam mais rápido do que um curto
SMA, o stop é movido para os lábios; caso contrário, ele arrasta os dentes. Ambos os movimentos exigem que o preço fique a pelo menos 12 pontos de distância
da linha alvo.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `TradeVolume` | Tamanho do pedido em lotes. Também está escrito em `Strategy.Volume`. |
| `CandleType` | Tipo de dados e prazo das velas de entrada. |
| `FractalPeriod` | Número ímpar de velas usadas para confirmar fractais (padrão 5). |
| `IndentPoints` | MetaTrader pontos adicionados acima/abaixo da máxima/mínima da vela de rompimento. |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Comprimento das médias móveis suavizadas usadas pelas linhas Alligator. |
| `JawShift`, `TeethShift`, `LipsShift` | Deslocamento para frente (em velas) aplicado às linhas Alligator. |
| `CloseDropTeeth` | Comportamento da regra de fechamento baseada na mandíbula: desativado, cruzamento próximo atual ou cruzamento próximo anterior. |
| `CloseReverseSignal` | Condição de saída em sinais opostos: desativado, em novo fractal ou quando o breakout oposto estiver armado. |
| `UseTrailing` | Ativa ou desativa a rotina de trailing stop Gragus. |
| `TrendSmaPeriod` | Período do auxiliar SMA usado pela comparação final. |
| `StopLossPoints` | Distância inicial do stop-loss em MetaTrader pontos. Defina como zero para desativar. |
| `TakeProfitPoints` | Distância inicial de lucro em MetaTrader pontos. Defina como zero para desativar. |
| `SignalShift` | Número de velas totalmente fechadas ignoradas ao ler valores AO e máximos/mínimos recentes. |

## Notas
- A estratégia assume que a segurança expõe um `PriceStep` válido (volta para `MinPriceStep`); se ambos estiverem faltando, um padrão de
`0.0001` é usado.
- Apenas uma posição líquida é gerenciada. Os sinais de reversão fecham automaticamente a posição oposta antes de abrir uma nova.
- Para melhores resultados, mantenha `FractalPeriod` ímpar; o especialista original usou 5 velas.
- `IndentPoints`, `StopLossPoints` e `TakeProfitPoints` imitam MetaTrader pontos. Ajuste-os de acordo com o preço do instrumento
escala.
