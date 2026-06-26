# Estratégia Gold Dust
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

A Estratégia Gold Dust reproduz o consultor especialista MetaTrader 5 "Gold Dust" dentro do framework StockSharp. Ela avalia até dois perceptrons construídos a partir de uma média móvel ponderada linear (LWMA) aplicada ao preço ponderado da vela. Cada perceptron observa como o preço desvia da média móvel em quatro pontos de retrocesso diferentes espaçados pelo período da MA. Quando a saída do perceptron é positiva, o especialista original abre uma posição de venda, e quando é negativa abre uma compra. O port StockSharp mantém o mesmo comportamento usando a API de assinatura de velas de alto nível.

## Geração de Sinais

1. Assinar o `CandleType` configurado e calcular um `WeightedMovingAverage` com o período de `MaPeriod`.
2. Em cada vela terminada, armazenar os preços de abertura e fechamento da vela junto com o valor da LWMA. A estratégia sempre mantém três períodos completos de MA de histórico para replicar as chamadas `CopyRates`/`CopyBuffer` da versão MQL.
3. Calcular os desvios preço/MA:
   - `a1` – fechamento atual menos LWMA atual
   - `a2` – preço de abertura um período de MA atrás menos LWMA na mesma vela
   - `a3` – preço de abertura dois períodos de MA atrás menos LWMA na mesma vela
   - `a4` – preço de abertura três períodos de MA atrás menos LWMA na mesma vela
4. Construir a saída do perceptron `result = Σ (wi × ai)` onde cada peso é o parâmetro bruto (por exemplo `X11`) menos 100, replicando a transformação original `w = x - 100`.
5. Interpretar as saídas do perceptron dependendo de `PassMode`:
   - `1` – usar apenas o primeiro perceptron.
   - `2` – usar apenas o segundo perceptron.
   - `3` – exigir que ambos os perceptrons produzam o mesmo sinal diferente de zero.
6. Um sinal negativo abre ou mantém uma posição longa, um sinal positivo abre ou mantém uma posição curta, e um sinal zero aciona a realização de lucros em posições existentes.

## Gestão de Posições

- **Entradas** – a estratégia opera com um `TradeVolume` fixo. Entrar comprado fecha qualquer exposição vendida pendente e vice-versa, mantendo apenas uma posição direcional.
- **Stop-loss** – `StopLossPips` é convertido em distância de preço absoluta usando `Security.PriceStep`. Para instrumentos cotados com três ou cinco decimais, a distância é multiplicada por dez para imitar a lógica do "ponto ajustado" na versão MQL. O stop é avaliado em cada vela concluída.
- **Trailing stop** – quando `TrailingStopPips` é maior que zero, a lógica de trailing é ativada. Após o preço se mover `TrailingStopPips + TrailingStepPips` a favor da operação, o stop é posicionado em `fechamento ± TrailingStopPips` (dependendo da direção).
- **Gestão de lucros** – quando nenhum perceptron concorda sobre uma direção (`signal == 0`), a estratégia fecha a posição apenas se o lucro flutuante for positivo.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `TradeVolume` | `1` | Volume base para cada nova entrada. Posições opostas são achatadas antes de tomar um novo lado. |
| `StopLossPips` | `150` | Distância inicial de stop-loss em pips ajustados (considera o multiplicador de 3/5 dígitos). Definir como zero para desabilitar o stop inicial. |
| `TrailingStopPips` | `25` | Distância de trailing stop em pips ajustados. Definir como zero para desabilitar o trailing. |
| `TrailingStepPips` | `5` | Movimento favorável adicional (em pips) necessário antes que o trailing stop avance. |
| `MaPeriod` | `20` | Comprimento do período da média móvel ponderada que alimenta os perceptrons. |
| `CandleType` | `H1` | Série de velas usada para avaliação de sinais. Qualquer outro período suportado pelo provedor de dados pode ser selecionado. |
| `PassMode` | `1` | Controla qual(is) perceptron(s) são avaliados: 1 – primeiro, 2 – segundo, 3 – consenso de ambos. |
| `X11`, `X21`, `X31`, `X41` | `100` | Pesos brutos para o perceptron #1. A estratégia subtrai 100 de cada valor antes de usar. |
| `X12`, `X22`, `X32`, `X42` | `100` | Pesos brutos para o perceptron #2, tratados da mesma forma que o primeiro conjunto. |

## Notas sobre a Conversão

- O EA original dependia de atualizações tick a tick para gerenciar stops; o port StockSharp avalia stops e trailing no fechamento da vela.
- Gestão monetária via `CMoneyFixedMargin` foi substituída por um parâmetro fixo `TradeVolume`.
- Cálculos do perceptron evitam buffers diretos de indicadores (`CopyBuffer`) armazenando em cache os valores necessários de velas e MA em listas limitadas.
- Todas as distâncias de pips respeitam a convenção do "ponto ajustado" do MetaTrader: se o instrumento opera com 3 ou 5 decimais, a distância é multiplicada por dez.

## Dicas de Uso

1. Criar ou selecionar um símbolo, depois definir `CandleType` para o período que corresponde ao gráfico histórico usado na versão MQL.
2. Revisar os pesos do perceptron (`X**`) e `PassMode` para corresponder à configuração otimizada do MetaTrader.
3. Ajustar `TradeVolume` para cumprir o tamanho mínimo e o passo do broker conectado.
4. Monitorar o log: cada vez que o trailing stop avança ou um stop-loss é acionado, uma mensagem é registrada.
