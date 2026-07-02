# Estratégia EMA WMA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
EMA WMA RSI é uma conversão do MetaTrader 4 consultor especialista "EMA WMA RSI" criado por cmillion. O robô original compara uma média móvel exponencial (EMA) e uma média móvel ponderada linear (WMA) calculada a partir de aberturas de velas e filtra cada cruzamento com um limite de Índice de Força Relativa (RSI). A porta StockSharp mantém a mesma lógica do indicador, opera em velas finalizadas e reproduz as opções de gerenciamento de dinheiro: achatamento de contraposição opcional, níveis de stop-loss/take-profit baseados em pontos e um trailing stop que pode seguir distâncias fixas, o fractal mais recente ou extremos recentes de velas.

A estratégia é projetada para um único símbolo e período selecionado por meio do parâmetro `Candle Type`. Ele assume MetaTrader "pontos" (o tick mínimo) ao converter distâncias de risco em preços absolutos, portanto, metadados de instrumentos como `Security.Step` e `Security.StepPrice` devem ser preenchidos para obter melhores resultados.

## Lógica estratégica
### Indicadores
* **EMA** – período definido por `EMA Period`, aplicado aos preços de abertura das velas.
* **WMA** – período definido por `WMA Period`, também alimentado com aberturas de velas.
* **RSI** – `RSI Period`, calculado no mesmo fluxo de preço de abertura.

Todos os indicadores são atualizados uma vez por vela finalizada. A porta espelha a execução original de "barra aberta", armazenando os valores EMA/WMA da barra anterior e comparando-os com a barra atual imediatamente após ela fechar.

### Regras de entrada
* **Configuração longa**
  1. O valor atual de EMA está abaixo do WMA, enquanto a barra anterior estava EMA acima do WMA (uma cruz descendente).
  2. O valor RSI está acima de 50.
  3. Se existir uma posição curta, ela será opcionalmente fechada quando `Close Counter Trades` estiver ativado; caso contrário, o sinal será ignorado até que a estratégia seja plana.
  4. Quando as condições são mantidas, uma ordem de compra a mercado é enviada usando o volume fixo ou o dimensionamento baseado em risco.
* **Configuração curta** – lógica simétrica: EMA cruza acima do WMA, a barra anterior mostrou EMA abaixo do WMA, RSI está abaixo de 50 e a estratégia nivela uma compra ou pula a negociação.

### Regras de saída
* **Proteção inicial** – `Stop Loss (points)` e `Take Profit (points)` são convertidos em distâncias absolutas usando o tamanho do tick do instrumento. Qualquer valor pode ser definido como zero para desativá-lo.
* **Parada final**
  * Se `Trailing Stop (points)` for maior que zero, o stop segue o preço a uma distância fixa medida a partir do último fechamento (apenas apertando, nunca afrouxando).
  * Se a distância final for zero, o algoritmo procura níveis adaptativos:
    * `Trailing Source = CandleExtremes` analisa os máximos/mínimos das velas anteriores. Um stop longo move-se para o primeiro mínimo, pelo menos cinco pontos abaixo do preço atual; uma parada curta usa máximas cinco pontos acima.
    * `Trailing Source = Fractals` verifica fractais previamente confirmados de Bill Williams (duas velas de cada lado). O mesmo buffer de cinco pontos se aplica para evitar colocar o stop muito próximo do preço atual.
  * Os ajustes finais só são ativados depois que o preço ultrapassa o preço de entrada original, reproduzindo o comportamento MetaTrader EA.
* **Saída da posição** – Quando o trailing stop ou take-profit é tocado dentro do intervalo de uma vela, a posição é fechada com uma ordem de mercado e o estado interno é redefinido.

### Dimensionamento de posição
* `Fixed Volume` fornece o tamanho exato da ordem de mercado (lotes/contratos). Este é o padrão, correspondendo ao parâmetro EA `Lot`.
* Definir `Fixed Volume` como zero ativa o dimensionamento baseado em risco. A estratégia estima o risco monetário por unidade usando a distância de stop disponível (seja o stop loss configurado ou a distância de trilha efetiva) e `Security.StepPrice`. `Risk %` determina quanto patrimônio do portfólio é exposto por negociação. Se o volume fixo e a porcentagem de risco forem zero, o sinal será ignorado.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `EMA Period` | Período da média móvel exponencial aplicada às aberturas das velas. | `28` |
| `WMA Period` | Período da média móvel ponderada linear nas aberturas. | `8` |
| `RSI Period` | Comprimento RSI usado como filtro direcional. | `14` |
| `Stop Loss (points)` | Compensação de stop loss em MetaTrader pontos. `0` desativa a parada protetora. | `0` |
| `Take Profit (points)` | Compensação de lucro em pontos. `0` desativa o alvo. | `500` |
| `Trailing Stop (points)` | Distância de fuga fixa em pontos. `0` muda para rastreamento adaptativo (fractais ou mínimos/máximos de velas). | `70` |
| `Trailing Source` | Método de trilha adaptativo: `CandleExtremes` para altos/baixos brutos, `Fractals` para Williams fractais. | `CandleExtremes` |
| `Close Counter Trades` | Feche uma posição oposta antes de abrir uma nova negociação. | `false` |
| `Fixed Volume` | Volume de ordens de mercado. Defina como `0` para ativar o dimensionamento baseado em risco. | `0.1` |
| `Risk %` | Porcentagem do patrimônio do portfólio comprometido quando `Fixed Volume` é zero. Requer uma distância de parada válida. | `10` |
| `Candle Type` | Prazo principal usado para indicadores e avaliação de sinais. | `30-minute candles` |

## Notas de implementação
* As conversões por etapas de preço dependem de `Security.Step` (ou `Security.PriceStep`) e `Security.StepPrice`. Forneça metadados realistas de instrumentos para manter a precisão dos cálculos ponto-preço.
* A estratégia processa apenas velas finalizadas e usa seus preços de abertura para atualizações de indicadores, correspondendo à lógica da "nova barra" no código MQL4.
* Os níveis finais mantêm pelo menos um buffer de cinco pontos longe do preço atual, assim como a função auxiliar original `SlLastBar`.
* Quando o fechamento da contraposição está desabilitado, a estratégia nunca faz hedge – apenas uma única posição líquida é gerenciada por vez.
* Nenhuma implementação Python está incluída neste pacote.
