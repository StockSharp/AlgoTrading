# Estratégia de 800BB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o consultor especialista MetaTrader 4 "800BB" usando a API de alto nível do StockSharp. Ela entra em trades de reversão à média quando o preço perfura uma Bollinger Band muito longa e imediatamente volta a entrar no canal na próxima barra. O risco é controlado por meio de distâncias de stop e take-profit baseadas em ATR combinadas com dimensionamento dinâmico de posição baseado no percentual de risco configurado.

## Visão geral

- Funciona em qualquer instrumento e período fornecido através do parâmetro `CandleType`.
- Usa uma Bollinger Band de 800 períodos com um envelope de dois desvios padrão para detectar excursões extremas.
- Confirma entradas na barra que abre de volta dentro da banda logo após um fechamento externo.
- Dimensiona ordens estimando a distância de stop derivada do ATR em pips e aplicando o `RiskPercent` selecionado ao valor atual do portfólio.
- Replica o cálculo de pips do MetaTrader multiplicando o passo de preço por 10 quando o símbolo tem 3 ou 5 casas decimais.

## Lógica de trading

### Configuração longa

1. O candle completado anterior abriu ou fechou abaixo da banda inferior de Bollinger, sinalizando uma excursão de sobrevendido.
2. O candle atual abre em ou acima do nível anterior da banda inferior (o preço voltou a entrar no canal).
3. Nenhuma posição longa está atualmente ativa. Qualquer posição curta aberta é fechada antes de abrir a nova posição longa.
4. O tamanho da posição é calculado usando a distância de stop baseada em ATR e o percentual de risco configurado.
5. Uma ordem de compra a mercado é enviada na abertura do candle. O stop-loss é colocado `StopLossAtrMultiplier × ATR` abaixo da entrada, enquanto o take-profit é `TakeProfitAtrMultiplier × ATR` acima da entrada.

### Configuração curta

1. O candle completado anterior abriu ou fechou acima da banda superior de Bollinger, sinalizando uma excursão de sobrecomprado.
2. O candle atual abre em ou abaixo do nível anterior da banda superior (o preço voltou a entrar no canal).
3. Nenhuma posição curta está atualmente ativa. Qualquer posição longa aberta é fechada antes de abrir a nova posição curta.
4. O tamanho da posição é determinado pelo mesmo cálculo de ATR e percentual de risco.
5. Uma ordem de venda a mercado é enviada na abertura do candle. O stop-loss é colocado `StopLossAtrMultiplier × ATR` acima da entrada, enquanto o take-profit é `TakeProfitAtrMultiplier × ATR` abaixo da entrada.

### Gestão de saída

- **Ordens protetoras:** Os níveis de stop-loss e take-profit são rastreados internamente e avaliados em cada candle completado. Se qualquer limiar for ultrapassado, a posição é fechada a mercado.
- **Sinais opostos:** Quando uma configuração oposta é acionada, a posição atual é zerada antes de a nova ordem ser colocada.
- **Visualização:** O EA original podia desenhar linhas verticais para trades potenciais. As anotações de gráfico não são recriadas aqui; em vez disso, a estratégia desenha candles, a Bollinger Band e os próprios trades quando uma área de gráfico está disponível.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `RiskPercent` | `2` | Percentual do valor do portfólio arriscado por trade. |
| `TakeProfitAtrMultiplier` | `1.5` | Múltiplo do ATR usado para calcular a distância do take-profit. |
| `StopLossAtrMultiplier` | `1` | Múltiplo do ATR usado para calcular a distância do stop-loss. |
| `AtrPeriod` | `14` | Período de retrospectiva para o indicador ATR. |
| `BollingerPeriod` | `800` | Período da média móvel da Bollinger Band. |
| `BollingerDeviation` | `2` | Multiplicador de desvio padrão para a Bollinger Band. |
| `CandleType` | `1 hour` | Período (ou qualquer outro tipo de candle) usado para geração de sinais. |

## Notas

- Certifique-se de que o adaptador de portfólio fornece `Portfolio.CurrentValue`; caso contrário, o dimensionamento de posição baseado em risco retorna zero e a estratégia não irá operar.
- Se o símbolo não expõe um passo de preço ou valor de tick válido, os cálculos de pips e dinheiro por pip recorrem a padrões conservadores.
- O longo período de retrospectiva do Bollinger (800 barras) significa que o primeiro trade só pode ocorrer após receber dados históricos suficientes para aquecer tanto os indicadores Bollinger quanto ATR.
