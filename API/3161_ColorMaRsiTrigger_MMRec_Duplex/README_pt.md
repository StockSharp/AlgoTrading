# Estratégia ColorMaRsi Trigger MMRec Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é um port da API de alto nível do StockSharp do especialista MetaTrader **Exp_ColorMaRsi-Trigger_MMRec_Duplex.mq5**. Executa dois blocos MaRsi-Trigger independentes – um para oportunidades longas e outro para oportunidades curtas. Cada bloco avalia um sinal composto gerado pela comparação de uma média móvel rápida e lenta juntamente com um RSI rápido e lento. O valor composto é limitado ao intervalo `[-1, 1]`, reproduzindo o comportamento do indicador original: `+1` marca alinhamento de alta, `-1` marca alinhamento de baixa e `0` indica condições mistas.

Um módulo de gestão de capital "MMRec" monitora os últimos trades para cada direção. Quando um número configurável de perdas aparece dentro de uma janela móvel, o próximo trade muda para um volume reduzido até que o desempenho se recupere. Isso reproduz a lógica de dimensionamento de posição adaptativo da biblioteca MetaTrader `TradeAlgorithms.mqh` usada pelo especialista.

## Lógica de trading

1. **Pipeline do indicador** (por bloco):
   - Calcular uma média móvel rápida (`MA_fast`) e uma lenta (`MA_slow`) no preço aplicado e período de tempo selecionados.
   - Calcular um RSI rápido (`RSI_fast`) e um lento (`RSI_slow`) em possivelmente diferentes preços aplicados.
   - Construir uma pontuação de cor: começar em `0`, adicionar `+1` se `MA_fast > MA_slow` ou `-1` caso contrário, depois adicionar `+1` se `RSI_fast > RSI_slow` ou `-1` caso contrário. Limitar o resultado a `[-1, 1]`.
   - Armazenar o histórico de pontuações e lê-lo com o deslocamento `SignalBar` configurado (o padrão corresponde à implementação do MetaTrader).

2. **Bloco longo**:
   - **Entrada**: permitida quando nenhuma posição longa está aberta (posições curtas são cobertas primeiro). A cor anterior (`SignalBar + 1`) deve ser `+1` enquanto a cor atual (`SignalBar`) é `≤ 0`, indicando que o bloco de alta acabou de se neutralizar.
   - **Saída**: quando a cor anterior se torna negativa (`-1`) e saídas estão habilitadas.

3. **Bloco curto**:
   - **Entrada**: permitida quando nenhuma posição curta está aberta (posições longas são fechadas primeiro). A cor anterior deve ser `-1` enquanto a cor atual é `≥ 0`, sinalizando uma transição fresca de baixa para neutro.
   - **Saída**: quando a cor anterior se torna positiva e saídas estão habilitadas.

4. **Stops e alvos**: distâncias opcionais de stop-loss e take-profit são expressas em passos de preço e reavaliadas em cada vela concluída. Cruzar qualquer limite fecha o respectivo posição imediatamente.

5. **Gestão de capital**: a estratégia armazena o resultado de cada trade concluído (por direção) e conta o número de perdas nos últimos `HistoryDepth` trades. Se a contagem de perdas atingir `LossTrigger`, a próxima ordem usa o volume reduzido. Caso contrário, o volume normal é usado.

## Parâmetros

| Grupo | Nome | Descrição | Padrão |
| --- | --- | --- | --- |
| Bloco Longo | `LongCandleType` | Período de tempo que alimenta o bloco MaRsi-Trigger longo. | `H4` |
|  | `LongAllowOpen` / `LongAllowClose` | Habilitar abertura / fechamento de posições longas. | `true` |
|  | `LongStopLossPoints` / `LongTakeProfitPoints` | Distâncias protetoras em pontos do instrumento. Definir como `0` para desabilitar. | `1000` / `2000` |
|  | `LongSignalBar` | Número de barras concluídas para deslocar ao amostrar os buffers do indicador. | `1` |
|  | `LongRsiPeriod` / `LongRsiLongPeriod` | Comprimentos de RSI rápido e lento. | `3` / `13` |
|  | `LongMaPeriod` / `LongMaLongPeriod` | Comprimentos de média móvel rápida e lenta. | `5` / `10` |
|  | `LongRsiPrice` / `LongRsiLongPrice` | Preço aplicado para RSI rápido / lento (Close, Open, High, Low, Median, Typical, Weighted). | `Weighted` / `Median` |
|  | `LongMaPrice` / `LongMaLongPrice` | Preço aplicado para MA rápida / lenta. | `Close` / `Close` |
|  | `LongMaType` / `LongMaLongType` | Algoritmos de média móvel (Simple, Exponential, Smoothed, Weighted). | `Exponential` / `Exponential` |
| Gestão de Capital | `LongNormalVolume` / `LongReducedVolume` | Volume de trade longo padrão e reduzido. | `0.1` / `0.01` |
|  | `LongHistoryDepth` | Número de trades longos recentes observados pelo filtro de gestão de capital. | `5` |
|  | `LongLossTrigger` | Contagem mínima de perdas dentro da janela para mudar para volume longo reduzido. | `3` |

| Grupo | Nome | Descrição | Padrão |
| --- | --- | --- | --- |
| Bloco Curto | `ShortCandleType` | Período de tempo que alimenta o bloco MaRsi-Trigger curto. | `H4` |
|  | `ShortAllowOpen` / `ShortAllowClose` | Habilitar abertura / fechamento de posições curtas. | `true` |
|  | `ShortStopLossPoints` / `ShortTakeProfitPoints` | Distâncias protetoras em pontos do instrumento. Definir como `0` para desabilitar. | `1000` / `2000` |
|  | `ShortSignalBar` | Número de barras concluídas para deslocar ao amostrar os buffers do indicador. | `1` |
|  | `ShortRsiPeriod` / `ShortRsiLongPeriod` | Comprimentos de RSI rápido e lento. | `3` / `13` |
|  | `ShortMaPeriod` / `ShortMaLongPeriod` | Comprimentos de média móvel rápida e lenta. | `5` / `10` |
|  | `ShortRsiPrice` / `ShortRsiLongPrice` | Preço aplicado para RSI rápido / lento. | `Weighted` / `Median` |
|  | `ShortMaPrice` / `ShortMaLongPrice` | Preço aplicado para MA rápida / lenta. | `Close` / `Close` |
|  | `ShortMaType` / `ShortMaLongType` | Algoritmos de média móvel (Simple, Exponential, Smoothed, Weighted). | `Exponential` / `Exponential` |
| Gestão de Capital | `ShortNormalVolume` / `ShortReducedVolume` | Volume de trade curto padrão e reduzido. | `0.1` / `0.01` |
|  | `ShortHistoryDepth` | Número de trades curtos recentes observados pelo filtro de gestão de capital. | `5` |
|  | `ShortLossTrigger` | Contagem mínima de perdas dentro da janela para mudar para volume curto reduzido. | `3` |

## Notas

- As opções de preço aplicado seguem a semântica do MetaTrader. Por exemplo, `Weighted` equivale a `(High + Low + 2 * Close) / 4` e `Typical` equivale a `(High + Low + Close) / 3`.
- Quando os blocos longo e curto compartilham o mesmo período de tempo (padrão), uma única assinatura de velas alimenta ambas as calculadoras.
- Definir o gatilho de perda como `0` força o volume reduzido imediatamente, espelhando o comportamento do helper original de gestão de capital.
- A estratégia usa ordens de mercado; o parâmetro `Deviation` do MetaTrader portanto não é necessário.
