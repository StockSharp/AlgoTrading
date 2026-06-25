# Estratégia de Cruzamento de Médias Móveis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
- Conversão do consultor especialista MetaTrader 5 **"Crossing Moving Average (barabashkakvn's edition)"** do fonte `MQL/21515`.
- Implementa a lógica sobre a API de alto nível do StockSharp com assinaturas de velas e vinculação de indicadores.
- Projetado para instrumentos onde o momentum e os cruzamentos de médias móveis capturam reversões de tendência.
- Este pacote contém apenas a versão em C#. Uma tradução para Python é intencionalmente omitida conforme solicitado.

## Ideia Central
A estratégia monitora duas médias móveis configuráveis (rápida e lenta) com deslocamentos opcionais para frente e combina seu cruzamento com um filtro de confirmação de momentum. Um trade é aberto apenas quando:
1. A média rápida cruza a média lenta por pelo menos a distância mínima configurada (em pips) durante as duas barras completadas mais recentes.
2. O indicador de momentum sobe acima (para comprado) ou cai abaixo (para vendido) do limite definido pelo usuário e melhora na direção do trade.
3. A fonte de preço do sinal pode ser escolhida entre preços de vela de abertura, máximo, mínimo, fechamento, mediana, típico ou ponderado para imitar os modos de preço aplicado do MetaTrader.

## Gestão de Risco e Trade
- O **volume de ordem** é fixo por trade e é aplicado tanto ao entrar em uma nova posição quanto ao reverter uma posição existente.
- As distâncias de **stop-loss / take-profit** são configuradas em pips e automaticamente traduzidas em offsets de preço usando `Security.PriceStep`. Para instrumentos cotados com 3 ou 5 dígitos decimais, a estratégia multiplica o passo por 10 para reproduzir o tamanho de pip do MetaTrader.
- O **trailing stop** ativa após o preço se mover por `TrailingStop + TrailingStep` (em pips) desde a entrada. Uma vez ativado, o stop é movido para `preço atual - TrailingStop` para posições compradas (ou `preço atual + TrailingStop` para vendidas) sempre que puder avançar pelo menos `TrailingStep` pips.
- Os níveis protetores são avaliados em cada vela terminada: se o intervalo da vela toca o stop-loss ou take-profit, a posição é fechada ao mercado para imitar a execução de ordens no MetaTrader.

## Indicadores
- **Média Móvel Rápida** – período configurável, deslocamento e método de suavização (SMA, EMA, SMMA, WMA).
- **Média Móvel Lenta** – mesmas opções que a MA rápida.
- **Momentum** – período e fonte de preço idênticos às médias móvies. A estratégia detecta automaticamente se o indicador emite valores em torno de 0 ou 100 e aplica o filtro adequadamente.

## Lógica de Sinais
1. Aguardar até que todos os indicadores estejam completamente formados. O algoritmo mantém um histórico interno dos valores mais recentes para avaliar cruzamentos deslocados exatamente como no consultor especialista original.
2. Calcular a distância de preço entre as médias rápida e lenta nas duas barras anteriores (com deslocamentos aplicados). A linha rápida deve cruzar a linha lenta e superar o filtro de distância mínima.
3. Recuperar os valores de momentum nas mesmas barras. Para entradas compradas, o momentum atual deve ser maior que tanto o limite configurado quanto o valor de momentum anterior; para entradas vendidas, o oposto é necessário.
4. Se um novo sinal aparece enquanto a posição é oposta, a estratégia fecha a posição existente e imediatamente abre uma na nova direção com o tamanho de lote configurado.

## Referência de Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `OrderVolume` | Volume base usado para cada ordem de mercado. | `1` |
| `StopLossPips` | Distância de stop-loss em pips (0 desabilita o stop). | `50` |
| `TakeProfitPips` | Distância de take-profit em pips (0 desabilita o alvo). | `50` |
| `TrailingStopPips` | Distância do trailing stop em pips (0 desabilita o trailing). | `5` |
| `TrailingStepPips` | Melhoria mínima em pips necessária para mover o trailing stop. | `5` |
| `MinDistancePips` | Separação mínima entre MAs para validar o cruzamento. | `0` |
| `MomentumFilter` | Diferença mínima de momentum necessária para permitir entradas. | `0.1` |
| `FastPeriod` / `FastShift` | Comprimento da MA rápida e deslocamento horizontal (barras). | `13` / `1` |
| `SlowPeriod` / `SlowShift` | Comprimento da MA lenta e deslocamento horizontal (barras). | `34` / `3` |
| `MaMethod` | Tipo de suavização de média móvel (Simple, Exponential, Smoothed, Weighted). | `Exponential` |
| `AppliedPrice` | Preço da vela usado para cálculos do indicador. | `Close` |
| `MomentumPeriod` | Comprimento de retrospectiva do momentum em barras. | `14` |
| `CandleType` | Tipo de dados de velas fornecidas à estratégia. | `TimeFrame(1m)` |

## Notas Práticas
- Sempre garantir que `Security.PriceStep` esteja configurado para o instrumento; caso contrário, o gerenciamento de risco baseado em pips recorrerá a unidades de preço brutas.
- A lógica de trailing requer um `TrailingStepPips` positivo quando `TrailingStopPips` está habilitado — refletindo a validação original do MetaTrader.
- Como os níveis de stop e take são avaliados nos intervalos das velas, o uso de velas de maior resolução fornece uma aproximação mais próxima da execução baseada em ticks.
- Mensagens de registro em entradas e ajustes de trailing são incluídas para facilitar a depuração e a otimização de parâmetros.
