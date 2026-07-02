# Estratégia de duplicação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Double Up é uma porta direta do MetaTrader consultor especialista `DoubleUp.mq4`. Ele combina um oscilador de índice de canal de commodities (CCI) com a linha principal do indicador MACD para detectar condições extremas de momento e, em seguida, aplica um modelo de dimensionamento de posição estilo martingale. Sempre que ambos os osciladores atingem a mesma zona extrema, o algoritmo se prepara para uma negociação na direção oposta. Assim que CCI retorna ao ponto médio, a estratégia abre uma nova posição longa (após fechar as posições curtas existentes) ou abre uma nova posição curta (após fechar as posições longas existentes).

O volume de cada nova posição é baseado em uma progressão exponencial (`baseVolume * 2^lossCounter`). Saídas perdedoras consecutivas aumentam o expoente, enquanto uma saída lucrativa redefine a progressão de acordo com o buffer de espera acumulado. Este comportamento reflete a lógica de pirâmide no código original, onde as variáveis ​​`pos` e `wait` controlam o multiplicador aplicado.

## Lógica de negociação
- Assine uma única série de velas e calcule a linha principal CCI (comprimento padrão 8) e MACD (padrão rápido 13, lento 33, sinal 2).
- Multiplique a leitura de MACD por um milhão para que sua magnitude corresponda ao nível limite de CCI.
- Quando ambos os osciladores excederem `+Threshold`, prepare a estratégia para uma futura entrada vendida. Quando ambos os osciladores caírem abaixo de `-Threshold`, prepare-os para uma entrada longa futura.
- Uma entrada longa pendente é executada assim que o valor CCI volta abaixo de `+Threshold`. Uma entrada curta pendente é executada quando CCI fica abaixo de `-Threshold` enquanto o sinalizador curto está ativo, reproduzindo a ordem exata do código original.
- Antes de abrir uma nova posição, o algoritmo fecha totalmente qualquer exposição oposta. A nova ordem é despachada somente após o fechamento de todas as negociações.
- As negociações de saída são ordens de mercado geradas durante reversões de sinal. O lucro ou perda realizado de cada negociação fechada alimenta os contadores martingale.

## Modelo de dimensionamento de posição
- As saídas perdidas aumentam o contador de perdas. Depois que o contador atinge `PreWait`, seu valor é adicionado ao buffer de espera e o contador de perdas é zerado.
- Uma saída lucrativa transfere o valor do buffer de espera (truncado) para o contador de perdas e limpa o buffer. Os tamanhos futuros de negociação, portanto, começam em `2^lossCounter` lotes.
- O buffer de espera é inicializado em `InitialWait` e é controlado pelas regras acima.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `CciPeriod` | 8 | Período do Commodity Channel Index. |
| `Threshold` | 230 | Nível absoluto usado para detectar extremos do oscilador. |
| `MacdFastPeriod` | 13 | Comprimento EMA rápido do cálculo MACD. |
| `MacdSlowPeriod` | 33 | Comprimento EMA lento do cálculo MACD. |
| `MacdSignalPeriod` | 2 | Comprimento do sinal EMA, necessário para corresponder à assinatura da chamada MetaTrader. |
| `BaseVolume` | 0,01 | Iniciando o multiplicador de volume antes de aplicar o expoente martingale. |
| `InitialWait` | 0 | Valor inicial do buffer de espera (variável `wait` no script original). |
| `PreWait` | 2 | Número mínimo de saídas perdedoras consecutivas necessárias antes que o buffer de espera acumule o contador de perdas. |
| `BackShift` | 0 | Mudança histórica para leituras de indicadores. Apenas zero é suportado nesta porta. |
| `CandleType` | Período de 15 minutos | Tipo de vela solicitado no feed de dados. Ajuste para corresponder ao período do gráfico usado em MetaTrader. |

## Notas
- A porta atualmente suporta apenas `BackShift = 0`, espelhando a configuração padrão do consultor especialista original.
- Cada envio e fechamento de ordens utiliza ordens de mercado. Anexe proteções separadas (stop-loss, take-profit), se necessário.
- Como a estratégia duplica o tamanho da posição após perdas nas negociações, certifique-se de que os limites de margem e os controles de risco sejam apropriados para o instrumento negociado.
