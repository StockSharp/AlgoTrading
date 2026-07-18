# Estratégia GoldWarrior02b
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Porta StockSharp abrangente do consultor especialista MetaTrader 4 *GoldWarrior02b* (pasta `MQL/7694`).
Ele combina um índice de canal de commodities (CCI), um medidor de impulso personalizado e um detector de balanço ZigZag feito à mão
e avalia os sinais apenas alguns segundos antes de cada limite de 15 minutos. O objetivo desta tradução é
para imitar a lógica de alto nível do robô original, respeitando o modelo de execução de posição líquida de StockSharp.

## Características principais

- **Filtro de impulso** – substitui o indicador personalizado `DayImpuls` calculando a média da distância de abertura/fechamento da vela
normalizado pela etapa de preço do instrumento.
- **Estrutura ZigZag** – reconstrói máximos e mínimos recentes para determinar se o mercado está com tendência de alta ou de baixa.
- **Gate de tempo** – as entradas são permitidas somente quando a vela atual fechar durante os últimos 15 segundos dos minutos 14, 29, 44 ou 59.
- **Controles de risco** – inclui stop-loss, take-profit, trailing stop (opcional) e uma meta de lucro medida para toda a conta
em unidades monetárias. Os padrões refletem as entradas MetaTrader (stop de 1.000 pontos, take-profit de 150 pontos, trailing desativado).
- **Exposição líquida** – StockSharp mantém uma única posição líquida por título, portanto, o hedge multinível e o escalonamento de lote
da implementação MQL não são reproduzidos. Em vez disso, a estratégia concentra-se num único volume de entrada.

## Lógica de negociação

### Preparação de Sinal

1. Assine velas definidas por `CandleType` (período de 5 minutos por padrão).
2. Calcule CCI e a média do impulso usando o `ImpulsePeriod` compartilhado (padrão 21 barras).
3. Atualize a direção do balanço do ZigZag quando o desvio exceder `ZigZagDeviation` pontos e a profundidade/recuo
restrições são atendidas.
4. Armazene os valores anteriores dos indicadores para replicar o "atual" (`cci0`, `imp`) e o "anterior" (`cci1`, `nimp`)
buffers usados no consultor especialista.

### Regras de entrada

Uma configuração é avaliada somente se nenhuma posição estiver aberta no momento, pelo menos 15 segundos se passaram desde a última saída e
`AllowEntryTime` retorna `true` (fim do bloco de 15 minutos).

**Longo:**
- A última oscilação do ZigZag aponta para baixo (novo mínimo inferior ao anterior).
- Ou
  - o CCI atual aumenta em comparação com a barra anterior, o CCI anterior está abaixo de -50, o CCI atual permanece abaixo de -30,
o impulso torna-se positivo e o impulso anterior é negativo; ou
  - o CCI atual está abaixo de -200, o CCI anterior ainda era menor, o impulso permanece abaixo de `ImpulseBuyThreshold`
e é mais forte que o impulso anterior.

**Curto:**
- A última oscilação do ZigZag aponta para cima (nova máxima superior à anterior).
- Ou
  - o CCI atual diminui em comparação com a barra anterior, o CCI anterior está acima de 50, o CCI atual permanece acima de 30,
o impulso torna-se negativo e o impulso anterior foi positivo; ou
  - atual CCI está acima de 200, o anterior CCI era maior, o impulso permanece acima de `ImpulseSellThreshold`
e é mais fraco que o impulso anterior.

Se o valor do impulso anterior estiver entre `ImpulseSellThreshold` e `ImpulseBuyThreshold` o sinal será ignorado.

### Gerenciamento de saída

- **Stop-loss** – é acionado quando o preço se move `StopLossPoints` além do preço de entrada (1.000 pontos por padrão).
- **Take-profit** – fecha a posição após viajar `TakeProfitPoints` (150 pontos).
- **Parada móvel** – opcional; quando ativado, ele é ativado após movimentos de preço `TrailingStopPoints + TrailingStepPoints`
a favor da posição e depois segue o preço em `TrailingStopPoints`.
- **Meta de lucro** – converte o lucro líquido aberto em moeda da conta usando `PriceStep` e `StepPrice` e
fecha a posição quando excede `ProfitTarget` (padrão 300).

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `BaseVolume` | Tamanho comercial para entradas. | `0.1` |
| `StopLossPoints` | Pare a distância em pontos. | `1000` |
| `TakeProfitPoints` | Distância de lucro em pontos. | `150` |
| `TrailingStopPoints` | Distância de parada final em pontos (0 desativa o rastreamento). | `0` |
| `TrailingStepPoints` | Distância adicional antes do rastreamento ser ativado. | `0` |
| `ImpulsePeriod` | Período para cálculos de CCI e de impulso. | `21` |
| `ZigZagDepth` | Barras mínimas entre novas oscilações do ZigZag. | `12` |
| `ZigZagDeviation` | Movimento do preço mínimo (em pontos) para confirmar uma oscilação. | `5` |
| `ZigZagBackstep` | Barras mínimas antes de aceitar um novo swing. | `3` |
| `ProfitTarget` | Limite de lucro não realizado (moeda da conta). | `300` |
| `ImpulseSellThreshold` | Valor mínimo de impulso necessário para shorts. | `-30` |
| `ImpulseBuyThreshold` | Valor máximo de impulso permitido para posições compradas. | `30` |
| `CandleType` | Prazo de trabalho. | `5 minute time frame` |

## Diferenças vs. Expert Advisor Original

- A versão MetaTrader usa `GlobalVariableSet` para limitar a taxa de pedidos e armazena contagens de tickets para grades de hedge.
Esta porta mantém o acelerador baseado no tempo, mas não a escada de média/cobertura porque StockSharp contas
são compensados.
- O gerenciamento de pedidos é feito por meio de ordens de mercado (`BuyMarket`, `SellMarket`) para permanecer dentro da orientação de alto nível API.
- O cálculo do impulso é simplificado; o `DayImpuls` original expõe dois buffers (`imp`, `nimp`). Aqui ambos os buffers
são aproximados pelas leituras de média móvel atuais e anteriores.

## Dicas de uso

- Configure `CandleType` para corresponder ao período usado durante a otimização (o EA original funciona em M5).
- Certifique-se de que o instrumento forneça metadados `PriceStep` e `StepPrice` para converter distâncias de pontos corretamente.
- Back-test com deslizamento/latência realista para confirmar se o portão de entrada (últimos segundos antes do quarto de hora) se comporta conforme o esperado.

## Isenção de responsabilidade

Esta estratégia é fornecida para fins educacionais. Teste exaustivamente com dados históricos e futuros antes
arriscando capital real.
