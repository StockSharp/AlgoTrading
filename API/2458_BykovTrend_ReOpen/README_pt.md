# Estratégia BykovTrend ReOpen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia BykovTrend ReOpen usa a lógica do BykovTrend baseada nos indicadores Williams %R e Average True Range. Um sinal de compra ocorre quando a tendência se torna de alta e um sinal de venda quando se torna de baixa. Após entrar em uma posição, a estratégia pode reabrir posições adicionais a cada passo de preço predefinido enquanto a tendência continua. O stop loss e o take-profit são aplicados a partir do preço da última entrada.

## Indicador
A estratégia não requer um arquivo de indicador separado. Ela calcula sinais usando:
- **Williams %R** com período `SSP`.
- **ATR** com período fixo de 15.
A tendência muda quando o Williams %R cruza os limiares `-100 + K` e `-K`, onde `K = 33 - Risk`.

## Regras de negociação
1. Em um sinal de alta, fecha as posições vendidas (se permitido) e abre uma posição comprada.
2. Em um sinal de baixa, fecha as posições compradas (se permitido) e abre uma posição vendida.
3. Enquanto uma posição está aberta, novas posições na mesma direção são adicionadas a cada `Price Step` unidades até atingir `Max Positions`.
4. Cada posição tem distâncias de stop loss e take-profit medidas a partir do último preço de entrada.

## Parâmetros
- `Risk` – fator de risco que define os limiares do indicador.
- `SSP` – período do Williams %R.
- `Price Step` – distância de preço para adicionar uma nova posição.
- `Max Positions` – número máximo de posições abertas por lado.
- `Stop Loss` – distância do stop loss em unidades de preço.
- `Take Profit` – distância do take-profit em unidades de preço.
- `Enable Long Open` – permitir abertura de posições compradas.
- `Enable Short Open` – permitir abertura de posições vendidas.
- `Enable Long Close` – permitir fechamento de posições compradas em sinal contrário.
- `Enable Short Close` – permitir fechamento de posições vendidas em sinal contrário.
- `Candle Type` – período utilizado para os cálculos.
