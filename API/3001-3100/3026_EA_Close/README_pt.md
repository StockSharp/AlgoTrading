# Estratégia EA Close
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia EA Close** é um port direto do StockSharp do consultor especialista MQL5 original "EA Close" criado por Vladimir Karputov. A estratégia combina um Índice de Canal de Commodities (CCI), uma média móvel ponderada (WMA) e um oscilador estocástico para detectar movimentos de exaustão ao final dos recuos. As ordens são avaliadas apenas uma vez por vela completada para imitar a lógica de "nova barra" usada no EA fonte.

A implementação do StockSharp mantém o conjunto de parâmetros e a estrutura da versão MQL para que as otimizações existentes possam ser reutilizadas. Os sinais são gerados a partir da vela completada anterior, o que torna o comportamento determinístico quando a estratégia é reproduzida em dados históricos.

## Indicadores
- **Commodity Channel Index (CCI)** – identifica extremos de sobrecompra e sobrevenda em relação ao preço médio durante um período configurável.
- **Weighted Moving Average (WMA)** – atua como filtro de microtendência; o EA original usa uma LWMA de 1 período do preço ponderado, que na prática se comporta como um preço de vela levemente suavizado. Neste port, a WMA é aplicada diretamente ao fluxo de velas.
- **Oscilador Estocástico (linha %K)** – confirma o esgotamento do momentum usando níveis clássicos de sobrecompra e sobrevenda.

## Lógica de negociação
1. **Configuração comprada**
   - O CCI da vela anterior cai abaixo de `-CciLevel`.
   - O %K estocástico anterior está abaixo de `StochasticLevelDown`.
   - A abertura da vela anterior está acima do valor WMA dessa vela.
   - Se essas condições se alinharem e a posição líquida atual for não positiva, a estratégia compra. A exposição vendida existente é compensada antes de abrir a nova posição comprada.
2. **Configuração vendida**
   - O CCI da vela anterior sobe acima de `CciLevel`.
   - O %K estocástico anterior está acima de `StochasticLevelUp`.
   - O fechamento da vela anterior está abaixo do valor WMA dessa vela.
   - Quando verdadeiro e a posição é não negativa, a estratégia vende. Qualquer posição comprada aberta é fechada na mesma ordem de mercado.

Apenas dados de velas finalizadas são usados. Isso espelha o gate `OnTick` de nova barra no script MQL e previne o repintamento intrabarra.

## Gestão de risco
`StartProtection` é habilitado durante `OnStarted`, reproduzindo as distâncias fixas de stop-loss e take-profit do código MQL. As distâncias são configuradas em **pips**. O helper converte pips para unidades de preço multiplicando o passo de preço do instrumento por 10 quando a precisão do passo tem três ou cinco casas decimais (ex.: 0,001 ou 0,00001), correspondendo ao ajuste de dígitos do EA para cotações de 3/5 dígitos. Definir uma distância como zero desabilita esse tramo de proteção.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `Volume` | Tamanho de ordem usado para entradas de mercado. | 1 |
| `StopLossPips` | Distância de stop-loss fixa medida em pips. | 35 |
| `TakeProfitPips` | Distância de take-profit fixa medida em pips. | 75 |
| `CciPeriod` | Comprimento de médio do indicador CCI. | 14 |
| `CciLevel` | Limiar absoluto que define os extremos do CCI. | 120 |
| `MaPeriod` | Comprimento do filtro de média móvel ponderada. | 1 |
| `StochasticLength` | Janela de look-back para o oscilador estocástico (faixa máx/mín). | 5 |
| `StochasticKPeriod` | Fator de suavização aplicado à linha %K. | 3 |
| `StochasticDPeriod` | Fator de suavização aplicado à linha %D. | 3 |
| `StochasticLevelUp` | Limiar de sobrecompra para a linha %K. | 70 |
| `StochasticLevelDown` | Limiar de sobrevenda para a linha %K. | 30 |
| `CandleType` | Série de velas usada como fonte de dados. | Período de 1 hora |

## Notas de uso
- A estratégia armazena valores de indicador e preço da vela finalizada mais recente e avalia sinais na próxima abertura de barra, replicando a lógica de deslocamento de array (`CopyBuffer(..., start=1)`) no EA.
- As ordens de mercado têm tamanho para neutralizar qualquer exposição oposta e simultaneamente abrir a nova posição, correspondendo de perto ao helper `ClosePositions` em MQL.
- O `StochasticOscillator` do StockSharp usa `Length` como janela de look-back, `KPeriod` para suavização de %K e `DPeriod` para suavização de %D, equivalente aos parâmetros `iStochastic` (K-period, slowing e D-period respectivamente).
- Como o StockSharp trabalha com velas agregadas em vez de callbacks de tick, não é necessária lógica adicional de atualização de taxa; a assinatura de dados garante que os indicadores recebam velas completas.

## Notas de conversão
- Nenhuma implementação em Python é fornecida intencionalmente, alinhando-se aos requisitos da tarefa de conversão.
- A média móvel ponderada opera na série de velas; se precisar do preço ponderado exato do MT5 `(High + Low + 2 * Close) / 4`, pré-processe os valores de vela antes de alimentá-los na WMA.
- As ordens de proteção são gerenciadas pela plataforma via `StartProtection`, portanto registros explícitos de stop/take após cada negociação não são necessários.
