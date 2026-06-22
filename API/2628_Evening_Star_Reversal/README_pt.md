# Estratégia de Reversão Evening Star
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port direto do Assessor Especializado **EveningStar.mq5** (MQL5 id 18507). Monitora a formação clássica de velas da Estrela Vespertina e abre uma posição assim que a próxima barra começa a ser negociada. A lógica foi reescrita sobre a API de alto nível do StockSharp mantendo os filtros de padrão e o gerenciamento de risco originais.

## Lógica de trading
1. A estratégia se subscreve ao período selecionado pelo parâmetro `CandleType`. Todo o processamento ocorre apenas em velas concluídas.
2. A cada vez que uma nova vela fecha, os últimos snapshots são armazenados em cache para que a janela de três velas definida por `Shift` possa ser avaliada.
3. O padrão Evening Star é considerado válido quando:
   - A vela *N-2* (a mais antiga) é altista (`open < close`).
   - A vela *N-1* (do meio) satisfaz a preferência `Candle2Bullish` (altista por padrão).
   - A vela *N* (a mais recente) é baixista (`open > close`).
   - Se `CheckCandleSizes` estiver habilitado, a vela do meio deve ter o menor corpo das três.
   - Se `ConsiderGap` estiver habilitado, deve haver uma lacuna entre os corpos das velas da mesma forma que no robô original (o tamanho da lacuna é igual a um pip calculado a partir do passo de preço do instrumento).
4. Uma vez confirmado o padrão, a estratégia verifica a direção selecionada por `Direction`:
   - `Short` (padrão) abre uma ordem de venda, correspondendo ao comportamento original da Evening Star.
   - `Long` permite executar a exposição exatamente oposta (mantido para paridade de funcionalidades com a versão MQL).
5. Antes de abrir uma posição, o algoritmo opcionalmente fecha a exposição oposta se `CloseOppositePositions` estiver configurado como `true`.
6. Os preços de stop-loss e take-profit são calculados a partir das distâncias em pips (`StopLossPips`, `TakeProfitPips`) usando o mesmo ajuste de 3/5 dígitos que existia no MetaTrader.
7. O tamanho da posição é derivado do valor atual da carteira e `RiskPercent`. Se o volume calculado for menor que o tamanho mínimo negociável, o sinal é ignorado.

## Gestão de posições
- Quando uma posição comprada está ativa, a estratégia monitora cada nova vela. Se o preço mínimo rompe o nível de stop ou o preço máximo atinge o nível de take-profit, toda a posição é fechada a mercado.
- Quando uma posição vendida está ativa, a mesma lógica é aplicada com comparações invertidas.
- Se o valor da carteira ou a distância ao stop forem iguais a zero, o tamanho da ordem não pode ser calculado, portanto a entrada é ignorada.

## Parâmetros
| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `Direction` | `Short` | Escolhe se o padrão deve abrir uma posição comprada ou vendida. |
| `TakeProfitPips` | `150` | Distância ao alvo de lucro expressa em pips. Definir como zero para desabilitar. |
| `StopLossPips` | `50` | Distância ao stop de proteção em pips. Um valor não positivo desabilita a operação. |
| `RiskPercent` | `5` | Porcentagem do patrimônio da carteira arriscada por operação. Usada para calcular o volume da ordem. |
| `Shift` | `1` | Número de barras ignoradas da vela mais recente antes de avaliar o padrão. |
| `ConsiderGap` | `true` | Requer uma lacuna entre corpos de velas como o Assessor Especializado original. |
| `Candle2Bullish` | `true` | Força a vela do meio a ser altista. Desabilitar para exigir uma vela do meio baixista. |
| `CheckCandleSizes` | `true` | Garante que a vela do meio tenha o menor corpo absoluto. |
| `CloseOppositePositions` | `true` | Fecha a exposição oposta antes de enviar a nova ordem. |
| `CandleType` | Período `1H` | Série de velas usada para análise. |

## Notas
- O tamanho do pip é derivado do passo de preço do instrumento. Para símbolos forex de 3 e 5 dígitos, um pip equivale a dez passos de preço, reproduzindo o comportamento do EA original.
- Se `StopLossPips` for zero, o tamanho da posição não pode ser calculado e o sinal é ignorado para evitar risco ilimitado.
- A estratégia recorta automaticamente o histórico em cache, de modo que o uso de memória permanece constante mesmo em sessões longas.
