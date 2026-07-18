# Estratégia MACD No Sample
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
MACD No Sample é um port do consultor especialista MetaTrader 5 `MACD No Sample`. A estratégia combina uma verificação de inclinação de média móvel com cruzamentos da linha de sinal MACD enquanto impõe uma amplitude mínima do MACD expressa em pips. Quando um setup altista é confirmado, a exposição vendida existente é fechada antes de entrar comprado; setups baixistas fazem o oposto. O gerenciamento de risco reflete o EA original com lógica de stop-loss, take-profit e trailing baseados em pips, mais um modo opcional de dimensionamento de posição por percentual de risco.

## Lógica da Estratégia
### Preparação do Indicador
* **Filtro de média móvel** – uma média móvel configurável (SMA, EMA, SMMA ou LWMA) aplicada a um preço de vela selecionável (fechamento, abertura, máxima, mínima, mediano, típico ou ponderado). A inclinação (`MA[0] > MA[1]` ou `<`) define a direção da tendência.
* **Sinal MACD** – o MACD é calculado a partir de comprimentos de EMA rápida/lenta e comprimento de sinal independentes, usando o preço aplicado escolhido. As linhas MACD e sinal brutas são monitoradas para detectar cruzamentos novos e a magnitude absoluta do MACD é comparada contra um limiar baseado em pips.

### Regras de Entrada
* **Entradas compradas**
  * A média móvel está subindo na última vela terminada.
  * O MACD está abaixo de zero, mas acabou de cruzar acima da linha de sinal (MACD atual > sinal atual enquanto MACD anterior < sinal anterior).
  * O valor absoluto do MACD supera o limiar pip configurado (convertido para unidades de preço).
  * Posições vendidas existentes são fechadas antes de uma ordem comprada ser colocada.
* **Entradas vendidas**
  * A média móvel está caindo na última vela terminada.
  * O MACD está acima de zero, mas acabou de cruzar abaixo da linha de sinal (MACD atual < sinal atual enquanto MACD anterior > sinal anterior).
  * O valor absoluto do MACD supera o limiar pip.
  * Posições compradas existentes são fechadas antes de uma ordem vendida ser colocada.

### Gestão de Saída
* **Stop-loss / take-profit fixo** – distâncias de pips opcionais convertidas em deslocamentos de preço a partir do preço de entrada. Definir qualquer parâmetro como `0` desativa o nível correspondente.
* **Trailing stop** – ativa quando a distância do trailing stop é positiva. A estratégia rastreia o melhor preço alcançado desde a entrada, deslocando o stop pelo menos a distância do passo de trailing (ambos expressos em pips) sem nunca afrouxá-lo.
* **Dimensionamento baseado em risco (opcional)** – quando habilitado, o volume da ordem é derivado do valor do portfólio, da distância do stop-loss e do percentual de risco configurado. Os volumes são alinhados ao `VolumeStep` do instrumento e restritos por `MinVolume`/`MaxVolume` quando disponíveis.

## Notas de Implementação
* Usa a API de alto nível através de `SubscribeCandles()` com um pipeline de indicadores manual dentro do callback `ProcessCandle`; nenhuma chamada `GetValue` de indicadores é usada.
* As entradas do indicador respeitam as seleções de preço aplicado e dependem das implementações de média móvel e MACD do StockSharp.
* A detecção do tamanho do pip reflete a lógica original do EA multiplicando o passo de preço por dez em instrumentos de três e cinco dígitos.
* A lógica de stop e trailing fecha a posição via ordens de mercado quando os níveis calculados são violados; nenhuma ordem de stop separada é registrada.
* Apenas a implementação em C# é fornecida; não há versão Python para esta estratégia.

## Parâmetros
* **Volume** – volume de negociação fixo para ordens de mercado.
* **Stop Loss (pips)** – distância de stop protetor; `0` o desativa.
* **Take Profit (pips)** – distância de alvo de lucro; `0` o desativa.
* **Trailing Stop (pips)** – distância de trailing; `0` desativa o trailing.
* **Trailing Step (pips)** – melhora mínima em pips antes que o trailing stop seja ajustado.
* **Position Sizing** – escolha entre dimensionamento de volume fixo e por percentual de risco.
* **Risk Percent** – percentual do portfólio usado quando o dimensionamento por risco está ativo.
* **MA Period / Method / Price** – configuração para o filtro de média móvel.
* **MACD Fast / Slow / Signal** – comprimentos de EMA para MACD.
* **MACD Price** – preço aplicado usado para o cálculo do MACD.
* **MACD Level (pips)** – magnitude mínima absoluta do MACD para validar uma operação.
* **Candle Type** – período que impulsiona as atualizações do indicador.
