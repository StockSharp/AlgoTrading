# Estratégia Cruzada Zero CCIT3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia CCIT3 Zero Cross é uma porta StockSharp do consultor especialista MetaTrader 5 que negocia reversões de linha zero do oscilador CCIT3. O indicador é construído aplicando a cadeia de suavização Tillson T3 a um índice de canal de commodities (CCI). Sempre que o oscilador suavizado muda de sinal, a estratégia abre uma nova posição na direção da inversão ou, se configurada, fecha a posição atual e a inverte.

## Lógica de negociação
- Calcule o CCI usando o preço e período aplicados selecionados.
- Suavize o oscilador com um pipeline Tillson T3. Dois modos de cálculo são fornecidos:
  - **Simples** – suavização persistente de seis estágios que se comporta como o indicador de recálculo original MetaTrader.
  - **NoRecalc** – avalia o polinômio T3 apenas para a barra mais recente, recriando a versão leve “sem recálculo” do código-fonte.
- Quando o valor CCIT3 passar de positivo para negativo, abra uma posição longa (ou reverta uma posição curta se `Trade Overturn` estiver ativado).
- Quando o valor CCIT3 passar de negativo para positivo, abra uma posição curta (ou reverta uma longa se `Trade Overturn` estiver ativado).
- Os níveis opcionais de take-profit, stop-loss e trailing stop são gerenciados por meio do ajudante `StartProtection` de StockSharp.

## Indicadores e cálculos
- **Índice de canal de commodities (CCI)** – é executado no preço aplicado configurável (fechamento, abertura, máximo, mínimo, mediano, típico, ponderado) e período.
- **Suavização Tillson T3** – implementada exatamente como no indicador MQL5 com o fator de volume `B`. O modo Simples mantém cadeias EMA com estado entre barras, enquanto NoRecalc recalcula o polinômio a partir da última leitura bruta CCI.
- **Detecção de cruzamento zero** – as negociações são acionadas estritamente em velas finalizadas, refletindo as verificações originais da nova barra no consultor especialista.

## Gestão de riscos e posições
- `Take Profit (pts)` e `Stop Loss (pts)` são convertidos em distâncias de preços absolutos usando o `PriceStep` do instrumento.
- `Trailing Stop (pts)` ativa o mecanismo de rastreamento de StockSharp com a mesma distância do ponto.
- `Max Drawdown Target` redimensiona o volume do pedido base usando o valor do portfólio atual ou inicial (`volume = OrderVolume * balance / target`). Deixe o parâmetro em zero para manter um tamanho de lote fixo.
- `Trade Overturn` permite a reversão completa – a posição atual é fechada primeiro e depois uma nova é aberta na direção oposta.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `Volume` | 1 | Volume base do pedido antes de qualquer escala de rebaixamento. |
| `Take Profit (pts)` | 1750 | Distância de lucro em pontos. |
| `Stop Loss (pts)` | 0 | Distância de stop-loss em pontos. |
| `Trailing Stop (pts)` | 0 | Distância de parada final em pontos (0 desativa o rastreamento). |
| `Trade Overturn` | falso | Inverta a posição nos sinais CCIT3 opostos. |
| `CCI Period` | 285 | Período de lookback para o indicador CCI. |
| `CCI Price` | Típico | Preço aplicado usado para alimentar o CCI. |
| `T3 Period` | 60 | Comprimento de suavização Tillson T3. |
| `T3 Volume Factor` | 0,618 | Coeficiente de Tillson T3 `B`. |
| `Mode` | Simples | Modo de cálculo CCIT3 (`Simple` ou `NoRecalc`). |
| `Candle Type` | Período de 1 hora | Prazo usado para assinaturas de velas. |
| `Max Drawdown Target` | 0 | Divisor de equilíbrio para dimensionamento de volume adaptável (0 desativa o dimensionamento). |

## Notas de implementação
- A estratégia assina uma única fonte de velas especificada por `Candle Type` e processa apenas velas concluídas.
- Todos os valores de volume estão alinhados à etapa de volume do título e limitados por `VolumeMin`/`VolumeMax`.
- Os parâmetros padrão replicam a configuração MT5 publicada: modo simples CCIT3 com um período CCI de 285 períodos, comprimento T3 60 e fator de volume 0,618.
- Mudar para NoRecalc mantém o comportamento do indicador original de reagir instantaneamente ao sinal bruto CCI enquanto ainda produz sinais positivos/negativos.
