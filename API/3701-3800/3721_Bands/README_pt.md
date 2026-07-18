# Estratégia de Bandas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia transporta o consultor especialista MetaTrader 5 **Bands.mq5** para o StockSharp API de alto nível. Espera por uma vela acabada
que perfura as bandas Bollinger de fora para dentro do canal e só abre uma posição quando o Donchian canal conf
afirma que a inclinação da banda tem sido estável para um número configurável de barras. Os múltiplos do intervalo verdadeiro médio (ATR) reproduzem o ori
distâncias iniciais de stop-loss e take-profit, enquanto um rastreador de regressão opcional imprime o coeficiente de determinação da curva de patrimônio
(R-quadrado) a cada 100 negociações, espelhando a saída de diagnóstico da versão MQL.

## Lógica de negociação
1. Assine um único fluxo de vela e calcule Bollinger bandas, um canal Donchian e ATR com os mesmos períodos do MetaT
robô Rader.
2. Quando nenhuma posição estiver aberta, inspecione a vela concluída **anteriormente**:
   - Digite longo se aquela vela abriu abaixo da banda inferior Bollinger e fechou acima dela, e a banda inferior Donchian não caiu
definido para mais de `ConfirmationPeriod` barras.
   - Digite short se a vela abriu acima da banda superior Bollinger e fechou abaixo dela, e a banda superior Donchian não subiu
en por mais de `ConfirmationPeriod` barras.
3. Quando existe uma posição, saia se o limite final Donchian for cruzado (usando o fechamento anterior) ou se o limite ATR-base
d os níveis de proteção intrabar são violados.
4. Cada negociação executada armazena o patrimônio atual do portfólio e imprime a métrica R-quadrado de regressão linear após cada bloco de
100 negociações. Uma inclinação negativa produz um R-quadrado negativo, assim como o consultor especialista original.

## Gestão de risco
- As ordens de entrada são sempre enviadas no mercado com o `TradeVolume` definido pelo usuário.
- Os níveis de proteção são recriados no código (em vez de usar ordens pendentes) comparando os máximos e mínimos das velas com o ATR mu
bebidas.
- Quando o stop-loss ou o take-profit são acionados, a estratégia fecha toda a posição com uma ordem de mercado e redefine a proteção.
em níveis.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TradeVolume` | Volume líquido (em lotes) para cada ordem de mercado. |
| `CandleType` | Tipo de dados/período de vela usado para todos os indicadores. |
| `BollingerPeriod` | Número de velas usadas pelas bandas Bollinger. |
| `BollingerDeviation` | Multiplicador de desvio padrão aplicado às bandas Bollinger. |
| `DonchianPeriod` | Comprimento do canal Donchian usado como filtro de tendência. |
| `ConfirmationPeriod` | Contagem mínima de barras consecutivas que devem manter a inclinação Donchian não decrescente (longa) ou não crescente (curta). |
| `AtrPeriod` | Período do Average True Range utilizado para gerenciamento de riscos. |
| `StopAtrMultiplier` | ATR múltiplo que define a distância do stop-loss. |
| `TakeAtrMultiplier` | ATR múltiplo que define a distância de take-profit. |

## Notas
- A verificação de inclinação Donchian é implementada como um contador contínuo em vez de copiar buffers de indicador, o que mantém o StockSharp
versão eficiente ao mesmo tempo que corresponde ao comportamento do EA original.
- Todos os comentários e diagnósticos são fornecidos em inglês, conforme exigido pelas diretrizes do projeto.
- Os auxiliares de gerenciamento de dinheiro do código MetaTrader não são reproduzidos; a implementação de StockSharp depende de `TradeVolume`
parâmetro para dimensionamento de posição.
