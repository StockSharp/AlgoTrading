# Estratégia de Parallax Sell
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Parallax Sell é uma estratégia martingale somente-vendida convertida do consultor especialista MetaTrader `parallax_sell`. O robô original operava cruzamentos JPY (CAD/JPY e CHF/JPY) e se baseia em uma confluência de filtros de Williams %R, MACD e oscilador estocástico para iniciar vendidos em rallies de sobrecompra. As saídas de posição dependem de sinais de desvanecimento do momentum fornecidos por Williams %R ou um estocástico lento, enquanto um esquema de dimensionamento de posição tipo martingale aumenta a exposição após sequências perdedoras.

## Lógica de entrada
- Trabalhar no período configurável (padrão: velas de 1 hora).
- Aguardar um fechamento de vela fresco.
- Exigir que Williams %R (período de retrocesso de entrada 350) esteja acima do limiar de sobrecompra (padrão -10).
- Exigir que a linha principal do MACD (configuração 12/120/9) permaneça acima de um limiar altista (padrão 0.178) para confirmar o momentum ascendente antes de desvanecê-lo.
- Detectar um cruzamento descendente do %K estocástico rápido (comprimento 10, retardamento 3) abaixo do nível de disparo de entrada (padrão 90). Apenas este evento de cruzamento pode produzir um novo vendido.
- Cada sinal qualificado envia uma ordem de venda de mercado adicional. Múltiplas ordens vendidas podem se acumular, seguindo a lógica de volume martingale.

## Lógica de saída
- Rastrear o lucro flutuante de todos os vendidos abertos em pips usando o tamanho de pip do instrumento.
- Se apenas um vendido estiver aberto e o lucro médio exceder o alvo de operação única (padrão 10 pips) **e** Williams %R cair abaixo do limiar de saída (padrão -80), fechar a posição.
- Se mais de um vendido estiver aberto e o lucro médio da cesta exceder o alvo de cesta (padrão 15 pips) **e** o %K estocástico lento (comprimento 90, retardamento 1) cair abaixo do gatilho de sobrevenda (padrão 12), fechar toda a cesta.
- Um take-profit de segurança adicional fecha a cesta quando o ganho médio atinge a distância de take-profit configurada (padrão 100 pips).

## Dimensionamento de posição
- Começar com o volume base (padrão 0.01 lotes).
- Após um ciclo lucrativo (aumento do PnL realizado), redefinir o próximo volume de ordem para o volume base.
- Após um ciclo perdedor (redução do PnL realizado), multiplicar o próximo volume de ordem pelo multiplicador martingale (padrão 1.6). Os volumes são automaticamente alinhados ao passo de volume do instrumento.

## Gestão de risco
- A estratégia registra uma ordem protetora de take-profit usando a distância de pip configurada. Nenhum stop-loss fixo é usado; as saídas são impulsionadas por filtros de indicadores.
- A proteção de início é ativada uma vez, conforme exigido pelas diretrizes de conversão do StockSharp.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Período usado para os cálculos. | Velas de 1H |
| `EntryWilliamsLength` | Período de retrocesso de Williams %R para entradas. | 350 |
| `ExitWilliamsLength` | Período de retrocesso de Williams %R para saídas. | 350 |
| `EntryStochasticLength` / `Signal` / `Slowing` | Configurações do estocástico rápido para o cruzamento de entrada. | 10 / 1 / 3 |
| `ExitStochasticLength` / `Signal` / `Slowing` | Configurações do estocástico lento para confirmação de saída. | 90 / 7 / 1 |
| `MacdFastLength` / `MacdSlowLength` / `MacdSignalLength` | Parâmetros do MACD. | 12 / 120 / 9 |
| `EntryWilliamsThreshold` | Valor mínimo de Williams %R necessário antes de vender. | -10 |
| `ExitWilliamsThreshold` | Nível de Williams %R que confirma saída para uma única operação. | -80 |
| `EntryStochasticTrigger` | Nível que o estocástico rápido deve cruzar para baixo para disparar entradas. | 90 |
| `ExitStochasticTrigger` | Nível abaixo do qual o estocástico lento deve cair para fechar cestas. | 12 |
| `MacdThreshold` | Valor mínimo da linha principal do MACD. | 0.178 |
| `SingleTradeTargetPips` | Alvo de lucro (pips) quando apenas um vendido está ativo. | 10 |
| `MultiTradeTargetPips` | Alvo de lucro (pips) quando múltiplos vendidos estão ativos. | 15 |
| `TakeProfitPips` | Distância rígida de take-profit (pips). | 100 |
| `InitialVolume` | Tamanho base da ordem. | 0.01 |
| `MartingaleMultiplier` | Multiplicador aplicado após uma perda quando o martingale está habilitado. | 1.6 |
| `UseMartingale` | Habilitar ou desabilitar escalada martingale. | true |

## Notas
- A estratégia opera apenas posições vendidas e assume convenções de pip do tipo Forex ao medir lucros.
- Os cálculos de lucro médio tratam cada entrada igualmente, refletindo o bloco MetaTrader que calculava a média de pips por operação.
- Ajuste os limiares ou desabilite o martingale (`UseMartingale = false`) para reduzir o risco em pares altamente voláteis.
