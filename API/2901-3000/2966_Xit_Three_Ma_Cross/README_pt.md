# Estratégia Xit de Cruzamento de Três MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma recriação em StockSharp do assessor especializado MetaTrader 5 **XIT_THREE_MA_CROSS.mq5**. Ela alinha três médias móveis, verifica a separação de momentum do MACD e dimensiona posições a partir de limites de risco baseados em ATR. O método é seguidor de tendência com confirmação de momentum e visa oscilações de médio prazo em pares de moedas líquidos ou índices.

## Visão geral

- **Regime de mercado**: Funciona melhor em instrumentos que têm tendência por múltiplas velas no período selecionado.
- **Indicadores**:
  - Médias móviles lenta, intermediária e rápida (tipo selecionável pelo usuário) avaliadas no período de trading.
  - MACD (baseado em EMA) para direção de momentum e distância entre a linha MACD e a sinal.
  - Dois cálculos de ATR (mesma comprimento, períodos independentes) usados para projetar distâncias de stop-loss e take-profit.
- **Direção da ordem**: Bidirecional. O motor pode abrir tanto operações compradas quanto vendidas.
- **Dimensionamento de posição**: Calculado a partir do percentual de risco configurado e a distância de stop baseada em ATR. Quando os metadados do instrumento estão incompletos, a estratégia recorre à propriedade `Volume` padrão.

## Lógica de trading

### Entrada comprada

Uma posição comprada é aberta quando todas as condições abaixo são verdadeiras em uma vela terminada:

1. A linha MACD aumenta em comparação com a barra anterior (`MACD[t] > MACD[t-1]`).
2. A linha de sinal MACD aumenta em comparação com a barra anterior.
3. A linha MACD excede a linha de sinal em pelo menos `MacdTriggerPoints * PriceStep`.
4. A média móvel intermediária sobe vs o valor anterior.
5. A média móvel rápida sobe vs o valor anterior.
6. A MA intermediária está acima da MA lenta.
7. A MA rápida está acima da MA intermediária.
8. Ambos os valores de ATR estão disponíveis para definir distâncias de stop e alvo.

### Entrada vendida

As regras do lado vendido espelham a configuração comprada com comparações invertidas:

1. A linha MACD diminui em comparação com a barra anterior.
2. A linha de sinal MACD diminui em comparação com a barra anterior.
3. A linha de sinal é maior que a linha MACD em pelo menos `MacdTriggerPoints * PriceStep`.
4. A MA intermediária cai em comparação com a vela anterior.
5. A MA rápida cai em comparação com a vela anterior.
6. A MA intermediária está abaixo da MA lenta.
7. A MA rápida está abaixo da MA intermediária.
8. Ambas as séries ATR entregaram um valor terminado.

### Lógica de saída

- **Posições compradas** fecham quando a MA rápida cai abaixo da MA intermediária, ou o preço atinge os níveis de stop/take-profit baseados em ATR.
- **Posições vendidas** fecham quando a MA rápida cruza acima da MA intermediária, ou os limites ATR são tocados.
- Após fechar uma posição, o algoritmo aguarda a próxima vela antes de avaliar novas entradas, seguindo o comportamento do EA original.

## Gestão de risco

- **Stop Loss**: A distância é igual ao último valor ATR de `AtrStopCandleType`. Para comprados, o preço de stop é `Entry - ATR`, para vendidos é `Entry + ATR`.
- **Take Profit**: A distância é igual ao valor ATR de `AtrTakeCandleType`. Os alvos são espelhados em relação ao preço de entrada.
- **Percentual de risco**: A estratégia estima a perda monetária por unidade a partir da distância de stop. Se `PriceStep` e `PriceStepCost` são conhecidos, o risco por contrato usa avaliação de tick. Caso contrário, a distância de preço bruta é usada. Tamanho da posição é `RiskPercent%` do valor atual do portfólio dividido pelo risco por unidade, arredondado para baixo ao `VolumeStep` mais próximo.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Período principal para cálculos de médias móveis e MACD. | Velas de 1 hora |
| `SlowMaLength` / `IntermediateMaLength` / `FastMaLength` | Períodos das médias móveis. | 60 / 14 / 4 |
| `SlowMaType`, `IntermediateMaType`, `FastMaType` | Famílias de médias móveis (Simples, Exponencial, Suavizado, Ponderado). | Simples |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | Comprimentos de EMA rápida, lenta e sinal MACD. | 12 / 26 / 9 |
| `MacdTriggerPoints` | Distância mínima entre MACD e seu sinal, medida em pontos do instrumento. Convertida usando `PriceStep`. | 7 |
| `AtrLength` | Período para ambos os indicadores ATR. | 14 |
| `AtrTakeCandleType` / `AtrStopCandleType` | Períodos para séries ATR de take-profit e stop-loss. | Velas de 4 horas |
| `RiskPercent` | Percentual do valor atual do portfólio arriscado em cada operação. | 10% |

## Notas de uso

1. Anexe a estratégia a um valor com `PriceStep`, `PriceStepCost` e `VolumeStep` precisos para obter dimensionamento de posição preciso.
2. Certifique-se de que os dados históricos estejam disponíveis para cada período subscrito (`CandleType`, `AtrTakeCandleType`, `AtrStopCandleType`). Valores ATR ausentes adiarão entradas.
3. O algoritmo opera em velas completamente fechadas e ignora flutuações intrabar, refletindo a lógica MetaTrader original de buscar buffers de indicadores atuais e anteriores.
4. Modifique os tipos de média móvel se o mercado alvo favorecer filtros mais suaves ou mais rápidos.

## Arquivos

- `CS/XitThreeMaCrossStrategy.cs` – Implementação C# com API de alto nível StockSharp, incluindo subscrições ATR e dimensionamento de risco.
- `README_ru.md` – Descrição em russo da estratégia.
- `README_zh.md` – Tradução para o chinês da documentação.
