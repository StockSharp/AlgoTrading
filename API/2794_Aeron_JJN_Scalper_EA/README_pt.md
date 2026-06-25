# Estratégia Aeron JJN Scalper EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port de alto nível no StockSharp do assessor especialista **Aeron JJN Scalper**. Observa velas terminadas, identifica situações específicas de reversão de duas barras e coloca ordens stop simuladas na abertura da vela oposta mais recente. Quando o mercado atinge o nível de stop armazenado, a estratégia entra com uma ordem a mercado, aplica metas de risco baseadas em ATR e gerencia a operação com um trailing stop baseado em pips.

Ideias-chave:

* A direção da operação é decidida por um padrão de reversão de duas velas de alta/baixa.
* Os níveis de entrada vêm do preço de abertura da última vela forte na direção oposta.
* Um valor ATR(8) medido na barra de sinal define as distâncias de stop-loss e take-profit.
* A lógica do trailing stop move o nível de proteção quando o preço avança pelos offsets de pip configurados.
* Os níveis pendentes expiram automaticamente após o número configurado de minutos.

## Regras de negociação
### Detecção de sinal
1. Trabalhar apenas com velas terminadas do período configurado (padrão: 1 minuto).
2. Calcular o tamanho do pip a partir do passo de preço do instrumento e multiplicar por 10 para preços de 3 ou 5 decimais para imitar o comportamento de pip do MetaTrader.
3. Manter uma janela deslizante das últimas 120 velas para buscar barras de referência.
4. Detectar uma **configuração de compra** quando:
   * A vela atual fecha acima de sua abertura (de alta), e
   * A vela anterior é de baixa com tamanho de corpo maior que `DojiDiff1Pips`.
   * Procurar para trás a última vela de baixa cujo corpo exceda `DojiDiff2Pips`; seu preço de abertura torna-se o nível de buy stop.
5. Detectar uma **configuração de venda** quando:
   * A vela atual fecha abaixo de sua abertura (de baixa), e
   * A vela anterior é de alta com tamanho de corpo maior que `DojiDiff1Pips`.
   * Procurar para trás a última vela de alta cujo corpo exceda `DojiDiff2Pips`; seu preço de abertura torna-se o nível de sell stop.
6. Ignorar novas configurações se já houver um nível pendente na mesma direção, ou se o valor ATR para a vela ainda não estiver disponível.

### Gestão de níveis pendentes
* O nível armazenado é tratado como uma ordem stop pendente. É descartado se o preço permanecer abaixo (comprado) ou acima (vendido) do gatilho até o tempo de expiração `ResetMinutes` decorrer.
* Quando o preço toca o nível em uma vela posterior (máxima ≥ nível de compra ou mínima ≤ nível de venda), a estratégia envia uma ordem a mercado dimensionada para reverter qualquer exposição existente e adicionar contratos `Volume`.
* Entrar em uma posição comprada limpa qualquer nível de venda pendente e vice-versa.

### Stop-loss, take-profit e trailing
* Ao entrar, a estratégia registra o valor ATR(8) da vela de sinal.
  * Operações compradas: stop-loss = `entry - ATR`, take-profit = `entry + ATR`.
  * Operações vendidas: stop-loss = `entry + ATR`, take-profit = `entry - ATR`.
* Em cada vela terminada, a estratégia:
  * Verifica se o preço atingiu o stop-loss ou take-profit e sai com uma ordem a mercado se tocado.
  * Aplica trailing quando o preço se moveu pelo menos `TrailingStopPips + TrailingStepPips` a favor da posição. O novo stop fica `TrailingStopPips` atrás do último fechamento. O stop nunca recua.
* Se a posição for fechada manualmente, o estado interno é redefinido automaticamente.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `Volume` | 0.1 | Tamanho da posição líquida usado para entradas; a estratégia adiciona a posição atual absoluta para inverter a direção quando necessário. |
| `TrailingStopPips` | 5 | Distância base do trailing stop (convertida para unidades de preço). |
| `TrailingStepPips` | 5 | Avanço adicional necessário antes de mover o trailing stop novamente. |
| `ResetMinutes` | 10 | Tempo de expiração para um nível pendente armazenado (minutos). |
| `DojiDiff1Pips` | 10 | Tamanho mínimo de corpo (em pips) para a vela de reversão que precede o sinal. |
| `DojiDiff2Pips` | 4 | Tamanho mínimo de corpo (em pips) para a vela usada como nível de referência de entrada. |
| `CandleType` | Período de 1 minuto | Tipo de dados de vela usado para cálculos. |

## Notas de implementação
* A estratégia opera puramente em velas terminadas e usa níveis em memória em vez de ordens stop reais; quando o nível é violado, uma ordem a mercado é enviada imediatamente. Isso reflete o comportamento original do EA dentro da API de alto nível do StockSharp.
* ATR(8) é calculado com `AverageTrueRange` e armazenado em cache para que as distâncias originais de stop/alvo permaneçam constantes para cada operação.
* A conversão de pip reproduz o ajuste do MetaTrader para cotações de 3 e 5 dígitos. Se o instrumento não tiver `PriceStep`, um passo padrão de `1` é usado.
* Até 120 velas históricas são armazenadas para replicar o look-back original de `CopyRates` de 100 barras com alguma margem de segurança.
* Não há port em Python para esta estratégia.

## Uso
1. Anexar a estratégia ao instrumento e portfólio desejados.
2. Ajustar o período das velas, os offsets de pip e os filtros baseados em ATR para adequar ao instrumento.
3. Iniciar a estratégia; ela rastreará sinais, enviará ordens a mercado quando os níveis de gatilho forem tocados e gerenciará as saídas automaticamente.
