# Estratégia de BADX ADX Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

Esta estratégia reproduz o consultor especializado BADX do MetaTrader usando a API de alto nível do StockSharp. Combina o **Average Directional Index (ADX)** com **Bollinger Bands** para negociar condições de range: quando o ADX cai abaixo de um limiar configurável e o preço toca a banda exterior, a estratégia desfaz o movimento esperando uma reversão à média. Todas as ordens de proteção, incluindo stop-loss, take-profit e trailing stop opcional, são gerenciadas automaticamente pelo `StartProtection`.

## Como Funciona

1. Assina a série de candles configurada e alimenta um indicador `AverageDirectionalIndex` e `BollingerBands` através de bindings de alto nível.
2. Para cada candle finalizado o callback recebe o valor ADX bem como os envelopes superior e inferior de Bollinger.
3. Se o ADX estiver abaixo de `AdxLevel`, o mercado é considerado sem tendência:
   - Quando o preço de fechamento está abaixo da banda inferior e não há posição aberta, a estratégia compra a mercado.
   - Quando o preço de fechamento está acima da banda superior e não há posição aberta, a estratégia vende a mercado.
4. O gerenciamento de risco converte distâncias em pips em offsets de preço absoluto. Stop-loss, take-profit e parâmetros de trailing (se habilitados) são aplicados imediatamente após as entradas pelo gerenciador de proteção.
5. Apenas uma posição pode estar ativa de cada vez. As saídas ocorrem através de ordens de proteção ou ajustes de trailing stop.

## Parâmetros

- **CandleType** (`DataType`): Período usado para os cálculos do indicador. Padrão: candles de 15 minutos.
- **AdxPeriod** (`int`): Período de médias para o indicador ADX. Padrão: 30.
- **AdxLevel** (`decimal`): Valor ADX máximo que ainda qualifica como mercado em range. Padrão: 20.
- **BollingerPeriod** (`int`): Período para a média móvel das Bollinger Bands. Padrão: 10.
- **BollingerDeviation** (`decimal`): Multiplicador de desvio padrão para as Bollinger Bands. Padrão: 1.5.
- **StopLossPips** (`decimal`): Distância de stop-loss medida em pips. Padrão: 50.
- **TakeProfitPips** (`decimal`): Distância de take-profit medida em pips. Padrão: 50.
- **TrailingStopPips** (`decimal`): Distância do trailing stop em pips. Padrão: 5.
- **TrailingStepPips** (`decimal`): Melhoria mínima de preço em pips antes de o trailing stop ser ajustado. Padrão: 5.

## Uso

1. Anexar a estratégia a um instrumento e configurar os parâmetros desejados.
2. Iniciar a estratégia. Ela assinará automaticamente o stream de candles necessário, construirá os indicadores e configurará as ordens de proteção.
3. Monitorar os trades na área do gráfico: candles, as Bollinger Bands e as ordens executadas são visualizadas quando a plataforma suporta gráficos.
4. Ajustar os parâmetros de risco (stop-loss, take-profit, distâncias de trailing) para se adequar à volatilidade do instrumento ou preferências pessoais.

## Notas

- Apenas candles terminados são processados para evitar entradas prematuras.
- O tamanho do pip é derivado do `PriceStep` do instrumento; quando o instrumento usa 3 ou 5 dígitos decimais, o pip é ajustado por um fator de dez, imitando o consultor especializado original.
- A estratégia mantém `Volume` em `1` por padrão. Modificar a propriedade `Volume` da classe base para corresponder ao tamanho de trade preferido.
- Todos os comentários inline no código-fonte são escritos em inglês de acordo com as diretrizes do repositório.
