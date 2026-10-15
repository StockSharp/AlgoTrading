# Estratégia T3MA(MTC)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertido do consultor especialista MetaTrader 4 **T3MA(MTC).mq4** (diretório `MQL/7904`). O robô original negocia sinais do indicador "T3MA-ALARM": ele constrói uma média móvel exponencial duplamente suavizada e coloca uma ordem sempre que a inclinação dessa curva muda de queda para subida ou vice-versa. A porta StockSharp espelha a mesma lógica com APIs idiomáticas de alto nível.

## Ideia de negociação

1. Construa um primeiro EMA usando o tipo e período de vela selecionados.
2. Suavize essa série com um segundo EMA do mesmo período.
3. Compare o valor suavizado com o anterior (opcionalmente deslocado por `MaShift`).
4. Quando a inclinação muda de direção, a estratégia registra um sinal. As ordens são executadas após o atraso `CalculationBarOffset` configurado, reproduzindo o parâmetro `CalculationBarIndex` do EA.
5. Cada sinal usa o mínimo da barra (para uma entrada longa) ou o máximo (para uma entrada curta) como um marcador exclusivo para evitar negociações duplicadas, assim como a variável `LastOrder` em MetaTrader.

## Detalhes de portabilidade

- Usa duas instâncias `ExponentialMovingAverage` para emular a cadeia de suavização T3MA-ALARM.
- Mantém uma pequena fila de valores suavizados recentes para dar suporte ao lookback `MaShift`.
- Os sinais são armazenados em uma fila FIFO e executados após o número solicitado de velas concluídas.
- As ordens de proteção são gerenciadas por meio de `StartProtection` com distâncias expressas em etapas de preço, correspondendo a MetaTrader pontos.
- O sinalizador `AllowMultiplePositions` reproduz a entrada `MultiPositions`: quando desabilitada, a estratégia espera até que a posição líquida seja plana antes de agir em um novo sinal.

## Parâmetros

- `MaPeriod` – EMA comprimento usado para ambas as passagens de suavização (padrão: 4).
- `MaShift` – número de barras para deslocar a série suavizada antes de comparar sua inclinação (padrão: 0).
- `CalculationBarOffset` – atraso (em velas finalizadas) entre a detecção de um sinal e o envio da ordem (padrão: 1).
- `TradeVolume` – volume base do pedido em lotes (padrão: 1).
- `UseStopLoss` / `StopLossPoints` – ativação e distância do stop loss em etapas de preço (padrão: ativado, 40 etapas).
- `UseTakeProfit` / `TakeProfitPoints` – ativação e distância do take-profit em etapas de preço (padrão: ativado, 11 etapas).
- `AllowMultiplePositions` – permite empilhar posições mesmo quando uma posição oposta está aberta (padrão: habilitado).
- `CandleType` – período de tempo ou tipo de dados usado para alimentar a cadeia do indicador (padrão: velas de 5 minutos).

## Fluxo de trabalho de negociação

1. Assine a série de velas escolhida e alimente os preços de fechamento por meio da cadeia dupla EMA.
2. Rastreie a direção atual da inclinação e gere um sinal quando ela virar.
3. Coloque cada sinal (ou a ausência de um) na fila de atraso para que as execuções aconteçam exatamente após `CalculationBarOffset` velas concluídas, assim como o script MQL4 lê buffers de indicadores mais antigos.
4. Quando um sinal amadurecido é executado:
   - Ignore se a negociação estiver desativada, a plataforma não estiver pronta ou `AllowMultiplePositions` estiver desativada enquanto uma posição líquida já estiver aberta.
   - Certifique-se de que o marcador de sinal seja diferente do anterior para evitar duplicatas.
   - Envie uma ordem de mercado (`BuyMarket`/`SellMarket`) com o volume configurado. Os batentes de proteção são anexados automaticamente quando ativados.

## Notas

- As comparações de preços usam uma pequena tolerância decimal para evitar artefatos de ponto flutuante ao verificar o análogo `LastOrder`.
- A estratégia não fecha automaticamente posições opostas quando `AllowMultiplePositions` está desativado, imitando o EA original que dependia de saídas de proteção.
- A visualização de velas e negociações próprias está disponível quando o subsistema de gráficos está presente.
