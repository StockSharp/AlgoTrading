# Estratégia XAng Zad C TM MM Rec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port em C# do assessor especializado do MetaTrader **Exp_XAng_Zad_C_Tm_MMRec**. Ela opera envelopes de preço adaptativos calculados pelo indicador personalizado *XAng Zad C* e adiciona uma janela de trading baseada em tempo junto com um contador simples de gestão de dinheiro. O objetivo é capturar rompimentos quando as linhas adaptativas superior e inferior se cruzam, escalando dinamicamente o tamanho da posição após um número configurável de operações perdedoras.

### Lógica principal
- **Indicador** – o indicador XAng Zad C produz um canal adaptativo superior e inferior. A versão C# reproduz o cálculo do envelope e suporta vários suavizadores de média móvel (SMA, EMA, SMMA, LWMA). Suavizadores exóticos do script original recorrem a EMA.
- **Sinais de entrada** – quando a vela anterior mostra a linha superior acima da inferior e a barra atual fecha com a linha superior caindo abaixo da inferior, um rompimento altista é detectado. A configuração oposta produz um rompimento baixista. O parâmetro `SignalShift` define quantas velas fechadas para trás devem ser comparadas.
- **Sinais de saída** – sinalizadores opcionais permitem fechar posições compradas quando a linha superior retorna abaixo da inferior e fechar vendidas no evento inverso. As posições também são fechadas imediatamente quando a janela de trading configurada termina.
- **Gestão de dinheiro** – a estratégia mantém uma lista de resultados de operações históricas. Se as operações perdedoras mais recentes de `BuyLossTrigger` (ou `SellLossTrigger`) aparecem dentro das últimas `BuyTotalTrigger` (ou `SellTotalTrigger`) operações, a próxima posição usa o volume reduzido. Caso contrário, o volume normal é restaurado.
- **Controle de risco** – metas estáticas de stop-loss e take-profit são aplicadas em múltiplos do passo de preço do instrumento. Se algum nível for atingido durante a vela, a posição é fechada no preço correspondente.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `NormalVolume` | Tamanho de ordem padrão usado quando não há sequência perdedora recente. |
| `ReducedVolume` | Tamanho de ordem aplicado após uma sequência de operações perdedoras. |
| `BuyTotalTrigger` / `SellTotalTrigger` | Número de operações históricas inspecionadas ao avaliar o contador de perdas. |
| `BuyLossTrigger` / `SellLossTrigger` | Operações perdedoras necessárias (dentro da janela acima) para mudar para o volume reduzido. |
| `EnableBuyEntries` / `EnableSellEntries` | Permitir entradas compradas ou vendidas. |
| `EnableBuyExit` / `EnableSellExit` | Permitir sinais de saída automáticos baseados em cruzamentos do canal. |
| `UseTradingWindow` | Habilitar o filtro de tempo. Fora da janela todas as posições são fechadas e nenhuma nova ordem é enviada. |
| `WindowStart` / `WindowEnd` | Horários de início e fim da janela de trading diária (UTC). A janela pode abranger a meia-noite. |
| `StopLoss` | Distância do stop-loss expressa em múltiplos de `Security.PriceStep`. Defina como `0` para desabilitar. |
| `TakeProfit` | Distância do alvo de lucro expressa em múltiplos de `Security.PriceStep`. Defina como `0` para desabilitar. |
| `SignalShift` | Número de velas já fechadas usadas para a comparação de cruzamento. |
| `CandleType` | Tipo de dados de vela usado para o indicador (padrão: velas de 4 horas). |
| `SmoothMethods` | Suavizador de média móvel dentro do indicador. Valores não suportados usam automaticamente EMA. |
| `MaLength` | Comprimento de suavização para o indicador. |
| `MaPhase` | Parâmetro de fase adicional retido do indicador original (atualmente informativo). |
| `Ki` | Razão que controla quão rapidamente os envelopes adaptativos reagem às mudanças de preço. |
| `AppliedPrices` | Fonte de preço usada para alimentar o indicador (fechamento, abertura, mediana, etc.). |

## Notas em comparação com a versão MQL5
- Os auxiliares de gestão de dinheiro do MetaTrader dependiam do histórico global de operações. A versão C# rastreia operações concluídas localmente e aplica a mesma lógica de disparo.
- O dimensionamento de lotes é expresso diretamente como volume de estratégia. Ajuste `NormalVolume`/`ReducedVolume` para corresponder à quantidade alvo para sua plataforma.
- As janelas de tempo são configuradas com valores `TimeSpan`. Quando `WindowStart` é igual a `WindowEnd`, o trading é desabilitado (correspondendo ao comportamento de janela de largura zero do script original).
- A estratégia assume reversões de posição completas e não mantém posições parciais de sinais anteriores.
- Tipos de suavização não suportados (JJMA, JurX, ParMA, T3, VIDYA, AMA) usam EMA por padrão. Considere estender `CreateMovingAverage` se precisar de uma alternativa específica.

## Dicas de uso
1. Escolha um tipo de vela que corresponda ao período do indicador usado no MetaTrader (padrão: H4).
2. Ajuste as distâncias de stop-loss e take-profit baseadas no tamanho do tick do instrumento para aproximar os valores baseados em pontos do EA original.
3. Otimize os gatilhos de gestão de dinheiro para refletir a volatilidade do ativo e sua tolerância ao risco.
4. Monitore o comportamento do indicador em um gráfico (linhas de canal superior/inferior) para confirmar que o indicador reconstruído atende às expectativas antes do trading ao vivo.
