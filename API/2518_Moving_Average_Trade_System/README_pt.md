# Estratégia do Sistema de Trading de Médias Móveis (2518)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

Esta estratégia é um port do StockSharp do assessor especialista MetaTrader "Moving Average Trade System". Analisa a tendência usando quatro médias móveis simples (SMA) calculadas no preço mediano da vela. O sistema aguarda um cruzamento confirmado entre as médias de médio e longo prazo enquanto as médias mais rápidas confirmam o alinhamento da tendência. Uma vez que a confirmação chega, a estratégia inverte sua posição na direção da nova tendência e gerencia o risco com offsets fixos de take profit, stop-loss e trailing stop definidos em passos de preço.

## Lógica de Trading

1. **Indicadores**
   - `SMA(5)` (rápida) no preço mediano.
   - `SMA(20)` (média) no preço mediano.
   - `SMA(40)` (sinal) no preço mediano.
   - `SMA(60)` (lenta) no preço mediano.

2. **Entrada comprada**
   - `SMA(5) > SMA(20) > SMA(40)`.
   - `SMA(40)` está acima de `SMA(60)` por pelo menos `SlopeThresholdSteps` passos de preço.
   - `SMA(40)` cruzou acima de `SMA(60)` na barra atual (`SMA(40)` anterior estava abaixo ou igual à SMA lenta).
   - Se uma posição vendida estiver aberta, a estratégia compra volume suficiente para fechá-la e estabelecer o tamanho comprado desejado.

3. **Entrada vendida**
   - `SMA(5) < SMA(20) < SMA(40)`.
   - `SMA(40)` está abaixo de `SMA(60)` por pelo menos `SlopeThresholdSteps` passos de preço.
   - `SMA(40)` cruzou abaixo de `SMA(60)` na barra atual (`SMA(40)` anterior estava acima ou igual à SMA lenta).
   - Se uma posição comprada estiver aberta, a estratégia vende volume suficiente para fechá-la e estabelecer o tamanho vendido desejado.

4. **Gestão de risco** (avaliada apenas quando nenhuma nova entrada é acionada na barra):
   - **Saída por tendência:** fechar compradas quando `SMA(40) <= SMA(60)` e fechar vendidas quando `SMA(40) >= SMA(60)`.
   - **Take profit:** sair quando o preço atingir a distância de take profit configurada a partir do preço de entrada.
   - **Stop-loss:** sair se o preço se mover contra a posição pela distância de stop-loss configurada.
   - **Trailing stop:** uma vez que o preço avança além da entrada, seguir o stop de proteção por `TrailingStopSteps` passos de preço usando o máximo mais alto (para compradas) ou o mínimo mais baixo (para vendidas) desde a entrada.

Todos os offsets de stop e lucro são medidos em **passos de preço** (o `PriceStep` do instrumento). Se o instrumento não reportar um passo de preço, o valor `1` é usado como fallback.

## Parâmetros

| Nome | Descrição | Valor padrão | Otimizável |
| --- | --- | --- | --- |
| `Volume` | Volume da ordem usado ao abrir novas posições. | `1` | Não |
| `TakeProfitSteps` | Distância ao alvo de take profit medida em passos de preço. | `50` | Sim |
| `StopLossSteps` | Distância ao stop de proteção medida em passos de preço. | `50` | Sim |
| `TrailingStopSteps` | Offset do trailing stop em passos de preço (`0` desabilita o trailing). | `11` | Sim |
| `SlopeThresholdSteps` | Separação mínima entre `SMA(40)` e `SMA(60)` para validar um rompimento (em passos de preço). | `1` | Sim |
| `FastPeriod` | Comprimento da SMA rápida. | `5` | Sim |
| `MediumPeriod` | Comprimento da SMA média. | `20` | Sim |
| `SignalPeriod` | Comprimento da SMA de sinal (comparada com a SMA lenta). | `40` | Sim |
| `SlowPeriod` | Comprimento da SMA lenta que define a tendência de fundo. | `60` | Sim |
| `CandleType` | Série de velas usada para cálculos do indicador. | `Período de 1h` | Não |

## Notas de Implementação

- Os indicadores são vinculados à subscrição de velas através da API de alto nível `Bind`, garantindo que os cálculos sejam dirigidos por eventos e não dependam de acesso manual ao buffer.
- O preço mediano é usado para todos os cálculos de SMA, replicando o comportamento do EA original do MetaTrader.
- O gerenciamento de posições armazena o preço de preenchimento real usando `OnNewMyTrade` para recalcular os níveis de stop-loss, take profit e trailing stop após cada preenchimento.
- Ao inverter uma posição, a estratégia envia uma única ordem de mercado que fecha a exposição existente e abre a nova, imitando o comportamento compatível com hedge do algoritmo original.
- Todos os comentários dentro do arquivo fonte C# são escritos em inglês, conforme exigido pelas diretrizes do repositório.

## Dicas de Uso

- Configure o parâmetro `Volume` de acordo com o tamanho do lote do instrumento ou multiplicador de contrato.
- Ajuste as distâncias de stop e lucro para corresponder à volatilidade do instrumento (os padrões refletem as configurações do MetaTrader de 50 pips de stop/take profit e 11 pips de trailing stop em pares de forex).
- O parâmetro `SlopeThresholdSteps` pode ser definido como `0` para remover o filtro de espaçamento adicional e reagir a qualquer cruzamento de `SMA(40)`/`SMA(60)`.
- Para backtesting ou trading ao vivo, certifique-se de que o instrumento forneça um `PriceStep` válido; caso contrário, a estratégia tratará uma unidade de preço como um único passo.
