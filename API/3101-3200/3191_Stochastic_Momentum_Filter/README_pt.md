# Estratégia Stochastic Momentum Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Stochastic Momentum Filter** é um port do StockSharp do expert advisor do MetaTrader `Stochastic.mq4` (pasta `MQL/23473`). O robô original combina dois osciladores estocásticos, médias móveis ponderadas lineares (LWMA), um filtro de desvio de momentum e uma verificação de tendência MACD de período maior. Esta versão em C# recria os mesmos blocos de construção no topo da API de alto nível do StockSharp e mantém o fluxo de trabalho de confirmação multicamadas:

1. **Filtro de tendência** – uma LWMA rápida deve estar acima (ou abaixo) de uma LWMA lenta antes que trades longos (ou curtos) sejam permitidos.
2. **Confirmação do oscilador** – tanto um estocástico rápido (5/2/2) quanto um estocástico lento (21/4/10) devem concordar em zonas de sobrevendido/sobrecomprado.
3. **Desvio de momentum** – pelo menos uma das três leituras de momentum mais recentes deve desviar da linha base 100 em mais de um limite configurável, correspondendo ao uso da função MT4 `iMomentum` pelo expert.
4. **MACD de período maior** – a linha principal do MACD em um período maior configurável deve permanecer acima da linha de sinal para longos (e abaixo para curtos). O período padrão de 30 dias aproxima o filtro mensal original.
5. **Lógica de risco** – stop loss, take profit e trailing opcional são tratados através do `StartProtection`, espelhando os parâmetros protetores do EA. As reversões de posição fecham automaticamente a exposição oposta antes de estabelecer a nova posição líquida.

A estratégia assina dois fluxos de velas: o período de negociação e o período maior que alimenta o filtro MACD. Todos os cálculos são realizados com indicadores do StockSharp e processados através dos helpers de alto nível `Bind`.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `StochasticBuyLevel` | `30` | Nível de sobrevendido que ambos os osciladores estocásticos devem romper para configurações longas. |
| `StochasticSellLevel` | `80` | Nível de sobrecomprado que ambos os osciladores estocásticos devem atingir para configurações curtas. |
| `FastMaPeriod` | `6` | Comprimento do filtro de tendência LWMA rápido. |
| `SlowMaPeriod` | `85` | Comprimento do filtro de tendência LWMA lento. |
| `FastStochasticPeriod` | `5` | Período `%K` do oscilador estocástico rápido. |
| `FastStochasticSignal` | `2` | Período de suavização `%D` do estocástico rápido. |
| `FastStochasticSmoothing` | `2` | Suavização extra aplicada ao estocástico rápido (corresponde ao "slowing" do MT4). |
| `SlowStochasticPeriod` | `21` | Período `%K` do oscilador estocástico lento. |
| `SlowStochasticSignal` | `4` | Período de suavização `%D` do estocástico lento. |
| `SlowStochasticSmoothing` | `10` | Suavização extra aplicada ao estocástico lento. |
| `MomentumPeriod` | `14` | Lookback do oscilador de momentum (igual ao `iMomentum` do MT4). |
| `MomentumThreshold` | `0.3` | Desvio absoluto mínimo da linha base 100 exigido dentro dos últimos três valores de momentum. |
| `MacdFastPeriod` | `12` | Período EMA rápido para o MACD de período maior. |
| `MacdSlowPeriod` | `26` | Período EMA lento para o MACD de período maior. |
| `MacdSignalPeriod` | `9` | Período EMA de sinal para o MACD de período maior. |
| `TakeProfitPoints` | `50` | Distância de take-profit (em pontos de preço). Defina como `0` para desabilitar. |
| `StopLossPoints` | `20` | Distância de stop-loss (em pontos de preço). Defina como `0` para desabilitar. |
| `EnableTrailing` | `true` | Habilita o trailing do StockSharp da proteção de stop. |
| `TradeVolume` | `1` | Tamanho de posição líquida alvo em cada sinal. |
| `MaxNetPositions` | `1` | Limita a exposição líquida empilhada (multiplica `TradeVolume`). |
| `CandleType` | período de `15m` | Período de negociação principal. |
| `HigherTimeframe` | período de `30d` | Período usado para confirmação MACD. |

## Lógica de negociação
1. **Preparação de indicadores** – a estratégia vincula ambas as LWMAs, ambos os osciladores estocásticos, o indicador de momentum e o MACD aos seus respectivos fluxos de velas.
2. **Histórico de momentum** – a distância absoluta do oscilador de momentum de 100 é armazenada para as últimas três barras concluídas. Isso replica os arrays `MomLevelB/MomLevelS` do EA.
3. **Regras de entrada**
   - **Longo**: LWMA rápida acima da LWMA lenta, valores de `%K` e `%D` de ambos os estocásticos abaixo de `StochasticBuyLevel`, desvio de momentum acima de `MomentumThreshold`, e linha principal do MACD acima da linha de sinal.
   - **Curto**: LWMA rápida abaixo da LWMA lenta, valores de `%K` e `%D` de ambos os estocásticos acima de `StochasticSellLevel`, desvio de momentum acima do limite, e linha principal do MACD abaixo da linha de sinal.
4. **Gerenciamento de posições** – as ordens são enviadas com `BuyMarket`/`SellMarket`. Quando um sinal de reversão aparece, a estratégia fecha automaticamente qualquer exposição líquida oposta antes de estabelecer a nova direção.
5. **Proteção** – `StartProtection` aplica as distâncias configuradas de take-profit e stop-loss (em pontos). Quando `EnableTrailing` é true, o StockSharp gerencia o trailing de stop de forma semelhante à rotina de trailing do EA.

## Diferenças em relação à versão MQL
- **Escalonamento de volume**: o EA escala tamanhos de lote usando `LotExponent` e permite vários tickets simultâneos. O port do StockSharp foca na exposição líquida e visa um único `TradeVolume` por direção (limitado por `MaxNetPositions`).
- **Gestão de margem**: verificações de margem, paradas de equity e funções de notificação do script original não são reproduzidas porque dependem de APIs de conta MT4.
- **Níveis de congelamento**: verificações de nível de congelamento específicas de corretor de baixo nível são omitidas; o roteamento de ordens do StockSharp lida com as restrições da bolsa.
- **Toggle de break-even**: o helper "move to breakeven" do MT4 é substituído pela proteção de trailing do StockSharp.

## Notas de uso
1. Atribuir um instrumento e conector, em seguida iniciar a estratégia. Ela assinará automaticamente tanto o período de negociação quanto o período maior exigido pelo filtro MACD.
2. Se sua fonte de dados não suportar um tipo de vela de 30 dias, ajuste `HigherTimeframe` para um intervalo suportado (por exemplo, semanal ou diário).
3. Definir `TradeVolume` para corresponder às suas unidades de portfólio.
4. Definir `TakeProfitPoints`/`StopLossPoints` como zero se as ordens protetoras devem ser desabilitadas.
5. Todos os comentários dentro do código são escritos em inglês, e o recuo usa tabulações.

## Arquivos
- `CS/StochasticMomentumFilterStrategy.cs` – implementação do StockSharp da lógica da estratégia.
- `README.md` – documentação em inglês (este arquivo).
- `README_ru.md` – documentação em russo.
- `README_zh.md` – documentação em chinês.
