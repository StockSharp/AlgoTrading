# MarsiEaEstratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

`MarsiEaStrategy` replica a lógica do consultor especialista MARSIEA MetaTrader original dentro do StockSharp API de alto nível. A estratégia combina uma média móvel simples com um filtro de índice de força relativa (RSI) e mantém apenas uma posição por vez. As ordens protetoras de stop-loss e take-profit são medidas em pips exatamente como a implementação da fonte, enquanto o volume negociado é dimensionado dinamicamente a partir do patrimônio do portfólio.

## Lógica de negociação

1. **Preparação de dados**
   - Uma média móvel simples (SMA) com comprimento configurável é executada na série de velas selecionada.
   - Um RSI com período configurável usa as mesmas velas.
   - A série de velas é configurável por meio do parâmetro `CandleType` e o padrão é velas de um minuto.

2. **Regras de entrada**
   - A estratégia exige que ambos os indicadores sejam formados e que não exista nenhuma posição aberta.
   - **Configuração longa:** o preço de fechamento está acima do SMA e o RSI está abaixo do limite de sobrevenda.
   - **Configuração curta:** o preço de fechamento está abaixo do SMA e o RSI está acima do limite de sobrecompra.
   - Apenas uma posição pode ser aberta a qualquer momento, refletindo o comportamento do especialista MetaTrader.

3. **Regras de saída**
   - Imediatamente após entrar numa negociação, a estratégia regista uma distância fixa de stop-loss e take-profit, ambas definidas em pips.
   - Não existem condições de saída adicionais; as ordens de proteção tratam do fechamento da posição.

## Dimensionamento de risco e posição

- `RiskPercent` controla a porcentagem do valor atual do portfólio arriscado por negociação.
- O valor pip é calculado a partir de `Security.PriceStep`, `Security.StepPrice` e o número de dígitos, emulando a verificação `_Digits` de MQL.
- O volume é arredondado para o `Security.VolumeStep` mais próximo permitido e respeita `Security.VolumeMin` quando disponível.
- Se o dimensionamento baseado em risco não puder ser calculado (faltando metadados do instrumento ou parada zero), a estratégia volta para a propriedade `Volume` (padrão para 1 contrato/lote).

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Série de velas usada para cálculos de indicadores. |
| `MaPeriod` | Comprimento do indicador SMA. |
| `RsiPeriod` | Comprimento de lookback para RSI. |
| `RsiOverbought` | Limite de RSI que define um mercado de sobrecompra para posições vendidas. |
| `RsiOversold` | RSI limite que define um mercado de sobrevenda para posições compradas. |
| `RiskPercent` | Porcentagem de patrimônio arriscado por negociação. |
| `StopLossPips` | Distância de stop-loss expressa em pips. |
| `TakeProfitPips` | Distância de lucro expressa em pips. |

## Notas sobre a conversão

- A implementação MetaTrader negociada a preços Bid/Ask; esta porta usa o fechamento da vela como referência de entrada porque os ticks intrabar não estão disponíveis no nível alto API.
- O tamanho do pip segue a mesma regra da versão MQL: símbolos de cinco ou três dígitos multiplicam o nível de preço por dez.
- `StartProtection()` é invocado uma vez para que as ordens stop-loss e take-profit sejam automaticamente vinculadas à posição aberta pelo mecanismo.
- A estratégia mantém o comportamento original de pular novas entradas enquanto qualquer posição estiver ativa.
