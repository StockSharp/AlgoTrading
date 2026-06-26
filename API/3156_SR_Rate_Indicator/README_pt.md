# Estratégia de SR Rate Indicator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é um port em C# do especialista MetaTrader 5 **Exp_SR-RateIndicator**. Reproduz a lógica de trading original usando a API de alto nível do StockSharp e uma implementação personalizada do oscilador SR Rate. O indicador mede o quão longe o preço ponderado da vela está dentro de um canal de suporte/resistência suavizado e pinta um código de cor que destaca leituras extremas.

O algoritmo processa velas concluídas de um período de tempo configurável. Sempre que a cor do oscilador salta para o extremo de alta ou baixa, a estratégia fecha qualquer posição oposta e abre um novo trade na direção do sinal. Níveis de stop loss e take profit protetores são aplicados com as mesmas distâncias em pontos usadas na versão do MetaTrader.

## Oscilador SR Rate

O indicador calcula uma banda suavizada gaussiana em torno do preço usando um comprimento de janela configurável:

1. Para cada barra, o máximo, mínimo e fechamento ponderado são suavizados com pesos gaussianos unilaterais de comprimento seis.
2. O máximo suavizado mais alto e o mínimo suavizado mais baixo sobre a janela definem um intervalo dinâmico.
3. O fechamento ponderado suavizado atual é normalizado dentro desse intervalo e mapeado para o intervalo `[-100, 100]`.
4. O valor final do oscilador é convertido em cinco estados de cor: `0` (fortemente de baixa), `1` (levemente de baixa), `2` (neutro), `3` (levemente de alta) e `4` (fortemente de alta).

Uma cor fortemente de alta (`4`) indica que o preço atingiu o extremo superior do intervalo, enquanto uma cor fortemente de baixa (`0`) sinaliza uma visita ao extremo inferior.

## Regras de trading

1. Assinar velas do tipo configurado e calcular o oscilador SR Rate em cada barra concluída.
2. Deslocar a avaliação de sinais por `SignalBar` velas fechadas (padrão: uma barra atrás) para imitar o comportamento do Expert Advisor.
3. Quando a cor deslocada se torna `4` e a cor anterior é menor que `4`:
   - Fechar qualquer posição curta existente se as saídas longas estiverem habilitadas.
   - Abrir uma nova posição longa se as entradas longas estiverem habilitadas e nenhuma outra posição estiver ativa.
4. Quando a cor deslocada se torna `0` e a cor anterior é maior que `0`:
   - Fechar qualquer posição longa existente se as saídas curtas estiverem habilitadas.
   - Abrir uma nova posição curta se as entradas curtas estiverem habilitadas e nenhuma outra posição estiver ativa.
5. Apenas uma posição pode estar aberta a qualquer momento. Novos sinais são ignorados até que o trade anterior seja fechado.
6. Níveis opcionais de stop loss e take profit são expressos em pontos de preço e convertidos automaticamente para preços absolutos usando o passo de preço do instrumento.

## Parâmetros

| Nome | Descrição |
|------|-------------|
| `OrderVolume` | Volume de negociação usado para cada ordem de mercado. |
| `EnableLongEntries` | Habilitar/desabilitar abertura de posições longas. |
| `EnableShortEntries` | Habilitar/desabilitar abertura de posições curtas. |
| `EnableLongExits` | Fechar posições longas quando uma cor fortemente de baixa aparecer. |
| `EnableShortExits` | Fechar posições curtas quando uma cor fortemente de alta aparecer. |
| `StopLossPoints` | Distância do stop loss em pontos do instrumento (convertido usando o passo de preço). |
| `TakeProfitPoints` | Distância do take profit em pontos do instrumento (convertido usando o passo de preço). |
| `SlippagePoints` | Slippage máximo tolerado ao fechar posições. Preservado por compatibilidade; nenhum controle explícito de slippage é aplicado pela API de alto nível. |
| `CandleType` | Tipo de vela e período de tempo usados para calcular o indicador. |
| `SignalBar` | Número de barras fechadas para pular antes de analisar o histograma (padrão 1). |
| `WindowSize` | Comprimento da janela deslizante usada pela normalização SR Rate. |
| `HighLevel` | Nível do oscilador que define o extremo de alta (padrão +20). |
| `LowLevel` | Nível do oscilador que define o extremo de baixa (padrão -20). |

## Notas

- A estratégia funciona com qualquer instrumento que forneça velas OHLC padrão.
- Os sinais são processados apenas em velas concluídas; recálculos intrabarra são ignorados, assim como na implementação do MetaTrader.
- O tratamento de slippage no especialista original dependia das configurações de execução. As ordens de mercado do StockSharp já respeitam as regras do exchange, portanto o parâmetro `SlippagePoints` é mantido apenas para fins de documentação.
- O indicador armazena apenas a quantidade mínima de histórico necessária para avaliar a janela, evitando uso desnecessário de memória.
- A versão Python é omitida intencionalmente de acordo com as diretrizes do projeto.
