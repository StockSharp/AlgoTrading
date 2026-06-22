# Estratégia de Rompimento Fractal RSI Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o expert advisor MetaTrader "RSI and Bollinger Bands" no StockSharp. Aplica Bandas de Bollinger ao oscilador RSI, aguarda um nível de rompimento fractal recente e coloca ordens stop além desse nível com deslocamentos configuráveis. Um filtro de trailing Parabolic SAR ajusta dinamicamente os stops assim que uma posição é aberta.

## Indicadores e sinais
- **RSI** (padrão 8 períodos) – o oscilador principal. Os limiares de sobrecompra e sobrevenda são usados para cancelar ordens pendentes.
- **Bandas de Bollinger sobre RSI** (padrão 14 períodos, desvio 1.0) – entradas só são acionadas quando o RSI fecha fora da banda superior ou inferior, correspondendo ao comportamento do script original onde Bollinger é alimentado pelos valores do RSI.
- **Fractais de Bill Williams** – a estratégia escaneia os últimos fractais confirmados de alta e baixa (padrão de 5 barras) e usa seus preços como níveis base de rompimento.
- **Parabolic SAR** (passo 0.003, máximo 0.2) – fornece uma referência de trailing stop assim que uma posição está ativa.

## Lógica de entrada
1. O trabalho é realizado em candles finalizados do período selecionado (padrão 4 horas).
2. Quando aparece um **fractal de alta** e o RSI fecha acima da **banda Bollinger superior**, enquanto o fechamento anterior permanece abaixo do fractal, é colocada uma **buy stop**:
   - Preço de entrada = máxima do fractal + indent (15 pips por padrão).
   - Stop loss opcional = entrada − StopLossPips.
   - Take profit opcional = entrada + TakeProfitPips.
3. Simetricamente, quando um **fractal de baixa** se forma e o RSI fecha abaixo da **banda Bollinger inferior**, enquanto o fechamento anterior permanece acima do fractal, é colocada uma **sell stop** abaixo do fractal.
4. O RSI revertendo para dentro do canal cancela as ordens pendentes:
   - RSI < limiar inferior cancela buy stops.
   - RSI > limiar superior cancela sell stops.

## Saída e gestão de risco
- As distâncias fixas de stop loss e take profit (em pips) replicam os inputs do MQL. Definir qualquer distância como `0` desabilita essa proteção.
- A lógica de trailing do Parabolic SAR exige que o SAR esteja pelo menos `SarTrailingPips` afastado do preço atual e só move o stop na direção favorável.
- Quando o trailing stop cruza o preço ou o preço atinge o take profit fixo, a posição é fechada com uma ordem a mercado.
- Abrir uma posição automaticamente elimina a ordem pendente contrária e armazena os níveis de proteção pretendidos.

## Parâmetros
| Parâmetro | Descrição | Padrão |
| --- | --- | --- |
| `RsiPeriod` | Comprimento de suavização do RSI. | 8 |
| `BandsPeriod` | Período Bollinger sobre RSI. | 14 |
| `BandsDeviation` | Multiplicador de desvio padrão para Bollinger sobre RSI. | 1.0 |
| `SarStep` | Passo de aceleração do Parabolic SAR. | 0.003 |
| `SarMax` | Aceleração máxima do Parabolic SAR. | 0.2 |
| `TakeProfitPips` | Distância take profit em pips. | 50 |
| `StopLossPips` | Distância stop loss em pips. | 135 |
| `IndentPips` | Deslocamento além de um fractal antes de colocar a ordem stop. | 15 |
| `RsiUpper` | Limiar RSI que cancela sell stops. | 70 |
| `RsiLower` | Limiar RSI que cancela buy stops. | 30 |
| `SarTrailingPips` | Distância mínima (em pips) entre o preço e o SAR antes do trailing. | 10 |
| `CandleType` | Tipo de dados / período para processamento. | Candles de 4 horas |

## Notas
- A versão Python é omitida intencionalmente, conforme solicitado.
- Use `Volume` na classe base para configurar o tamanho do lote (padrão 1 se não especificado).
- A estratégia deve ser executada no mesmo período da configuração original do EA (EURUSD H4 de acordo com o arquivo `.set` fornecido).
